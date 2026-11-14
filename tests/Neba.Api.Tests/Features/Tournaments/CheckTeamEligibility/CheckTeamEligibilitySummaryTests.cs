using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

[UnitTest]
[Component("Tournaments")]
public sealed class CheckTeamEligibilitySummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary and description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new CheckTeamEligibilitySummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "Constructor should register a response for each status code")]
    [InlineData(200, TestDisplayName = "200")]
    [InlineData(400, TestDisplayName = "400")]
    [InlineData(404, TestDisplayName = "404")]
    [InlineData(422, TestDisplayName = "422")]
    public void Constructor_ShouldRegisterResponse(int statusCode)
    {
        // Arrange & Act
        var summary = new CheckTeamEligibilitySummary();

        // Assert
        summary.Responses.ShouldContainKey(statusCode);
    }
}