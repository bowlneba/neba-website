using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentDetailViewModel")]
public sealed class TournamentDetailViewModelTests
{
    [Fact(DisplayName = "Should report multiple money sources only when sponsor money and NEBA added money are both positive")]
    public void HasMultipleMoneySources_ShouldRequireBothSponsorAndNebaMoney_WhenEvaluated()
    {
        // Arrange
        var both = TournamentDetailViewModelFactory.Create() with
        {
            SponsorMoney = 1000m,
            NebaAddedMoney = 500m,
        };
        var sponsorOnly = both with { NebaAddedMoney = 0m };
        var nebaOnly = both with { SponsorMoney = 0m };
        var neither = both with { SponsorMoney = 0m, NebaAddedMoney = 0m };

        // Assert
        both.HasMultipleMoneySources.ShouldBeTrue();
        sponsorOnly.HasMultipleMoneySources.ShouldBeFalse();
        nebaOnly.HasMultipleMoneySources.ShouldBeFalse();
        neither.HasMultipleMoneySources.ShouldBeFalse();
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