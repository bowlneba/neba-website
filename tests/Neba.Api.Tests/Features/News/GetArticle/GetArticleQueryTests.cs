using Neba.Api.Features.News.GetArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.GetArticle;

[UnitTest]
[Component("News")]
public sealed class GetArticleQueryTests
{
    [Fact(DisplayName = "Expiry should be 7 days")]
    public void Expiry_ShouldBe7Days()
    {
        // Act
        var query = new GetArticleQuery { Slug = "my-article", CallerHasArticleManagementPermission = false };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(7));
    }

    [Theory(DisplayName = "Cache key should follow neba:news:{slug}:article:scope:public format when caller lacks management permission")]
    [InlineData("my-article")]
    [InlineData("season-recap-2025")]
    [InlineData("tournament-results")]
    public void Cache_Key_ShouldFollowExpectedFormat_WhenCallerLacksManagementPermission(string slug)
    {
        // Act
        var query = new GetArticleQuery { Slug = slug, CallerHasArticleManagementPermission = false };

        // Assert
        query.Cache.Key.ShouldBe($"neba:news:{slug}:article:scope:public");
    }

    [Theory(DisplayName = "Cache key should follow neba:news:{slug}:article:scope:management format when caller has management permission")]
    [InlineData("my-article")]
    [InlineData("season-recap-2025")]
    [InlineData("tournament-results")]
    public void Cache_Key_ShouldFollowExpectedFormat_WhenCallerHasManagementPermission(string slug)
    {
        // Act
        var query = new GetArticleQuery { Slug = slug, CallerHasArticleManagementPermission = true };

        // Assert
        query.Cache.Key.ShouldBe($"neba:news:{slug}:article:scope:management");
    }

    [Fact(DisplayName = "Cache key should differ between caller with and without management permission")]
    public void Cache_Key_ShouldDifferByCallerManagementPermission()
    {
        // Arrange
        var publicQuery = new GetArticleQuery { Slug = "my-article", CallerHasArticleManagementPermission = false };
        var managementQuery = new GetArticleQuery { Slug = "my-article", CallerHasArticleManagementPermission = true };

        // Act + Assert
        publicQuery.Cache.Key.ShouldNotBe(managementQuery.Cache.Key);
    }

    [Theory(DisplayName = "Cache tags should include neba, category tag, and specific article tag")]
    [InlineData("my-article")]
    [InlineData("season-recap-2025")]
    public void Cache_Tags_ShouldIncludeExpectedTags(string slug)
    {
        // Act
        var query = new GetArticleQuery { Slug = slug, CallerHasArticleManagementPermission = false };

        // Assert
        query.Cache.Tags.ShouldContain("neba");
        query.Cache.Tags.ShouldContain("neba:news");
        query.Cache.Tags.ShouldContain($"neba:news:{slug}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new GetArticleQuery { Slug = "my-article", CallerHasArticleManagementPermission = false };

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Cache key should be specific to slug")]
    public void Cache_Key_ShouldBeSpecificToSlug()
    {
        // Act
        var article1 = new GetArticleQuery { Slug = "my-article", CallerHasArticleManagementPermission = false };
        var article2 = new GetArticleQuery { Slug = "season-recap-2025", CallerHasArticleManagementPermission = false };

        // Assert
        article1.Cache.Key.ShouldNotBe(article2.Cache.Key);
    }
}