using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleCommandTests
{
    [Fact(DisplayName = "ArticleId should return the value assigned at construction")]
    public void ArticleId_ShouldReturnAssignedValue()
    {
        // Arrange
        var articleId = ArticleId.New();

        // Act
        var command = new DeleteArticleCommand { ArticleId = articleId };

        // Assert
        command.ArticleId.ShouldBe(articleId);
    }
}