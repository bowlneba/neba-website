using Neba.Api.Features.Series.DeleteSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.DeleteSeries;

[UnitTest]
[Component("Series")]
public sealed class DeleteSeriesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new DeleteSeriesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.DeleteSeries");
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new DeleteSeriesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 409]);
    }
}