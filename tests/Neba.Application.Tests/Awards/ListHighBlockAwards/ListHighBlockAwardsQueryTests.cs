using Neba.Application.Awards.ListHighBlockAwards;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Awards.ListHighBlockAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighBlockAwardsQueryTests
{
    [Fact(DisplayName = "Expiry should be 365 days")]
    public void Expiry_ShouldBe365Days()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(365));
    }

    [Fact(DisplayName = "Cache key should be neba:awards:high-block:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Cache.Key.ShouldBe("neba:awards:high-block:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards")]
    public void Cache_Tags_ShouldContainAwardsTag()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards:high-block")]
    public void Cache_Tags_ShouldContainHighBlockTag()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards:high-block");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new ListHighBlockAwardsQuery();

        query.Cache.Tags.Count.ShouldBe(3);
    }
}
