using Neba.Application.Documents.SyncDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Documents.SyncDocument;

[UnitTest]
[Component("Documents")]
public sealed class SyncDocumentToStorageJobTests
{
    [Fact(DisplayName = "JobName should format document name with sync prefix")]
    public void JobName_ShouldFormatCorrectly_WithDocumentName()
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = "bylaws",
            TriggeredBy = "scheduled"
        };

        // Act & Assert
        job.JobName.ShouldBe("SyncDocumentToStorage: bylaws");
    }

    [Theory(DisplayName = "JobName should include the configured document name")]
    [InlineData("bylaws", "SyncDocumentToStorage: bylaws")]
    [InlineData("tournament-rules", "SyncDocumentToStorage: tournament-rules")]
    [InlineData("officer-handbook", "SyncDocumentToStorage: officer-handbook")]
    public void JobName_ShouldIncludeDocumentName_ForAnyDocument(string documentName, string expectedJobName)
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = documentName,
            TriggeredBy = "scheduled"
        };

        // Act & Assert
        job.JobName.ShouldBe(expectedJobName);
    }
}