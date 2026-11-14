using Neba.Api.Features.Bowlers.SearchBowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.SearchBowlers;

[UnitTest]
[Component("Bowlers")]
public sealed class SearchBowlersSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary, description and example request")]
    public void Constructor_ShouldSetSummaryDescriptionAndExample()
    {
        // Arrange & Act
        var summary = new SearchBowlersSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.RequestExamples.ShouldNotBeEmpty();
    }

    [Theory(DisplayName = "Constructor should register a response for each status code")]
    [InlineData(200, TestDisplayName = "200")]
    [InlineData(400, TestDisplayName = "400")]
    [InlineData(429, TestDisplayName = "429")]
    public void Constructor_ShouldRegisterResponse(int statusCode)
    {
        // Arrange & Act
        var summary = new SearchBowlersSummary();

        // Assert
        summary.Responses.ShouldContainKey(statusCode);
    }
}
