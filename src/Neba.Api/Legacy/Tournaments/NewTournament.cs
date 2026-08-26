using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using ErrorOr;

using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Tournaments;

internal static class NewTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewTournament()
        {
            app.MapPost("/tournaments/new", (
                NewTournamentRequest request,
                [FromServices] IValidator<NewTournamentRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<NewTournamentSyncJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record NewTournamentRequest(int TournamentId);

internal sealed class NewTournamentRequestValidator
    : AbstractValidator<NewTournamentRequest>
{
    public NewTournamentRequestValidator()
    {
        RuleFor(request => request.TournamentId)
            .GreaterThan(0);
    }
}

internal static class LegacyTournamentLinkExtensions
{
    extension(Tournament tournament)
    {
        public void ApplyLegacyId(int legacyTournamentId) => tournament.LegacyId = legacyTournamentId;
    }
}

internal sealed class NewTournamentSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    IEmailSender emailSender,
    ILogger<NewTournamentSyncJob> logger)
{
    private static readonly TimeZoneInfo EasternTimeZone = FindEasternTimeZone();

    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var alreadyLinkedTournament = await db.Set<Tournament>()
            .Include(t => t.Squads)
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (alreadyLinkedTournament is not null)
        {
            logger.LogLegacyTournamentAlreadyLinked(legacyTournamentId);
            await SyncSquadsAsync(alreadyLinkedTournament, legacyTournamentId, ct);
            await db.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync($"neba:tournaments:{alreadyLinkedTournament.Id}", token: ct);
            return;
        }

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyTournamentRow>(
            """
            SELECT
                t.Id,
                t.End,
                s.TournamentType AS SinglesTournamentType,
                tm.TeamSize AS TeamSize,
                tm.OverUnder AS OverUnder
            FROM
                Tournaments t
            LEFT JOIN Tournaments_SinglesTournament s ON s.Id = t.Id
            LEFT JOIN Tournaments_TeamTournament tm ON tm.Id = t.Id
            WHERE
                t.Id = @Id
            """, new
            {
                Id = legacyTournamentId
            }
        );
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyTournamentNotFound(legacyTournamentId);
            return;
        }

        var endDate = DateOnly.FromDateTime(row.End);

        var candidates = await db.Set<Tournament>()
            .Include(t => t.Squads)
            .Where(t => t.EndDate == endDate && t.LegacyId == null)
            .ToListAsync(ct);

        var isTeam = row.TeamSize.HasValue;

        if (candidates.Count > 1)
        {
            candidates = isTeam
                ?
                [
                    .. candidates.Where(t => t.TournamentType.TeamSize > 1)
                ]
                :
                [
                    .. candidates.Where(t => t.TournamentType.TeamSize == 1)
                ];
        }

        if (candidates.Count > 1)
        {
            var mappedType = MapLegacyTournamentType(row);
            if (mappedType is not null)
            {
                candidates =
                [
                    .. candidates.Where(t => t.TournamentType == mappedType)
                ];
            }
        }

        if (candidates.Count != 1)
        {
            logger.LogLegacyTournamentCannotBeDerived(legacyTournamentId, candidates.Count);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: tournament link",
                HtmlBody = new TournamentLinkCannotBeDerivedEmail(legacyTournamentId, endDate, candidates.Count).ToHtmlBody()
            }, ct);

            return;
        }

        candidates[0].ApplyLegacyId(legacyTournamentId);
        await SyncSquadsAsync(candidates[0], legacyTournamentId, ct);
        await db.SaveChangesAsync(ct);

        // A brand-new link changes which tournament shows up in the season's list, not just that
        // tournament's own detail.
        await cache.RemoveByTagAsync($"neba:tournaments:{candidates[0].SeasonId}", token: ct);
        await cache.RemoveByTagAsync($"neba:tournaments:{candidates[0].Id}", token: ct);
    }

    private async Task SyncSquadsAsync(Tournament tournament, int legacyTournamentId, CancellationToken ct)
    {
        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var squadRows = await legacyConnection.QueryAsync<LegacySquadRow>(
            new CommandDefinition(
                """
                SELECT
                    s.Id AS SquadId,
                    COALESCE(ss.TournamentId, ts.TournamentId) AS TournamentId,
                    s.BowlingDate AS BowlingDate
                FROM
                    Squads s
                LEFT JOIN Squads_SinglesSquad ss ON ss.Id = s.Id
                LEFT JOIN Squads_TeamSquad ts ON ts.Id = s.Id
                WHERE
                    COALESCE(ss.TournamentId, ts.TournamentId) = @TournamentId
                """,
                new { TournamentId = legacyTournamentId },
                cancellationToken: ct)
        );
#pragma warning restore DAP005

        foreach (var squadRow in squadRows)
        {
            if (tournament.Squads.Any(squad => squad.LegacyId == squadRow.SquadId))
            {
                continue;
            }

            var bowlingDateTime = ToEasternDateTimeOffset(squadRow.BowlingDate);

            var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: squadRow.SquadId);
            if (result.IsError)
            {
                logger.LogLegacySquadSyncFailed(squadRow.SquadId, legacyTournamentId, result.FirstError.Description);
            }
        }
    }

    // The legacy database stores squad bowling dates/times as naive local wall-clock values in
    // U.S. Eastern time (EST or EDT depending on the date), with no offset recorded. Deriving the
    // correct offset per-date (rather than assuming a fixed -05:00/-04:00) is required so the
    // resulting UTC instant is correct across the DST boundary.
    private static DateTimeOffset ToEasternDateTimeOffset(DateTime easternLocalDateTime)
    {
        var unspecified = DateTime.SpecifyKind(easternLocalDateTime, DateTimeKind.Unspecified);

        // Spring-forward gap: the wall-clock time never existed. Shift forward past the gap
        // rather than deriving an offset for a moment that isn't real.
        if (EasternTimeZone.IsInvalidTime(unspecified))
        {
            var delta = EasternTimeZone.GetAdjustmentRules()
                .FirstOrDefault(rule => unspecified >= rule.DateStart && unspecified <= rule.DateEnd)
                ?.DaylightDelta ?? TimeSpan.FromHours(1);

            unspecified = unspecified.Add(delta);
        }

        // Ambiguous times (fall-back hour) resolve to standard time (EST) — TimeZoneInfo's
        // own default resolution for an ambiguous local time.
        return new DateTimeOffset(unspecified, EasternTimeZone.GetUtcOffset(unspecified));
    }

    private static TimeZoneInfo FindEasternTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }

    // Maps the legacy row's singles-type enum or team shape to the website's TournamentType.
    // Returns null when the legacy row can't be confidently mapped to an exact type (an unrecognized
    // SinglesTournamentTypes value, or a team combination with no website equivalent) - callers treat
    // a null mapping as "type unknown," meaning the exact-type narrowing step is skipped rather than
    // incorrectly eliminating every remaining candidate.
    private static TournamentType? MapLegacyTournamentType(LegacyTournamentRow row)
    {
        if (row.SinglesTournamentType.HasValue)
        {
            return row.SinglesTournamentType.Value switch
            {
                0 => TournamentType.Singles,           // Standard
                1 => TournamentType.NonChampions,
                2 => TournamentType.Senior,
                3 => TournamentType.Women,
                4 => TournamentType.TournamentOfChampions, // Champions
                5 => TournamentType.Invitational,
                6 => TournamentType.Masters,
                7 => TournamentType.Youth,
                8 => TournamentType.SeniorAndWomen,     // SeniorWithWomen
                _ => null
            };
        }

        return !row.TeamSize.HasValue
            ? null
            : (row.TeamSize.Value, row.OverUnder) switch
            {
                (2, true) => TournamentType.OverUnderFiftyDoubles, // Forty variant confirmed unreachable via this bit
                (2, false or null) => TournamentType.Doubles,
                (3, false or null) => TournamentType.Trios,
                (5, false or null) => TournamentType.Baker, // No IsBaker column exists; Baker is identified by team size alone
                _ => null
            };
    }
}

