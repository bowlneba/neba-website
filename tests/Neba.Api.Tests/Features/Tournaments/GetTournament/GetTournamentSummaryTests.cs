using Neba.Api.Features.Tournaments.GetTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that mentions the Series")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new GetTournamentSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series the tournament is a Stage of");
    }

    [Fact(DisplayName = "Constructor should register the 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        // Arrange & Act
        var summary = new GetTournamentSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
    }
}