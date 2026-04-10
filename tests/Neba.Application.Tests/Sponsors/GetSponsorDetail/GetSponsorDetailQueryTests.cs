using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        var query = new GetSponsorDetailQuery { Slug = "acme" };

        query.Expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Theory(DisplayName = "Cache key should follow neba:sponsors:{slug}:detail format")]
    [InlineData("acme")]
    [InlineData("best-pro-shop")]
    [InlineData("lane-masters")]
    public void Cache_Key_ShouldFollowExpectedFormat(string slug)
    {
        var query = new GetSponsorDetailQuery { Slug = slug };

        query.Cache.Key.ShouldBe($"neba:sponsors:{slug}:detail");
    }

    [Theory(DisplayName = "Cache tags should include neba, category tag, and specific sponsor tag")]
    [InlineData("acme")]
    [InlineData("best-pro-shop")]
    public void Cache_Tags_ShouldIncludeExpectedTags(string slug)
    {
        var query = new GetSponsorDetailQuery { Slug = slug };

        query.Cache.Tags.ShouldContain("neba");
        query.Cache.Tags.ShouldContain("neba:sponsors");
        query.Cache.Tags.ShouldContain($"neba:sponsors:{slug}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new GetSponsorDetailQuery { Slug = "acme" };

        query.Cache.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Cache descriptor key should be specific to slug")]
    public void Cache_Key_ShouldBeSpecificToSlug()
    {
        var acme = new GetSponsorDetailQuery { Slug = "acme" };
        var bestProShop = new GetSponsorDetailQuery { Slug = "best-pro-shop" };

        acme.Cache.Key.ShouldNotBe(bestProShop.Cache.Key);
    }
}