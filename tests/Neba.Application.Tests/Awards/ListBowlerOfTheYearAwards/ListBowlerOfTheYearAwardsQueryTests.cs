using Neba.Application.Awards.ListBowlerOfTheYearAwards;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Awards.ListBowlerOfTheYearAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListBowlerOfTheYearAwardsQueryTests
{
    [Fact(DisplayName = "Expiry should be 365 days")]
    public void Expiry_ShouldBe365Days()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(365));
    }

    [Fact(DisplayName = "Cache key should be neba:awards:bowler-of-the-year:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Cache.Key.ShouldBe("neba:awards:bowler-of-the-year:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards")]
    public void Cache_Tags_ShouldContainAwardsTag()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards:bowler-of-the-year")]
    public void Cache_Tags_ShouldContainBowlerOfTheYearTag()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Cache.Tags.ShouldContain("neba:awards:bowler-of-the-year");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new ListBowlerOfTheYearAwardsQuery();

        query.Cache.Tags.Count.ShouldBe(3);
    }
}
