using Neba.Api.Documents;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Documents;

[UnitTest]
[Component("Documents")]
public sealed class DocumentStorageTests
{
    [Fact(DisplayName = "Container should be bowlneba-private")]
    public void Container_ShouldBeBowlnebaPrivate()
    {
        // Assert
        DocumentStorage.Container.ShouldBe("bowlneba-private");
    }

    [Theory(DisplayName = "BlobName should prefix the document name with documents/")]
    [InlineData("bylaws", "documents/bylaws", TestDisplayName = "bylaws")]
    [InlineData("tournament-rules", "documents/tournament-rules", TestDisplayName = "tournament-rules")]
    public void BlobName_ShouldPrefixDocumentName(string documentName, string expected)
    {
        // Act
        var blobName = DocumentStorage.BlobName(documentName);

        // Assert
        blobName.ShouldBe(expected);
    }
}