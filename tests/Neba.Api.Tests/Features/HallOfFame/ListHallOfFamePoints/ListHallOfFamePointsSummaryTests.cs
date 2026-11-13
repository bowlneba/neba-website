using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFamePoints;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFamePointsSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary and description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Act
        var summary = new ListHallOfFamePointsSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register a 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        // Act
        var summary = new ListHallOfFamePointsSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
    }
}
