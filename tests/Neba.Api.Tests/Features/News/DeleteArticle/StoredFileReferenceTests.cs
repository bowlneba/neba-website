using Neba.Api.Features.News.DeleteArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class StoredFileReferenceTests
{
    [Fact(DisplayName = "Properties should return the values assigned at construction")]
    public void Properties_ShouldReturnAssignedValues()
    {
        // Arrange & Act
        var reference = new StoredFileReference { Container = "articles", Path = "articles/attachment.png" };

        // Assert
        reference.Container.ShouldBe("articles");
        reference.Path.ShouldBe("articles/attachment.png");
    }

    [Fact(DisplayName = "Records with the same values should be equal")]
    public void Records_ShouldBeEqual_WhenValuesMatch()
    {
        // Arrange
        var first = new StoredFileReference { Container = "articles", Path = "articles/attachment.png" };
        var second = new StoredFileReference { Container = "articles", Path = "articles/attachment.png" };

        // Act & Assert
        first.ShouldBe(second);
    }
}
