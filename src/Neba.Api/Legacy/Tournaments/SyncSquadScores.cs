using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using FluentValidation;

using Hangfire;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Tournaments;

internal static class SyncSquadScoresEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapSyncSquadScores()
        {
            app.MapPost("/squads/scores/sync", (
                SyncSquadScoresRequest request,
                [FromServices] IValidator<SyncSquadScoresRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
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
    IFusionCache cache,
    IManualInterventionNotifier manualInterventionNotifier,
    ILogger<SyncSquadScoresSyncJob> logger)
{
    public async Task SyncAsync(int legacySquadId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        var squad = await db.Set<Squad>().SingleOrDefaultAsync(s => s.LegacyId == legacySquadId, ct);
        if (squad is null)
        {
            logger.LogLegacySquadNotSyncedForScoreSync(legacySquadId);
            return;
        }

        // Squad.Tournament isn't a mapped navigation (SquadConfiguration.HasOne<Tournament>() has no
        // navigation expression), so it can't be Include()'d - look the owning tournament's id up via
        // Tournament.Squads (the mapped side of the relationship) instead.
        var tournamentId = await db.Set<Tournament>()
            .Where(t => t.Squads.Any(s => s.Id == squad.Id))
            .Select(t => t.Id)
            .SingleAsync(ct);

        var rows = await LoadLegacyScoreRowsAsync(legacySquadId);
        var bowlerIdsByLegacyId = await LoadBowlerIdsByLegacyIdAsync(rows, ct);

        // The configured NpgsqlRetryingExecutionStrategy rejects user-initiated transactions, so the
        // whole transaction runs as one retriable unit through the strategy. Each attempt starts from a
        // clean change tracker and rebuilds its working state, since a retry re-runs the delegate.
        var unmappedRowsByLegacyBowlerId = new Dictionary<int, List<LegacyQualifyingScoreRow>>();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(() => ReplaceSquadScoresAsync(
            squad, legacySquadId, rows, bowlerIdsByLegacyId, unmappedRowsByLegacyBowlerId, ct));

        await cache.RemoveByTagAsync($"neba:tournaments:{tournamentId}", token: ct);

        if (unmappedRowsByLegacyBowlerId.Count == 0)
        {
            return;
        }

        // One alert for the whole squad, sent after SaveChangesAsync so the sync isn't held up by it -
        // a per-bowler post would hit Discord's webhook rate limit on a squad with many unlinked bowlers.
        var unsyncedScores = unmappedRowsByLegacyBowlerId
            .OrderBy(entry => entry.Key)
            .Select(entry => $"{entry.Key}: " + string.Join(", ", entry.Value
                .OrderBy(row => row.Game)
                .Select(row => $"G{row.Game} {row.Score}")))
            .ToList();

        var alert = new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Unsynced bowlers in score sync",
            "These legacy bowlers have scores in a squad sync but no matching website bowler (no Bowler.LegacyId match). "
            + "Their scores were not saved. Link the bowlers, then re-trigger this squad's score sync.",
            new Dictionary<string, string>
            {
                ["LegacySquadId"] = legacySquadId.ToString(CultureInfo.InvariantCulture),
                ["UnsyncedBowlerCount"] = unmappedRowsByLegacyBowlerId.Count.ToString(CultureInfo.InvariantCulture),
                ["UnsyncedScores"] = DiscordFieldFormatter.JoinLines(unsyncedScores)
            });

        await manualInterventionNotifier.NotifyAsync(alert, ct);
    }

    private async Task<List<LegacyQualifyingScoreRow>> LoadLegacyScoreRowsAsync(int legacySquadId)
    {
        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = await legacyConnection.QueryAsync<LegacyQualifyingScoreRow>(
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
            new { SquadId = legacySquadId });
#pragma warning restore DAP005

        return [.. rows];
    }

    private async Task<Dictionary<int, BowlerId>> LoadBowlerIdsByLegacyIdAsync(
        List<LegacyQualifyingScoreRow> rows,
        CancellationToken ct)
    {
        var legacyBowlerIds = rows.Select(row => row.BowlerId).Distinct().ToList();

        return await db.Bowlers
            .Where(bowler => bowler.LegacyId != null && legacyBowlerIds.Contains(bowler.LegacyId.Value))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, bowler => bowler.Id, ct);
    }

    private async Task ReplaceSquadScoresAsync(
        Squad squad,
        int legacySquadId,
        List<LegacyQualifyingScoreRow> rows,
        Dictionary<int, BowlerId> bowlerIdsByLegacyId,
        Dictionary<int, List<LegacyQualifyingScoreRow>> unmappedRowsByLegacyBowlerId,
        CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        unmappedRowsByLegacyBowlerId.Clear();

        var existing = await db.SquadScores
            .Where(squadScore => squadScore.SquadId == squad.Id)
            .ToListAsync(ct);

        // Intentionally NOT "batch then save once" (the usual SaveChanges-in-a-loop fix): the loop
        // itself is the fix. Audit.EntityFramework serializes every changed entry's old/new values
        // from one SaveChangesAsync call into a single EF audit event property, and a large squad
        // (many bowlers x games) can produce hundreds of changed SquadScore rows in one call - past
        // Azure Table Storage's 64KB per-property limit, silently dropped by
        // ResilientAuditDataProvider. Capping each SaveChangesAsync's entity count bounds that
        // property's size regardless of squad size. The whole batch loop runs inside one explicit
        // transaction so a mid-loop failure rolls back every prior batch instead of leaving the
        // squad's scores partially resynced - PooledAuditSaveChangesInterceptor opens/closes its own
        // Audit.EntityFramework scope per SaveChangesAsync call regardless of an enclosing
        // transaction, so this doesn't merge or grow any audit event.
        const int saveBatchSize = 50;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        foreach (var batch in existing.Chunk(saveBatchSize))
        {
            db.SquadScores.RemoveRange(batch);
            await db.SaveChangesAsync(ct);
        }

        var pendingScores = BuildPendingScores(
            squad, legacySquadId, rows, bowlerIdsByLegacyId, unmappedRowsByLegacyBowlerId);

        foreach (var batch in pendingScores.Chunk(saveBatchSize))
        {
            await db.SquadScores.AddRangeAsync(batch, ct);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    // Rows whose legacy bowler id has no website Bowler.LegacyId match are grouped into
    // unmappedRowsByLegacyBowlerId so one alert covers all of that bowler's scores rather than one
    // alert per row.
    private List<SquadScore> BuildPendingScores(
        Squad squad,
        int legacySquadId,
        List<LegacyQualifyingScoreRow> rows,
        Dictionary<int, BowlerId> bowlerIdsByLegacyId,
        Dictionary<int, List<LegacyQualifyingScoreRow>> unmappedRowsByLegacyBowlerId)
    {
        var pendingScores = new List<SquadScore>();

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

            pendingScores.Add(created.Value);
        }

        return pendingScores;
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
        Message = "No website bowler found for legacy bowler {LegacyBowlerId} (legacy squad {LegacySquadId}); skipping their scores and sending a manual-intervention alert.")]
    public static partial void LogLegacyBowlerNotSyncedForScoreSync(this ILogger<SyncSquadScoresSyncJob> logger, int legacyBowlerId, int legacySquadId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to sync score for legacy bowler {LegacyBowlerId}, game {Game} (legacy squad {LegacySquadId}): {Reason}")]
    public static partial void LogLegacySquadScoreSyncFailed(this ILogger<SyncSquadScoresSyncJob> logger, int legacyBowlerId, int game, int legacySquadId, string reason);
}