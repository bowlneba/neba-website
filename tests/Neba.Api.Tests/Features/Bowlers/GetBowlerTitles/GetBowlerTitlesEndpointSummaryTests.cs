using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[UnitTest]
[Component("Bowlers")]
public sealed class GetBowlerTitlesEndpointSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new GetBowlerTitlesEndpointSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new GetBowlerTitlesEndpointSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register a 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        // Arrange & Act
        var summary = new GetBowlerTitlesEndpointSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
    }

    [Fact(DisplayName = "Constructor should register a 400 response")]
    public void Constructor_ShouldRegisterBadRequestResponse()
    {
        // Arrange & Act
        var summary = new GetBowlerTitlesEndpointSummary();

        // Assert
        summary.Responses.ShouldContainKey(400);
    }

    [Fact(DisplayName = "Constructor should register a 404 response")]
    public void Constructor_ShouldRegisterNotFoundResponse()
    {
        // Arrange & Act
        var summary = new GetBowlerTitlesEndpointSummary();

        // Assert
        summary.Responses.ShouldContainKey(404);
    }
}