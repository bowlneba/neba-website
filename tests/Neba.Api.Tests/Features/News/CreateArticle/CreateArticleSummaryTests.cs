using Neba.Api.Features.News.CreateArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.CreateArticle;

[UnitTest]
[Component("News")]
public sealed class CreateArticleSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new CreateArticleSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new CreateArticleSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 201, 401, 403, 409, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new CreateArticleSummary();

        // Assert
        summary.Responses.ShouldContainKey(201);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}
