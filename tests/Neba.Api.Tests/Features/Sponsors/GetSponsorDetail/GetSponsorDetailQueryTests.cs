using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        // Act
        var query = new GetSponsorDetailQuery { Slug = "acme", CallerHasSponsorManagementPermission = false };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Theory(DisplayName = "Cache key should follow neba:sponsors:{slug}:detail:scope:public format when caller lacks sponsor management permission")]
    [InlineData("acme")]
    [InlineData("best-pro-shop")]
    [InlineData("lane-masters")]
    public void Cache_Key_ShouldFollowExpectedFormat_WhenCallerLacksManagementPermission(string slug)
    {
        // Act
        var query = new GetSponsorDetailQuery { Slug = slug, CallerHasSponsorManagementPermission = false };

        // Assert
        query.Cache.Key.ShouldBe($"neba:sponsors:{slug}:detail:scope:public");
    }

    [Theory(DisplayName = "Cache key should follow neba:sponsors:{slug}:detail:scope:management format when caller has sponsor management permission")]
    [InlineData("acme")]
    [InlineData("best-pro-shop")]
    public void Cache_Key_ShouldFollowExpectedFormat_WhenCallerHasManagementPermission(string slug)
    {
        // Act
        var query = new GetSponsorDetailQuery { Slug = slug, CallerHasSponsorManagementPermission = true };

        // Assert
        query.Cache.Key.ShouldBe($"neba:sponsors:{slug}:detail:scope:management");
    }

    [Fact(DisplayName = "Cache key should differ between public and management scopes for the same slug")]
    public void Cache_Key_ShouldDifferByScope()
    {
        // Act
        var publicQuery = new GetSponsorDetailQuery { Slug = "acme", CallerHasSponsorManagementPermission = false };
        var managementQuery = new GetSponsorDetailQuery { Slug = "acme", CallerHasSponsorManagementPermission = true };

        // Assert
        publicQuery.Cache.Key.ShouldNotBe(managementQuery.Cache.Key);
    }

    [Theory(DisplayName = "Cache tags should include neba, category tag, and specific sponsor tag")]
    [InlineData("acme")]
    [InlineData("best-pro-shop")]
    public void Cache_Tags_ShouldIncludeExpectedTags(string slug)
    {
        // Act
        var query = new GetSponsorDetailQuery { Slug = slug, CallerHasSponsorManagementPermission = false };

        // Assert
        query.Cache.Tags.ShouldContain("neba");
        query.Cache.Tags.ShouldContain("neba:sponsors");
        query.Cache.Tags.ShouldContain($"neba:sponsors:{slug}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new GetSponsorDetailQuery { Slug = "acme", CallerHasSponsorManagementPermission = false };

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Cache descriptor key should be specific to slug")]
    public void Cache_Key_ShouldBeSpecificToSlug()
    {
        // Act
        var acme = new GetSponsorDetailQuery { Slug = "acme", CallerHasSponsorManagementPermission = false };
        var bestProShop = new GetSponsorDetailQuery { Slug = "best-pro-shop", CallerHasSponsorManagementPermission = false };

        // Assert
        acme.Cache.Key.ShouldNotBe(bestProShop.Cache.Key);
    }
}
