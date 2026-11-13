using Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.GetHallOfFamePointsDetail;

[UnitTest]
[Component("HallOfFame")]
public sealed class GetHallOfFamePointsDetailSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary and description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Act
        var summary = new GetHallOfFamePointsDetailSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "Constructor should register the documented responses")]
    [InlineData(200, TestDisplayName = "200 response")]
    [InlineData(400, TestDisplayName = "400 response")]
    [InlineData(404, TestDisplayName = "404 response")]
    public void Constructor_ShouldRegisterResponse(int statusCode)
    {
        // Act
        var summary = new GetHallOfFamePointsDetailSummary();

        // Assert
        summary.Responses.ShouldContainKey(statusCode);
    }
}
