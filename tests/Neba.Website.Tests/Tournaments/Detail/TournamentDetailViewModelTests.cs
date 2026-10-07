using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentDetailViewModel")]
public sealed class TournamentDetailViewModelTests
{
    [Fact(DisplayName = "Should order breakdown sponsors by amount descending")]
    public void AddedMoneyBreakdown_ShouldOrderByAmountDescending_WhenAmountsDiffer()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(sponsors:
        [
            TournamentDetailSponsorViewModelFactory.Create(name: "B", sponsorshipAmount: 700m),
            TournamentDetailSponsorViewModelFactory.Create(name: "A", sponsorshipAmount: 1000m)
        ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.Select(l => l.Name).ShouldBe(["A", "B"]);
    }

    [Fact(DisplayName = "Should order breakdown sponsors with equal amounts alphabetically")]
    public void AddedMoneyBreakdown_ShouldOrderEqualAmountsAlphabetically_WhenAmountsTie()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(sponsors:
        [
            TournamentDetailSponsorViewModelFactory.Create(name: "Zed", sponsorshipAmount: 700m),
            TournamentDetailSponsorViewModelFactory.Create(name: "alpha", sponsorshipAmount: 700m),
            TournamentDetailSponsorViewModelFactory.Create(name: "Mid", sponsorshipAmount: 700m)
        ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.Select(l => l.Name).ShouldBe(["alpha", "Mid", "Zed"]);
    }

    [Fact(DisplayName = "Should leave a sponsor with a zero amount out of the breakdown")]
    public void AddedMoneyBreakdown_ShouldExcludeSponsor_WhenAmountIsZero()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(sponsors:
        [
            TournamentDetailSponsorViewModelFactory.Create(name: "Presenting", sponsorshipAmount: 0m),
            TournamentDetailSponsorViewModelFactory.Create(name: "Sponsor A", sponsorshipAmount: 1000m)
        ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.Select(l => l.Name).ShouldBe(["Sponsor A"]);
    }

    [Fact(DisplayName = "Should list NEBA last even when it contributed the most")]
    public void AddedMoneyBreakdown_ShouldListNebaLast_WhenNebaContributedTheMost()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(
            nebaAddedMoney: 1700m,
            sponsors:
            [
                TournamentDetailSponsorViewModelFactory.Create(name: "A", sponsorshipAmount: 1000m)
            ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.Select(l => l.Name).ShouldBe(["A", "NEBA"]);
        lines[^1].IsNeba.ShouldBeTrue();
        lines[^1].Amount.ShouldBe(1700m);
    }

    [Fact(DisplayName = "Should omit NEBA when its added money is zero")]
    public void AddedMoneyBreakdown_ShouldOmitNeba_WhenNebaAddedMoneyIsZero()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(
            nebaAddedMoney: 0m,
            sponsors:
            [
                TournamentDetailSponsorViewModelFactory.Create(name: "A", sponsorshipAmount: 1000m)
            ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.ShouldNotContain(l => l.IsNeba);
    }

    [Fact(DisplayName = "Should return an empty breakdown when no sponsor or NEBA money is attributed")]
    public void AddedMoneyBreakdown_ShouldBeEmpty_WhenNoMoneyIsAttributed()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(
            addedMoney: 1000m,
            nebaAddedMoney: 0m,
            sponsors: []);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should have breakdown lines that sum to the added money total")]
    public void AddedMoneyBreakdown_ShouldSumToAddedMoney_WhenAllContributionsAreListed()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(
            addedMoney: 2500m,
            nebaAddedMoney: 100m,
            sponsors:
            [
                TournamentDetailSponsorViewModelFactory.Create(name: "A", sponsorshipAmount: 1000m),
                TournamentDetailSponsorViewModelFactory.Create(name: "B", sponsorshipAmount: 700m),
                TournamentDetailSponsorViewModelFactory.Create(name: "C", sponsorshipAmount: 700m)
            ]);

        // Act
        var lines = model.AddedMoneyBreakdown;

        // Assert
        lines.Sum(l => l.Amount).ShouldBe(2500m);
    }

    [Fact(DisplayName = "Should show the champion badge only when winners exist and the tournament is title eligible")]
    public void ShowChampionBadge_ShouldBeTrueOnlyWhenWinnersExistAndTitleEligible_WhenEvaluated()
    {
        // Arrange
        var eligibleWithWinners = TournamentDetailViewModelFactory.Create() with
        {
            Winners = ["A Winner"],
            TitleEligible = true,
        };
        var ineligibleWithWinners = eligibleWithWinners with { TitleEligible = false };
        var eligibleWithoutWinners = eligibleWithWinners with { Winners = [] };

        // Assert
        eligibleWithWinners.ShowChampionBadge.ShouldBeTrue();
        ineligibleWithWinners.ShowChampionBadge.ShouldBeFalse();
        eligibleWithoutWinners.ShowChampionBadge.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should use the tournament's own logo URL when set")]
    public void DisplayLogoSrc_ShouldUseLogoUrl_WhenSet()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(
            tournamentType: "Doubles",
            logoUrl: new Uri("https://cdn.example.com/tournament-logo.png"));

        // Assert
        model.DisplayLogoSrc.ShouldBe("https://cdn.example.com/tournament-logo.png");
    }

    [Fact(DisplayName = "Should fall back to the format-specific default logo when no logo URL is set")]
    public void DisplayLogoSrc_ShouldUseFormatDefault_WhenLogoUrlIsNull()
    {
        // Arrange
        var model = TournamentDetailViewModelFactory.Create(tournamentType: "Doubles", logoUrl: null);

        // Assert
        model.DisplayLogoSrc.ShouldBe("/images/neba-doubles.jpg");
    }
}