using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that mentions the Series")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ListTournamentsInSeasonSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series each tournament is a Stage of");
    }

    [Fact(DisplayName = "Constructor should register the 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        // Arrange & Act
        var summary = new ListTournamentsInSeasonSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
    }
}
