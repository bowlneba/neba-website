using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using ErrorOr;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.Tournaments;

internal static class NewTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewTournament()
        {
            app.MapPost("/tournaments/new", (
                NewTournamentRequest request,
                IValidator<NewTournamentRequest> validator,
                IBackgroundJobClient jobs) =>
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
    IEmailSender emailSender,
    ILogger<NewTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var alreadyLinked = await db.Set<Tournament>()
            .AnyAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (alreadyLinked)
        {
            logger.LogLegacyTournamentAlreadyLinked(legacyTournamentId);
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
        await db.SaveChangesAsync(ct);
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

        if (!row.TeamSize.HasValue)
        {
            return null;
        }

        return (row.TeamSize.Value, row.OverUnder) switch
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