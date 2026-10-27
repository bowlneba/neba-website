using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentOilPatternAnnouncementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(2);

    [Fact(DisplayName = "GetOilPatternAnnouncementTime returns null when there is no reveal date")]
    public void GetOilPatternAnnouncementTime_ShouldReturnNull_WhenNoRevealDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: null);

        // Act
        var result = tournament.GetOilPatternAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetOilPatternAnnouncementTime returns null when the reveal date is in the past")]
    public void GetOilPatternAnnouncementTime_ShouldReturnNull_WhenRevealDateIsPast()
    {
        // Arrange
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: Now.AddMinutes(-1));

        // Act
        var result = tournament.GetOilPatternAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetOilPatternAnnouncementTime returns null when the reveal date is now")]
    public void GetOilPatternAnnouncementTime_ShouldReturnNull_WhenRevealDateIsNow()
    {
        // Arrange
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: Now);

        // Act
        var result = tournament.GetOilPatternAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetOilPatternAnnouncementTime returns null when the reveal was already announced, even if the date moved later")]
    public void GetOilPatternAnnouncementTime_ShouldReturnNull_WhenAlreadyAnnounced()
    {
        // Arrange
        var tournament = TournamentFactory.Create(
            oilPatternRevealDateTime: Now.AddDays(3),
            oilPatternAnnouncedAtUtc: Now.AddDays(-1));

        // Act
        var result = tournament.GetOilPatternAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetOilPatternAnnouncementTime adds the delay to a future reveal date")]
    public void GetOilPatternAnnouncementTime_ShouldAddDelayToRevealDate_WhenRevealDateIsFuture()
    {
        // Arrange
        var revealAt = Now.AddDays(3);
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: revealAt);

        // Act
        var result = tournament.GetOilPatternAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBe(revealAt + Delay);
    }

    [Fact(DisplayName = "MarkOilPatternAnnounced records the time")]
    public void MarkOilPatternAnnounced_ShouldRecordTime()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        tournament.MarkOilPatternAnnounced(Now);

        // Assert
        tournament.OilPatternAnnouncedAtUtc.ShouldBe(Now);
    }

    [Fact(DisplayName = "MarkOilPatternAnnounced keeps the first timestamp when called twice")]
    public void MarkOilPatternAnnounced_ShouldKeepFirstTimestamp_WhenCalledTwice()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.MarkOilPatternAnnounced(Now);

        // Act
        tournament.MarkOilPatternAnnounced(Now.AddHours(1));

        // Assert
        tournament.OilPatternAnnouncedAtUtc.ShouldBe(Now);
    }

    [Fact(DisplayName = "Create drops sub-microsecond ticks from the reveal date")]
    public void Create_ShouldTruncateRevealDateToMicroseconds()
    {
        // Arrange
        var revealAt = Now.AddDays(3);

        // Act
        var tournament = TournamentFactory.Create(oilPatternRevealDateTime: revealAt.AddTicks(7));

        // Assert
        tournament.OilPatternRevealDateTime.ShouldBe(revealAt);
    }

    [Fact(DisplayName = "Update drops sub-microsecond ticks from the reveal date")]
    public void Update_ShouldTruncateRevealDateToMicroseconds()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var revealAt = Now.AddDays(3);

        // Act
        var result = tournament.Update(
            name: tournament.Name,
            tournamentType: tournament.TournamentType,
            startDate: tournament.StartDate,
            endDate: tournament.EndDate,
            seasonId: tournament.SeasonId,
            statsEligible: tournament.StatsEligible,
            entryFee: tournament.EntryFee,
            nebaAddedMoney: tournament.NebaAddedMoney,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: revealAt.AddTicks(7));

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.OilPatternRevealDateTime.ShouldBe(revealAt);
    }
}
