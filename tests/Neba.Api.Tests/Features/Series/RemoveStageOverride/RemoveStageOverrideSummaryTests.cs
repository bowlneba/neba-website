using Neba.Api.Features.Series.RemoveStageOverride;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.RemoveStageOverride;

[UnitTest]
[Component("Series")]
public sealed class RemoveStageOverrideSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission and the missing-override rule")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new RemoveStageOverrideSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.ManageAdvancement");
        summary.Description.ShouldContain("without checking the Version");
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404 and 409 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new RemoveStageOverrideSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 409]);
    }
}