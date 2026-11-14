using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamEligibilityMappingExtensions")]
public sealed class TeamEligibilityMappingExtensionsTests
{
    [Fact(DisplayName = "Maps the outcome, summary, reasons and checks of a response")]
    public void ToViewModel_ShouldMapResponseFields()
    {
        // Arrange
        var response = TeamEligibilityResponseFactory.Create(
            summary: "Two champions, one allowed.",
            professionalTitlesVerified: true,
            reasons: [new TeamEligibilityReasonResponse { Code = "A", Message = "First reason" }],
            checks: [new TeamEligibilityCheckResponse { Label = "Titles", Passed = false }]);

        // Act
        var viewModel = response.ToViewModel(TeamEligibilityOutcomeKind.Ineligible);

        // Assert
        viewModel.Outcome.ShouldBe(TeamEligibilityOutcomeKind.Ineligible);
        viewModel.Summary.ShouldBe("Two champions, one allowed.");
        viewModel.ProfessionalTitlesVerified.ShouldBeTrue();
        viewModel.Reasons.ShouldBe(["First reason"]);
        viewModel.Checks.Count.ShouldBe(1);
        viewModel.Checks[0].Label.ShouldBe("Titles");
        viewModel.Checks[0].Passed.ShouldBeFalse();
    }

    [Fact(DisplayName = "Maps date checks with only the reason messages")]
    public void ToViewModel_ShouldMapDateChecks()
    {
        // Arrange
        var asOf = new DateOnly(2026, 3, 7);
        var response = TeamEligibilityResponseFactory.Create(
            dateChecks:
            [
                new TeamEligibilityDateCheckResponse
                {
                    AsOf = asOf,
                    Eligible = true,
                    Reasons = [new TeamEligibilityReasonResponse { Code = "B", Message = "Second reason" }]
                }
            ]);

        // Act
        var viewModel = response.ToViewModel(TeamEligibilityOutcomeKind.DependsOnDate);

        // Assert
        viewModel.DateChecks.Count.ShouldBe(1);
        viewModel.DateChecks[0].AsOf.ShouldBe(asOf);
        viewModel.DateChecks[0].Eligible.ShouldBeTrue();
        viewModel.DateChecks[0].Reasons.ShouldBe(["Second reason"]);
    }

    [Fact(DisplayName = "Maps the defending source to a team with no champions listed")]
    public void ToViewModel_ShouldMapDefendingSource_WhenPresent()
    {
        // Arrange
        var response = TeamEligibilityResponseFactory.Create(
            defendingSource: new DefendingSourceResponse
            {
                TournamentName = "Joe's Pizza Open",
                SeasonDescription = "2025 Season",
                SourceTruncated = true,
                Undefended = true
            });

        // Act
        var viewModel = response.ToViewModel(TeamEligibilityOutcomeKind.DefendingChampions);

        // Assert
        viewModel.DefendingSource.ShouldNotBeNull();
        viewModel.DefendingSource.TournamentName.ShouldBe("Joe's Pizza Open");
        viewModel.DefendingSource.SeasonDescription.ShouldBe("2025 Season");
        viewModel.DefendingSource.SourceTruncated.ShouldBeTrue();
        viewModel.DefendingSource.Undefended.ShouldBeTrue();
        viewModel.DefendingSource.Champions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Leaves the defending source null when the response has none")]
    public void ToViewModel_ShouldLeaveDefendingSourceNull_WhenAbsent()
    {
        // Arrange
        var response = TeamEligibilityResponseFactory.Create(defendingSource: null);

        // Act
        var viewModel = response.ToViewModel(TeamEligibilityOutcomeKind.Eligible);

        // Assert
        viewModel.DefendingSource.ShouldBeNull();
    }

    [Fact(DisplayName = "Maps every field of a member")]
    public void ToViewModel_ShouldMapMemberFields()
    {
        // Arrange
        var member = TeamEligibilityMemberResponseFactory.Create(
            name: "Dana Jones",
            usbcId: "1234-56789",
            notInSystem: false,
            lastTitleYear: 2023,
            titlesLast5Years: 2,
            titlesLast10Years: 3,
            countsAs: "MultiTimeChampion",
            ageVerified: true,
            hallOfFameSuperiorPerformance: false,
            titlesBreakRule: true,
            ageBreaksRule: true,
            hallOfFameBreaksRule: true);

        // Act
        var viewModel = member.ToViewModel();

        // Assert
        viewModel.Name.ShouldBe("Dana Jones");
        viewModel.UsbcId.ShouldBe("1234-56789");
        viewModel.NotInSystem.ShouldBeFalse();
        viewModel.LastTitleYear.ShouldBe(2023);
        viewModel.TitlesLast5Years.ShouldBe(2);
        viewModel.TitlesLast10Years.ShouldBe(3);
        viewModel.CountsAs.ShouldBe(ChampionStandingKind.MultiTimeChampion);
        viewModel.AgeVerified.ShouldBe(true);
        viewModel.HallOfFameSuperiorPerformance.ShouldBe(false);
        viewModel.TitlesBreakRule.ShouldBeTrue();
        viewModel.AgeBreaksRule.ShouldBeTrue();
        viewModel.HallOfFameBreaksRule.ShouldBeTrue();
    }

    [Theory(DisplayName = "ParseStanding reads a known standing and returns null for anything else")]
    [InlineData("NonChampion", ChampionStandingKind.NonChampion, TestDisplayName = "Non-champion")]
    [InlineData("OneTimeChampion", ChampionStandingKind.OneTimeChampion, TestDisplayName = "One-time champion")]
    [InlineData("MultiTimeChampion", ChampionStandingKind.MultiTimeChampion, TestDisplayName = "Multi-time champion")]
    [InlineData("SuperChampion", null, TestDisplayName = "Unknown name")]
    [InlineData("99", null, TestDisplayName = "Number outside the enum")]
    [InlineData("", null, TestDisplayName = "Empty")]
    public void ParseStanding_ShouldReturnStanding_WhenKnown(string countsAs, ChampionStandingKind? expected)
    {
        // Arrange
        var member = TeamEligibilityMemberResponseFactory.Create(countsAs: countsAs);

        // Act
        var standing = member.ParseStanding();

        // Assert
        standing.ShouldBe(expected);
    }

    [Fact(DisplayName = "A member with an unknown standing counts as a non-champion")]
    public void ToViewModel_ShouldCountAsNonChampion_WhenStandingUnknown()
    {
        // Arrange
        var member = TeamEligibilityMemberResponseFactory.Create(countsAs: "SuperChampion");

        // Act
        var viewModel = member.ToViewModel();

        // Assert
        viewModel.CountsAs.ShouldBe(ChampionStandingKind.NonChampion);
    }

    [Fact(DisplayName = "Maps a bowler search result")]
    public void ToViewModel_ShouldMapBowlerSearchResponse()
    {
        // Arrange
        var response = BowlerSearchResponseFactory.Create(bowlerId: "abc", name: "Dana Jones", usbcId: null);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.BowlerId.ShouldBe("abc");
        viewModel.Name.ShouldBe("Dana Jones");
        viewModel.UsbcId.ShouldBeNull();
    }
}