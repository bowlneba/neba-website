using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

internal static class OilPatternAnnouncementScheduling
{
    extension(IBackgroundJobScheduler scheduler)
    {
        /// <summary>
        /// Schedules the announcement job when <paramref name="tournament"/> is due one. Does nothing when
        /// the reveal time is missing or past, or the reveal was already announced. Does nothing when
        /// <paramref name="previousRevealDateTime"/> is the current reveal time either: the job from the
        /// earlier save is still pending, so another would only add a no-op run.
        /// </summary>
        public void ScheduleOilPatternAnnouncement(
            Tournament tournament, DateTimeOffset now, TimeSpan delay, DateTimeOffset? previousRevealDateTime = null)
        {
            ArgumentNullException.ThrowIfNull(tournament);

            var announceAt = tournament.GetOilPatternAnnouncementTime(now, delay);

            if (announceAt is null)
            {
                return;
            }

            if (previousRevealDateTime == tournament.OilPatternRevealDateTime)
            {
                return;
            }

            scheduler.Schedule(
                new AnnounceOilPatternToDiscordJob
                {
                    TournamentId = tournament.Id,
                    ExpectedRevealDateTime = tournament.OilPatternRevealDateTime!.Value
                },
                announceAt.Value);
        }
    }

}