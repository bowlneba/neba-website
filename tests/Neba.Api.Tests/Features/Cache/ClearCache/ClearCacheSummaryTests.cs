using Neba.Api.Features.Cache.ClearCache;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Cache.ClearCache;

[UnitTest]
[Component("Cache")]
public sealed class ClearCacheSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new ClearCacheSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new ClearCacheSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 401, and 403 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ClearCacheSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
    }
}