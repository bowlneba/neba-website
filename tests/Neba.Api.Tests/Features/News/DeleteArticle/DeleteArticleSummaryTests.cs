using Neba.Api.Features.News.DeleteArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new DeleteArticleSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new DeleteArticleSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 401, and 403 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new DeleteArticleSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
    }
}
