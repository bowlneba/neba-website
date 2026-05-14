using Neba.Api.Features.Awards.ListHighAverageAwards;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Awards.ListHighAverageAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighAverageAwardsQueryTests
{
    [Fact(DisplayName = "Expiry should be 365 days")]
    public void Expiry_ShouldBe365Days()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(365));
    }

    [Fact(DisplayName = "Cache key should be neba:awards:high-average:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Cache.Key.ShouldBe("neba:awards:high-average:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards")]
    public void Cache_Tags_ShouldContainAwardsTag()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards:high-average")]
    public void Cache_Tags_ShouldContainHighAverageTag()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards:high-average");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new ListHighAverageAwardsQuery();

        query.Cache.Tags.Count.ShouldBe(3);
    }
}