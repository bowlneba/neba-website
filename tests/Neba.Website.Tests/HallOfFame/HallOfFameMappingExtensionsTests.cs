using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameMappingExtensions")]
public sealed class HallOfFameMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModel_ShouldMapAllFields()
    {
        // Arrange
        var responses = HallOfFameInductionResponseFactory.Bogus(3, seed: 1);

        // Act
        var viewModels = responses.Select(r => r.ToViewModel()).ToList();

        // Assert
        await Verify(viewModels);
    }

    [Fact(DisplayName = "Orders categories in display order regardless of the order the API sent them")]
    public void ToViewModel_ShouldOrderCategories_InDisplayOrder()
    {
        // Arrange
        var response = HallOfFameInductionResponseFactory.Create(
            categories: ["Friend of NEBA", "Superior Performance", "Meritorious Service"]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.Categories.ShouldBe(["Superior Performance", "Meritorious Service", "Friend of NEBA"]);
    }

    [Fact(DisplayName = "Splits standings into eligible and not yet eligible and keeps the API order")]
    public void ToViewModel_ShouldSplitStandingsByEligible()
    {
        // Arrange
        var response = HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Ray Dunleavy", totalPoints: 42, eligible: true),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Ken Almeida", totalPoints: 42, eligible: false),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Marcus Hale", totalPoints: 36, eligible: true),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis", totalPoints: 12, eligible: false)
        ]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.Eligible.Select(bowler => bowler.BowlerName).ShouldBe(["Ray Dunleavy", "Marcus Hale"]);
        viewModel.NotYetEligible.Select(bowler => bowler.BowlerName).ShouldBe(["Ken Almeida", "Tom Davis"]);
    }

    [Fact(DisplayName = "Works out seasons needed, membership years needed, and the line position")]
    public void ToViewModel_ShouldDeriveNeeds_FromRequirements()
    {
        // Arrange
        var response = HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "A", totalPoints: 18, seasonsWithPoints: 2, membershipYears: 8),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "B", totalPoints: 42, seasonsWithPoints: 5, membershipYears: 12)
        ]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        var low = viewModel.NotYetEligible.Single(bowler => bowler.BowlerName == "A");
        low.SeasonsNeeded.ShouldBe(1);
        low.MembershipYearsNeeded.ShouldBe(2);
        low.MeetsMembershipYears.ShouldBeFalse();
        low.LinePercent.ShouldBe(50);

        var high = viewModel.NotYetEligible.Single(bowler => bowler.BowlerName == "B");
        high.SeasonsNeeded.ShouldBe(0);
        high.MembershipYearsNeeded.ShouldBe(0);
        high.MeetsMembershipYears.ShouldBeTrue();
        high.LinePercent.ShouldBe(100);
    }

    [Fact(DisplayName = "Carries the requirements from the response")]
    public void ToViewModel_ShouldCarryRequirements()
    {
        // Arrange
        var response = HallOfFamePointsResponseFactory.Create(
            requiredPoints: 40, requiredSeasonsWithPoints: 4, requiredMembershipYears: 12, membershipDataStartYear: 2001);

        // Act
        var requirements = response.ToViewModel().Requirements;

        // Assert
        requirements.Points.ShouldBe(40);
        requirements.Seasons.ShouldBe(4);
        requirements.MembershipYears.ShouldBe(12);
        requirements.MembershipDataStartYear.ShouldBe(2001);
    }

    [Fact(DisplayName = "Builds the inducted line in display order")]
    public void ToViewModel_ShouldBuildInductedLine()
    {
        // Arrange
        var response = HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Both", inductedCategories: ["Friend of NEBA", "Meritorious Service"]),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "None", inductedCategories: [])
        ]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.NotYetEligible.Single(bowler => bowler.BowlerName == "Both").InductedLine
            .ShouldBe("In the Hall of Fame for Meritorious Service and Friend of NEBA");
        viewModel.NotYetEligible.Single(bowler => bowler.BowlerName == "None").InductedLine.ShouldBeNull();
    }

    [Fact(DisplayName = "Groups rules by group in the order sent")]
    public void ToViewModel_ShouldGroupRules()
    {
        // Arrange
        var response = HallOfFamePointsResponseFactory.Create(rules:
        [
            new HallOfFamePointRuleResponse { Group = "Awards", Label = "Bowler of the Year", Points = 12 },
            new HallOfFamePointRuleResponse { Group = "Titles", Label = "Singles Title", Points = 6 },
            new HallOfFamePointRuleResponse { Group = "Awards", Label = "Rookie of the Year", Points = 2 }
        ]);

        // Act
        var groups = response.ToViewModel().RuleGroups;

        // Assert
        groups.Select(group => group.Name).ShouldBe(["Awards", "Titles"]);
        groups.First().Rules.Select(rule => (rule.Label, rule.Points))
            .ShouldBe([("Bowler of the Year", 12), ("Rookie of the Year", 2)]);
    }

    [Fact(DisplayName = "Maps the detail response, taking the year from each breakdown date")]
    public async Task DetailToViewModel_ShouldMapAllFields()
    {
        // Arrange
        var responses = HallOfFamePointsDetailResponseFactory.Bogus(2, seed: 3);

        // Act
        var viewModels = responses.Select(response => response.ToViewModel()).ToList();

        // Assert
        await Verify(viewModels);
    }
}