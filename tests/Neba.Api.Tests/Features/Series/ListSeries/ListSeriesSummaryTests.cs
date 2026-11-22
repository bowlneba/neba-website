using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Features.Series.ListSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListSeries;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ListSeriesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ListSeriesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example with a Series and its next Stage")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange & Act
        var summary = new ListSeriesSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<CollectionResponse<SeriesSummaryResponse>>();
        var item = example.Items.Single();
        item.Id.Length.ShouldBe(26);
        item.NextStage.ShouldNotBeNull();
    }
}
