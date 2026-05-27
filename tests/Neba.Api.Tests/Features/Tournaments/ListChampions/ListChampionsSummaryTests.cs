using Neba.Api.Features.Tournaments.ListChampions;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListChampions;

[UnitTest]
[Component("Tournaments")]
public sealed class ListChampionsSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new ListChampionsSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new ListChampionsSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register a 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        // Arrange & Act
        var summary = new ListChampionsSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
    }
}