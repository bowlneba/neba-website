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
        // Act
        var query = new ListBowlingCentersQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(7));
    }

    [Fact(DisplayName = "Cache key should be neba:bowling-centers:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListBowlingCentersQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:bowling-centers:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:bowling-centers")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListBowlingCentersQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:bowling-centers");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListBowlingCentersQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        // Act
        var query = new ListBowlingCentersQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(2);
    }
}