using Neba.Api.Features.HallOfFame.ListHallOfFameInductions;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFameInductions;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFameInductionsQueryTests
{
    [Fact(DisplayName = "Expiry should be 100 days")]
    public void Expiry_ShouldBe100Days()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(100));
    }

    [Fact(DisplayName = "Cache key should be neba:hall-of-fame:inductions:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:hall-of-fame:inductions:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:hall-of-fame:inductions")]
    public void Cache_Tags_ShouldContainSpecificTag()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:hall-of-fame:inductions");
    }

    [Fact(DisplayName = "Cache tags should contain neba:hall-of-fame")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:hall-of-fame");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new ListHallOfFameInductionsQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }
}
