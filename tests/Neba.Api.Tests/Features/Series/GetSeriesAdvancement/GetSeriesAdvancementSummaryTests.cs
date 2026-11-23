using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Features.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancementSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new GetSeriesAdvancementSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.EditSeries");
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, 403 and 404 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new GetSeriesAdvancementSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200, 400, 401, 403, 404]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example of the advancement response")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange & Act
        var summary = new GetSeriesAdvancementSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<SeriesAdvancementResponse>();
        example.Stages.ShouldNotBeEmpty();
        example.Stages[0].Kind.ShouldBe("Qualifying");
    }
}