using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.RecomputeSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class RecomputeSeriesAdvancementSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new RecomputeSeriesAdvancementSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.EditSeries");
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404 and 409 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new RecomputeSeriesAdvancementSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 409]);
    }

    [Fact(DisplayName = "Constructor should give the request an example with a Version and no confirmation")]
    public void Constructor_ShouldGiveTheRequestAnExample()
    {
        // Arrange & Act
        var summary = new RecomputeSeriesAdvancementSummary();

        // Assert
        var example = summary.RequestExamples.Single().Value.ShouldBeOfType<RecomputeSeriesAdvancementRequest>();
        example.Id.Length.ShouldBe(26);
        example.StageId.Length.ShouldBe(26);
        example.Recomputation.Version.ShouldBe(3);
        example.Recomputation.ConfirmResetOfficial.ShouldBeFalse();
    }
}