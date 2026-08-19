using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;
using Neba.Api.Legacy.Tournaments.Stats;

namespace Neba.Api.Legacy.Tournaments.Complete;

// Thin on purpose: this job's only job is "mark the website tournament complete, then hand off
// to whatever else needs to happen as a result." It does not itself populate TournamentResult
// rows or touch neba-fwk beyond the one EF write - that's SyncTournamentResultsJob's job,
// chained from here so it (and any future sibling, e.g. a season-stats generator) runs as its
// own independent, independently-retryable Hangfire job rather than being bundled into one
// large unit of work.
internal sealed class CompleteTournamentSyncJob(
    AppDbContext db,
    IBackgroundJobClient jobs,
    IEmailSender emailSender,
    ILogger<CompleteTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var tournament = await db.Set<Tournament>()
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            logger.LogLegacyTournamentNotSyncedForCompletion(legacyTournamentId);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: tournament completion with no linked tournament",
                HtmlBody = new UnlinkedTournamentCompletionEmail(legacyTournamentId).ToHtmlBody()
            }, ct);

            return;
        }

        var completeResult = tournament.CompleteTournament();
        if (completeResult.IsError)
        {
            // AlreadyComplete: expected on retry/re-fire. Not fatal — still chain the follow-on
            // jobs below (see idempotency decision in the plan); they're each independently safe
            // to re-run.
            logger.LogLegacyTournamentAlreadyCompleteForResultSync(legacyTournamentId);
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        jobs.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None));

        // Scheduled, not enqueued: gives SyncTournamentResultsJob time to finish placing/writing
        // TournamentResult rows before GenerateSeasonStatsJob reads them. See the plan's "Ordering"
        // discussion - this is a data-freshness improvement, not a correctness dependency, since
        // GenerateSeasonStatsJob's delete-and-regenerate is idempotent and self-corrects on retry.
        jobs.Schedule<GenerateSeasonStatsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None), TimeSpan.FromMinutes(10));
    }
}