using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.Tournaments;

internal static class SyncSquadScoresEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapSyncSquadScores()
        {
            app.MapPost("/squads/scores/sync", (
                SyncSquadScoresRequest request,
                IValidator<SyncSquadScoresRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<SyncSquadScoresSyncJob>(job => job.SyncAsync(request.SquadId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record SyncSquadScoresRequest(int SquadId);

internal sealed class SyncSquadScoresRequestValidator
    : AbstractValidator<SyncSquadScoresRequest>
{
    public SyncSquadScoresRequestValidator()
    {
        RuleFor(request => request.SquadId)
            .GreaterThan(0);
    }
}

internal sealed class SyncSquadScoresSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IEmailSender emailSender,
    ILogger<SyncSquadScoresSyncJob> logger)
{
    public async Task SyncAsync(int legacySquadId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var squad = await db.Set<Squad>().SingleOrDefaultAsync(s => s.LegacyId == legacySquadId, ct);
        if (squad is null)
        {
            logger.LogLegacySquadNotSyncedForScoreSync(legacySquadId);
            return;
        }

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = (await legacyConnection.QueryAsync<LegacyQualifyingScoreRow>(
            """
            SELECT
                BowlerId,
                Game,
                Score
            FROM
                QualifyingScores
            WHERE
                SquadId = @SquadId
            """,
            new { SquadId = legacySquadId })).ToList();
#pragma warning restore DAP005

        var legacyBowlerIds = rows.Select(row => row.BowlerId).Distinct().ToList();
        var bowlerIdsByLegacyId = await db.Bowlers
            .Where(bowler => bowler.LegacyId != null && legacyBowlerIds.Contains(bowler.LegacyId.Value))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, bowler => bowler.Id, ct);

        var existing = await db.SquadScores
            .Where(squadScore => squadScore.SquadId == squad.Id)
            .ToListAsync(ct);
        db.SquadScores.RemoveRange(existing);

        // Rows whose legacy bowler id has no website Bowler.LegacyId match, grouped so one email
        // covers all of that bowler's scores rather than one email per row.
        var unmappedRowsByLegacyBowlerId = new Dictionary<int, List<LegacyQualifyingScoreRow>>();

        foreach (var row in rows)
        {
            if (!bowlerIdsByLegacyId.TryGetValue(row.BowlerId, out var bowlerId))
            {
                logger.LogLegacyBowlerNotSyncedForScoreSync(row.BowlerId, legacySquadId);

                if (!unmappedRowsByLegacyBowlerId.TryGetValue(row.BowlerId, out var unmappedRows))
                {
                    unmappedRows = [];
                    unmappedRowsByLegacyBowlerId[row.BowlerId] = unmappedRows;
                }

                unmappedRows.Add(row);
                continue;
            }

            var created = SquadScore.Create(squad.Id, bowlerId, (short)row.Game, row.Score);
            if (created.IsError)
            {
                logger.LogLegacySquadScoreSyncFailed(legacySquadId, row.BowlerId, row.Game, created.FirstError.Description);
                continue;
            }

            await db.SquadScores.AddAsync(created.Value, ct);
        }

        await db.SaveChangesAsync(ct);

        // Sent after SaveChangesAsync so the rest of the squad's sync isn't held up by email
        // delivery - same ordering NewTournamentSyncJob uses for its own manual-intervention email.
        foreach ((int legacyBowlerId, List<LegacyQualifyingScoreRow> unmappedRows) in unmappedRowsByLegacyBowlerId)
        {
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: unsynced bowler in score sync",
                HtmlBody = new UnsyncedBowlerScoreSyncEmail(legacyBowlerId, legacySquadId, unmappedRows).ToHtmlBody()
            }, ct);
        }
    }
}

internal sealed record LegacyQualifyingScoreRow(int BowlerId, int Game, int Score);

internal static partial class SyncSquadScoresSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website squad found for legacy squad {LegacySquadId}; skipping score sync.")]
    public static partial void LogLegacySquadNotSyncedForScoreSync(this ILogger<SyncSquadScoresSyncJob> logger, int legacySquadId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website bowler found for legacy bowler {LegacyBowlerId} (legacy squad {LegacySquadId}); skipping their scores and sending a manual-intervention email.")]
    public static partial void LogLegacyBowlerNotSyncedForScoreSync(this ILogger<SyncSquadScoresSyncJob> logger, int legacyBowlerId, int legacySquadId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to sync score for legacy bowler {LegacyBowlerId}, game {Game} (legacy squad {LegacySquadId}): {Reason}")]
    public static partial void LogLegacySquadScoreSyncFailed(this ILogger<SyncSquadScoresSyncJob> logger, int legacyBowlerId, int game, int legacySquadId, string reason);
}

internal sealed class UnsyncedBowlerScoreSyncEmail(int legacyBowlerId, int legacySquadId, IReadOnlyCollection<LegacyQualifyingScoreRow> unmappedRows)
{
    public string ToHtmlBody()
    {
        var scoreRows = string.Join("", unmappedRows
            .OrderBy(row => row.Game)
            .Select(row => $"<tr><td>{WebUtility.HtmlEncode(row.Game.ToString(CultureInfo.CurrentCulture))}</td><td>{WebUtility.HtmlEncode(row.Score.ToString(CultureInfo.CurrentCulture))}</td></tr>"));

        var body = $"""
                    <p>Legacy bowler id <strong>{WebUtility.HtmlEncode(legacyBowlerId.ToString(CultureInfo.CurrentCulture))}</strong> has scores in a squad score sync but no matching website bowler (no <code>Bowler.LegacyId</code> match).</p>
                    <p>Legacy squad id: {WebUtility.HtmlEncode(legacySquadId.ToString(CultureInfo.CurrentCulture))}</p>
                    <p>This usually means the <code>NewBowler</code>/<code>UpdateBowler</code> backdoor sync never ran for this bowler. Their scores below were not saved to the website and will need to be re-synced (re-triggering this squad's score sync again after the bowler is linked will pick them up).</p>
                    <table><thead><tr><th>Game</th><th>Score</th></tr></thead><tbody>{scoreRows}</tbody></table>
                    """;

        return EmailLayout.Wrap(body);
    }
}