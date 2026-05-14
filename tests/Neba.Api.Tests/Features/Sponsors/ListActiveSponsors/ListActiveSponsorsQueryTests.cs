using Neba.Api.Features.Sponsors.ListActiveSponsors;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.ListActiveSponsors;

[UnitTest]
[Component("Sponsors")]
public sealed class ListActiveSponsorsQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        var query = new ListActiveSponsorsQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact(DisplayName = "Cache key should be neba:sponsors:active:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListActiveSponsorsQuery();

        query.Cache.Key.ShouldBe("neba:sponsors:active:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:sponsors")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        var query = new ListActiveSponsorsQuery();

        query.Cache.Tags.ShouldContain("neba:sponsors");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListActiveSponsorsQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        var query = new ListActiveSponsorsQuery();

        query.Cache.Tags.Count.ShouldBe(2);
    }
}