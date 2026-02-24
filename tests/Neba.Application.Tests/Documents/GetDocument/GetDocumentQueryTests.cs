using Neba.Application.Documents.GetDocument;
using Neba.TestFactory.Attributes;

using Shouldly;

namespace Neba.Application.Tests.Documents.GetDocument;

[UnitTest]
[Component("Documents")]
public sealed class GetDocumentQueryTests
{
    [Fact(DisplayName = "Expiry should be 7 days")]
    public void Expiry_ShouldBe7Days()
    {
        var query = new GetDocumentQuery { DocumentName = "bylaws" };

        query.Expiry.ShouldBe(TimeSpan.FromDays(7));
    }

    [Theory(DisplayName = "Cache key should follow neba:document:{name}:content format")]
    [InlineData("bylaws")]
    [InlineData("constitution")]
    [InlineData("high-average")]
    public void Cache_Key_ShouldFollowExpectedFormat(string documentName)
    {
        var query = new GetDocumentQuery { DocumentName = documentName };

        query.Cache.Key.ShouldBe($"neba:document:{documentName}:content");
    }

    [Theory(DisplayName = "Cache tags should include category tag and specific document tag")]
    [InlineData("bylaws")]
    [InlineData("constitution")]
    public void Cache_Tags_ShouldIncludeCategoryAndSpecificTags(string documentName)
    {
        var query = new GetDocumentQuery { DocumentName = documentName };

        query.Cache.Tags.ShouldContain("neba:documents");
        query.Cache.Tags.ShouldContain($"neba:document:{documentName}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        var query = new GetDocumentQuery { DocumentName = "bylaws" };

        query.Cache.Tags.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Cache descriptor key should be specific to document name")]
    public void Cache_Key_ShouldBeSpecificToDocumentName()
    {
        var bylaws = new GetDocumentQuery { DocumentName = "bylaws" };
        var constitution = new GetDocumentQuery { DocumentName = "constitution" };

        bylaws.Cache.Key.ShouldNotBe(constitution.Cache.Key);
    }
}