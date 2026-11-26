using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[UnitTest]
[Component("Tournaments")]
public sealed class ListEligibleBowlersSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary and description")]
    public void Constructor_ShouldSetSummaryAndDescription_WhenConstructed()
    {
        // Arrange & Act
        var summary = new ListEligibleBowlersSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set an example request")]
    public void Constructor_ShouldSetExampleRequest_WhenConstructed()
    {
        // Arrange & Act
        var summary = new ListEligibleBowlersSummary();

        // Assert
        summary.ExampleRequest.ShouldBeOfType<ListEligibleBowlersRequest>();
    }

    [Theory(DisplayName = "Constructor should register a response for each status code")]
    [InlineData(200, TestDisplayName = "200")]
    [InlineData(400, TestDisplayName = "400")]
    [InlineData(404, TestDisplayName = "404")]
    public void Constructor_ShouldRegisterResponse_WhenConstructed(int statusCode)
    {
        // Arrange & Act
        var summary = new ListEligibleBowlersSummary();

        // Assert
        summary.Responses.ShouldContainKey(statusCode);
    }
}