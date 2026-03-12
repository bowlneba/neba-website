using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.BowlingCenters.ListBowlingCenters;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFameInductionsQueryTests
{
    [Fact(DisplayName = "Expiry should be 100 days")]
    public void Expiry_ShouldBe100Days()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Expiry.ShouldBe(TimeSpan.FromDays(100));
    }

    [Fact(DisplayName = "Cache key should be neba:hall-of-fame:inductions:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Cache.Key.ShouldBe("neba:hall-of-fame:inductions:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:hall-of-fame:inductions")]
    public void Cache_Tags_ShouldContainSpecificTag()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Cache.Tags.ShouldContain("neba:hall-of-fame:inductions");
    }

    [Fact(DisplayName = "Cache tags should contain neba:hall-of-fame")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Cache.Tags.ShouldContain("neba:hall-of-fame");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new ListHallOfFameInductionsQuery();

        query.Cache.Tags.Count.ShouldBe(3);
    }
}