internal sealed record LegacyTournamentRow(
    int Id,
    DateTime End,
    int? SinglesTournamentType,
    int? TeamSize,
    bool? OverUnder);

internal sealed record LegacySquadRow(
    int SquadId,
    int TournamentId,
    DateTime BowlingDate);

internal static partial class NewTournamentSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy tournament {LegacyTournamentId} is already linked; skipping.")]
    public static partial void LogLegacyTournamentAlreadyLinked(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No tournament found in neba-fwk for legacy id {LegacyTournamentId}; skipping link sync.")]
    public static partial void LogLegacyTournamentNotFound(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not derive a unique website tournament for legacy id {LegacyTournamentId} ({CandidateCount} candidate(s) remaining after narrowing); manual intervention email sent.")]
    public static partial void LogLegacyTournamentCannotBeDerived(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId, int candidateCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to sync legacy squad {LegacySquadId} for legacy tournament {LegacyTournamentId}: {Reason}")]
    public static partial void LogLegacySquadSyncFailed(this ILogger<NewTournamentSyncJob> logger, int legacySquadId, int legacyTournamentId, string reason);
}

internal sealed class TournamentLinkCannotBeDerivedEmail(int legacyTournamentId, DateOnly endDate, int candidateCount)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Tournament with legacy id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> cannot be derived and needs manual intervention.</p>
                    <p>End date: {WebUtility.HtmlEncode(endDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture))}<br/>
                    Candidate website tournaments remaining after narrowing: {WebUtility.HtmlEncode(candidateCount.ToString(CultureInfo.CurrentCulture))}</p>
                    <p>Use the tournament's <code>LegacyId</code> column to link it manually once the correct match is identified.</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}