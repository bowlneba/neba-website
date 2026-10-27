using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

internal static class OilPatternAnnouncementScheduling
{
    // PostgreSQL stores timestamps to the microsecond, but a reveal time saved from the API can carry
    // finer ticks. The job compares the time it was scheduled for with the stored one, so schedule with
    // the value the database will return.
    private const long TicksPerMicrosecond = 10;

    extension(IBackgroundJobScheduler scheduler)
    {
        /// <summary>
        /// Schedules the announcement job when <paramref name="tournament"/> is due one. Does nothing when
        /// the reveal time is missing or past, or the reveal was already announced.
        /// </summary>
        public void ScheduleOilPatternAnnouncement(Tournament tournament, DateTimeOffset now, TimeSpan delay)
        {
            ArgumentNullException.ThrowIfNull(tournament);

            var announceAt = tournament.GetOilPatternAnnouncementTime(now, delay);

            if (announceAt is null)
            {
                return;
            }

            scheduler.Schedule(
                new AnnounceOilPatternToDiscordJob
                {
                    TournamentId = tournament.Id,
                    ExpectedRevealDateTime = TruncateToMicroseconds(tournament.OilPatternRevealDateTime!.Value)
                },
                announceAt.Value);
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        => new(value.Ticks - (value.Ticks % TicksPerMicrosecond), value.Offset);
}
