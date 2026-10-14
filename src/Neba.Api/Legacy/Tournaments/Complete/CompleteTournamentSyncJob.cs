using System.Data;
using System.Globalization;

using Dapper;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Legacy.Tournaments.Stats;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Tournaments.Complete;

// Thin on purpose: this job's only job is "mark the website tournament complete, then hand off
// to whatever else needs to happen as a result." It does not itself populate TournamentResult
// rows or touch neba-fwk beyond the one EF write - that's SyncTournamentResultsJob's job,
// chained from here so it (and any future sibling, e.g. a season-stats generator) runs as its
// own independent, independently-retryable Hangfire job rather than being bundled into one
// large unit of work.
internal sealed class CompleteTournamentSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IBackgroundJobClient jobs,
    IFusionCache cache,
    IManualInterventionNotifier manualInterventionNotifier,
    ILogger<CompleteTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        var tournament = await db.Set<Tournament>()
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            logger.LogLegacyTournamentNotSyncedForCompletion(legacyTournamentId);

            var alert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Tournament completion could not be matched",
                "The legacy tournament completion event referenced a legacy tournament id with no linked website tournament (no Tournament.LegacyId match). Results were not synced. Re-trigger completion after the tournament is linked.",
                new Dictionary<string, string>
                {
                    ["LegacyTournamentId"] = legacyTournamentId.ToString(CultureInfo.InvariantCulture)
                });

            await manualInterventionNotifier.NotifyAsync(alert, ct);

            return;
        }

        var entryCount = await GetEntryCountAsync(legacyTournamentId, tournament.TournamentType.TeamSize, ct);

        var completeResult = tournament.CompleteTournament(entryCount);
        if (completeResult.IsError)
        {
            // AlreadyFinalized: expected on retry/re-fire, or if the tournament was already
            // Truncated/Cancelled from the admin panel before the legacy event arrived. Not fatal —
            // still chain the follow-on jobs below; they're each independently safe to re-run.
            logger.LogLegacyTournamentAlreadyCompleteForResultSync(legacyTournamentId);
        }
        else
        {
            await db.SaveChangesAsync(ct);

            // Status flip changes both the tournament's own detail and how it renders in the
            // season's tournament list (e.g. complete/upcoming filtering).
            await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: ct);
            await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: ct);

            // A completed tournament counts toward the season's stat minimums, so cached season stats
            // are stale now rather than when GenerateSeasonStatsJob runs ten minutes later.
            await cache.RemoveByTagAsync($"neba:stats:seasons:{tournament.SeasonId}", token: ct);
        }

        jobs.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None));

        // Scheduled, not enqueued: gives SyncTournamentResultsJob time to finish placing/writing
        // TournamentResult rows before GenerateSeasonStatsJob reads them. See the plan's "Ordering"
        // discussion - this is a data-freshness improvement, not a correctness dependency, since
        // GenerateSeasonStatsJob's delete-and-regenerate is idempotent and self-corrects on retry.
        jobs.Schedule<GenerateSeasonStatsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None), TimeSpan.FromMinutes(10));
    }

    // Same distinct-(BowlerId, SquadId)-pairs-÷-TeamSize formula GetTournamentQueryHandler already
    // uses for website-tracked tournaments (see EntryCount there) — just read from the legacy DB,
    // since this job runs before SyncTournamentResultsJob populates any website-side SquadScore rows.
#pragma warning disable DAP005
    private async Task<int> GetEntryCountAsync(int legacyTournamentId, int teamSize, CancellationToken ct)
    {
        var pairCount = await legacyConnection.QuerySingleAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(DISTINCT CONCAT(s.BowlerId, '-', q.SquadId))
                FROM Stats s
                INNER JOIN Stats_QualifyingStats q ON s.Id = q.Id
                WHERE s.TournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId },
                cancellationToken: ct));

        return pairCount / teamSize;
    }
#pragma warning restore DAP005
}