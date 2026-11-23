using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Features.Series.ListSeriesFinales;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListSeriesFinales;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesFinalesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ListSeriesFinalesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ListSeriesFinalesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example with a Finale and its host center")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange & Act
        var summary = new ListSeriesFinalesSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<CollectionResponse<SeriesFinaleSummaryResponse>>();
        var item = example.Items.Single();
        item.SeriesId.Length.ShouldBe(26);
        item.Finale.BowlingCenterName.ShouldNotBeNullOrWhiteSpace();
    }
}