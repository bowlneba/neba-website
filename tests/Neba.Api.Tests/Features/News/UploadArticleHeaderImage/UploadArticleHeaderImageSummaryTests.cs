using Neba.Api.Features.News.UploadArticleHeaderImage;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.UploadArticleHeaderImage;

[UnitTest]
[Component("News")]
public sealed class UploadArticleHeaderImageSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new UploadArticleHeaderImageSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new UploadArticleHeaderImageSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, and 403 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new UploadArticleHeaderImageSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
    }
}