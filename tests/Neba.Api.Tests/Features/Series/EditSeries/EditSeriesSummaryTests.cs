using Neba.Api.Features.Series.EditSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.EditSeries;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new EditSeriesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.EditSeries");
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new EditSeriesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 409, 422]);
    }
}