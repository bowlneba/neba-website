using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Features.Series.GetSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeries;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new GetSeriesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new GetSeriesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200, 404]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example with a Finale and Stages")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange & Act
        var summary = new GetSeriesSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<SeriesDetailResponse>();
        example.Id.Length.ShouldBe(26);
        example.Finale.ShouldNotBeNull();
        example.Stages.ShouldNotBeEmpty();
    }
}