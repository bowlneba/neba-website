using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamEligibilityViewModels")]
public sealed class TeamEligibilityViewModelTests
{
    private static TeamEligibilityDateCheckViewModel Check(bool eligible)
        => new(new DateOnly(2026, 3, 7), eligible, []);

    [Theory(DisplayName = "ShowProfessionalTitlesNote is true only when the team passed on NEBA data")]
    [InlineData(TeamEligibilityOutcomeKind.Eligible, false, true, TestDisplayName = "Eligible")]
    [InlineData(TeamEligibilityOutcomeKind.Ineligible, false, false, TestDisplayName = "Ineligible")]
    [InlineData(TeamEligibilityOutcomeKind.DefendingChampions, false, false, TestDisplayName = "Defending champions")]
    [InlineData(TeamEligibilityOutcomeKind.Eligible, true, false, TestDisplayName = "Eligible, pro titles verified")]
    public void ShowProfessionalTitlesNote_ShouldFollowOutcome(TeamEligibilityOutcomeKind outcome, bool verified, bool expected)
    {
        // Arrange
        var result = new TeamEligibilityResultViewModel { Outcome = outcome, ProfessionalTitlesVerified = verified };

        // Act & Assert
        result.ShowProfessionalTitlesNote.ShouldBe(expected);
    }

    [Theory(DisplayName = "ShowProfessionalTitlesNote for DependsOnDate needs at least one date where the team passes")]
    [InlineData(true, false, true, TestDisplayName = "Passes on one date")]
    [InlineData(false, true, true, TestDisplayName = "Passes on the other date")]
    [InlineData(false, false, false, TestDisplayName = "Passes on neither")]
    public void ShowProfessionalTitlesNote_ShouldNeedAPassingDate_WhenDependsOnDate(bool first, bool second, bool expected)
    {
        // Arrange
        var result = new TeamEligibilityResultViewModel
        {
            Outcome = TeamEligibilityOutcomeKind.DependsOnDate,
            DateChecks = [Check(first), Check(second)]
        };

        // Act & Assert
        result.ShowProfessionalTitlesNote.ShouldBe(expected);
    }

    [Theory(DisplayName = "BowlerSearchResultViewModel shows the USBC ID in brackets when there is one")]
    [InlineData("4821-09317", "Dave Smith (4821-09317)", TestDisplayName = "With a USBC ID")]
    [InlineData(null, "Dave Smith", TestDisplayName = "No USBC ID")]
    [InlineData("  ", "Dave Smith", TestDisplayName = "Blank USBC ID")]
    public void DisplayText_ShouldIncludeUsbcId_WhenPresent(string? usbcId, string expected)
    {
        // Arrange
        var bowler = new BowlerSearchResultViewModel("id", "Dave Smith", usbcId);

        // Act & Assert
        bowler.DisplayText.ShouldBe(expected);
    }

    [Theory(DisplayName = "DefendingTeamViewModel notes a truncated tournament or a team that could not defend")]
    [InlineData(false, false, null, TestDisplayName = "Normal team")]
    [InlineData(true, false, "tournament truncated", TestDisplayName = "Truncated source")]
    [InlineData(false, true, "could not defend", TestDisplayName = "Undefended")]
    [InlineData(true, true, "tournament truncated", TestDisplayName = "Truncated wins the note")]
    public void Note_ShouldDescribeWhyTheTeamIsListed(bool truncated, bool undefended, string? expected)
    {
        // Arrange
        var team = new DefendingTeamViewModel("Joe's Open", "2025 Season", truncated, undefended, ["Dave Smith"]);

        // Act & Assert
        team.Note.ShouldBe(expected);
    }
}
