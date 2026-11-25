using Neba.Api.Contracts.Series.GetSeriesAdvancers;
using Neba.Api.Features.Series.GetSeriesAdvancers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancers;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancersSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that says the endpoint is public")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange

        // Act
        var summary = new GetSeriesAdvancersSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Public");
    }

    [Fact(DisplayName = "Constructor should register 200, 400 and 404 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange

        // Act
        var summary = new GetSeriesAdvancersSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200, 400, 404]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example of the advancers response")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange

        // Act
        var summary = new GetSeriesAdvancersSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<SeriesAdvancersResponse>();
        example.Stages.ShouldNotBeEmpty();
        example.Stages[0].Kind.ShouldBe("Qualifying");
    }
}