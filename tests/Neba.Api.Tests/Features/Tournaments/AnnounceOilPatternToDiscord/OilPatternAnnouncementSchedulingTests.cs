using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.AnnounceOilPatternToDiscord;

[UnitTest]
[Component("Tournaments")]
public sealed class OilPatternAnnouncementSchedulingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(2);

    [Fact(DisplayName = "ScheduleOilPatternAnnouncement schedules the job at the reveal date plus the delay")]
    public void ScheduleOilPatternAnnouncement_ShouldScheduleAtRevealDatePlusDelay_WhenRevealDateIsFuture()
    {
        // Arrange
        var revealAt = Now.AddDays(3);
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: revealAt);
        AnnounceOilPatternToDiscordJob? scheduled = null;
        DateTimeOffset? scheduledAt = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(s => s.Schedule(It.IsAny<AnnounceOilPatternToDiscordJob>(), It.IsAny<DateTimeOffset>()))
            .Callback<AnnounceOilPatternToDiscordJob, DateTimeOffset>((job, at) =>
            {
                scheduled = job;
                scheduledAt = at;
            })
            .Returns("job-id");

        // Act
        scheduler.Object.ScheduleOilPatternAnnouncement(tournament, Now, Delay);

        // Assert
        scheduled.ShouldNotBeNull();
        scheduled.TournamentId.ShouldBe(tournament.Id);
        scheduled.ExpectedRevealDateTime.ShouldBe(revealAt);
        scheduledAt.ShouldBe(revealAt + Delay);
    }

    [Fact(DisplayName = "ScheduleOilPatternAnnouncement truncates the expected reveal date to microseconds")]
    public void ScheduleOilPatternAnnouncement_ShouldTruncateExpectedRevealDateToMicroseconds()
    {
        // Arrange
        var revealAt = Now.AddDays(3).AddTicks(7);
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: revealAt);
        AnnounceOilPatternToDiscordJob? scheduled = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(s => s.Schedule(It.IsAny<AnnounceOilPatternToDiscordJob>(), revealAt + Delay))
            .Callback<AnnounceOilPatternToDiscordJob, DateTimeOffset>((job, _) => scheduled = job)
            .Returns("job-id");

        // Act
        scheduler.Object.ScheduleOilPatternAnnouncement(tournament, Now, Delay);

        // Assert
        scheduled.ShouldNotBeNull();
        scheduled.ExpectedRevealDateTime.ShouldBe(Now.AddDays(3));
    }

    [Fact(DisplayName = "ScheduleOilPatternAnnouncement schedules nothing when there is no reveal date")]
    public void ScheduleOilPatternAnnouncement_ShouldScheduleNothing_WhenNoRevealDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: null);
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);

        // Act
        var act = () => scheduler.Object.ScheduleOilPatternAnnouncement(tournament, Now, Delay);

        // Assert
        Should.NotThrow(act); // the strict mock has no setup, so any Schedule call would throw
    }

    [Fact(DisplayName = "ScheduleOilPatternAnnouncement schedules nothing when the reveal date is in the past")]
    public void ScheduleOilPatternAnnouncement_ShouldScheduleNothing_WhenRevealDateIsPast()
    {
        // Arrange
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: Now.AddMinutes(-1));
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);

        // Act
        var act = () => scheduler.Object.ScheduleOilPatternAnnouncement(tournament, Now, Delay);

        // Assert
        Should.NotThrow(act); // the strict mock has no setup, so any Schedule call would throw
    }

    [Fact(DisplayName = "ScheduleOilPatternAnnouncement schedules nothing when the reveal was already announced")]
    public void ScheduleOilPatternAnnouncement_ShouldScheduleNothing_WhenAlreadyAnnounced()
    {
        // Arrange
        var tournament = TournamentFactory.Create(
            oilPatternRevealDateTime: Now.AddDays(3),
            oilPatternAnnouncedAtUtc: Now.AddDays(-1));
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);

        // Act
        var act = () => scheduler.Object.ScheduleOilPatternAnnouncement(tournament, Now, Delay);

        // Assert
        Should.NotThrow(act); // the strict mock has no setup, so any Schedule call would throw
    }
}
