using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Features.Series.ListStageResults;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListStageResults;

[UnitTest]
[Component("Series")]
public sealed class ListStageResultsSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ListStageResultsSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.ManageAdvancement");
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, 403 and 404 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ListStageResultsSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200, 400, 401, 403, 404]);
    }

    [Fact(DisplayName = "Constructor should give the 200 response an example collection with one item")]
    public void Constructor_ShouldGiveTheOkResponseAnExample()
    {
        // Arrange & Act
        var summary = new ListStageResultsSummary();

        // Assert
        var example = SummaryExamples.For(summary, 200).ShouldBeOfType<CollectionResponse<StageResultSummaryResponse>>();
        example.Items.Count.ShouldBe(1);
        example.Items.Single().Place.ShouldBe(1);
    }
}