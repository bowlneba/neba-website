using Neba.Api.Features.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.BowlingCenters.ListBowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class ListBowlingCentersQueryTests
{
    [Fact(DisplayName = "Expiry should be 7 days")]
    public void Expiry_ShouldBe7Days()
    {
        var query = new ListBowlingCentersQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(7));
    }

    [Fact(DisplayName = "Cache key should be neba:bowling-centers:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListBowlingCentersQuery();

        query.Cache.Key.ShouldBe("neba:bowling-centers:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:bowling-centers")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        var query = new ListBowlingCentersQuery();

        query.Cache.Tags.ShouldContain("neba:bowling-centers");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListBowlingCentersQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        var query = new ListBowlingCentersQuery();

        query.Cache.Tags.Count.ShouldBe(2);
    }
}