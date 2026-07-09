using Neba.Api.Features.News.DeleteArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleFilesJobTests
{
    [Fact(DisplayName = "JobName should format the file count")]
    public void JobName_ShouldFormatFileCount()
    {
        // Arrange
        var job = new DeleteArticleFilesJob
        {
            Files =
            [
                new StoredFileReference { Container = "articles", Path = "one.png" },
                new StoredFileReference { Container = "articles", Path = "two.png" },
            ]
        };

        // Act & Assert
        job.JobName.ShouldBe("DeleteArticleFilesJob: 2 file(s)");
    }

    [Fact(DisplayName = "JobName should format zero files")]
    public void JobName_ShouldFormatZeroFiles_WhenFilesIsEmpty()
    {
        // Arrange
        var job = new DeleteArticleFilesJob { Files = [] };

        // Act & Assert
        job.JobName.ShouldBe("DeleteArticleFilesJob: 0 file(s)");
    }
}
