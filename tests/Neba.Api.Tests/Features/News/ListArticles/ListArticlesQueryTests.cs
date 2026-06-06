using Neba.Api.Features.News.ListArticles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.ListArticles;

[UnitTest]
[Component("News")]
public sealed class ListArticlesQueryTests
{
    [Fact(DisplayName = "Expiry should be 45 minutes")]
    public void Expiry_ShouldBe45Minutes()
    {
        // Arrange
        var query = new ListArticlesQuery { Page = 1, PageSize = 10 };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBe(TimeSpan.FromMinutes(45));
    }

    [Theory(DisplayName = "Cache key should embed page and page size")]
    [InlineData(1, 10, "neba:news:articles:list:page:1:size:10")]
    [InlineData(2, 10, "neba:news:articles:list:page:2:size:10")]
    [InlineData(1, 25, "neba:news:articles:list:page:1:size:25")]
    public void Cache_Key_ShouldEmbedPageAndPageSize(int page, int pageSize, string expectedKey)
    {
        // Arrange
        var query = new ListArticlesQuery { Page = page, PageSize = pageSize };

        // Act
        var key = query.Cache.Key;

        // Assert
        key.ShouldBe(expectedKey);
    }

    [Fact(DisplayName = "Cache tags should contain neba:news:articles")]
    public void Cache_Tags_ShouldContainArticlesTag()
    {
        // Arrange
        var query = new ListArticlesQuery { Page = 1, PageSize = 10 };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba:news:articles");
    }

    [Fact(DisplayName = "Cache tags should contain neba:news")]
    public void Cache_Tags_ShouldContainNewsCategoryTag()
    {
        // Arrange
        var query = new ListArticlesQuery { Page = 1, PageSize = 10 };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba:news");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Arrange
        var query = new ListArticlesQuery { Page = 1, PageSize = 10 };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Arrange
        var query = new ListArticlesQuery { Page = 1, PageSize = 10 };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Different pages should produce different cache keys")]
    public void Cache_Key_ShouldDifferByPage()
    {
        // Arrange
        var page1 = new ListArticlesQuery { Page = 1, PageSize = 10 };
        var page2 = new ListArticlesQuery { Page = 2, PageSize = 10 };

        // Act + Assert
        page1.Cache.Key.ShouldNotBe(page2.Cache.Key);
    }

    [Fact(DisplayName = "Different page sizes should produce different cache keys")]
    public void Cache_Key_ShouldDifferByPageSize()
    {
        // Arrange
        var size10 = new ListArticlesQuery { Page = 1, PageSize = 10 };
        var size25 = new ListArticlesQuery { Page = 1, PageSize = 25 };

        // Act + Assert
        size10.Cache.Key.ShouldNotBe(size25.Cache.Key);
    }
}