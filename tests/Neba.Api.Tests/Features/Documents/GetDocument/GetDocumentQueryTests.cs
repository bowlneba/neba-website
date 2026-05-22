using Neba.Api.Features.Documents.GetDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Documents.GetDocument;

[UnitTest]
[Component("Documents")]
public sealed class GetDocumentQueryTests
{
    [Fact(DisplayName = "Expiry should be 7 days")]
    public void Expiry_ShouldBe7Days()
    {
        // Act
        var query = new GetDocumentQuery { DocumentName = "bylaws" };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(7));
    }

    [Theory(DisplayName = "Cache key should follow neba:document:{name}:content format")]
    [InlineData("bylaws")]
    [InlineData("constitution")]
    [InlineData("high-average")]
    public void Cache_Key_ShouldFollowExpectedFormat(string documentName)
    {
        // Act
        var query = new GetDocumentQuery { DocumentName = documentName };

        // Assert
        query.Cache.Key.ShouldBe($"neba:document:{documentName}:content");
    }

    [Theory(DisplayName = "Cache tags should include category tag and specific document tag")]
    [InlineData("bylaws")]
    [InlineData("constitution")]
    public void Cache_Tags_ShouldIncludeCategoryAndSpecificTags(string documentName)
    {
        // Act
        var query = new GetDocumentQuery { DocumentName = documentName };

        // Assert
        query.Cache.Tags.ShouldContain("neba:documents");
        query.Cache.Tags.ShouldContain($"neba:document:{documentName}");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new GetDocumentQuery { DocumentName = "bylaws" };

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new GetDocumentQuery { DocumentName = "bylaws" };

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Cache descriptor key should be specific to document name")]
    public void Cache_Key_ShouldBeSpecificToDocumentName()
    {
        // Act
        var bylaws = new GetDocumentQuery { DocumentName = "bylaws" };
        var constitution = new GetDocumentQuery { DocumentName = "constitution" };

        // Assert
        bylaws.Cache.Key.ShouldNotBe(constitution.Cache.Key);
    }
}
