using Neba.Api.Features.Awards.ListHighBlockAwards;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Awards.ListHighBlockAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighBlockAwardsQueryTests
{
    [Fact(DisplayName = "Expiry should be 365 days")]
    public void Expiry_ShouldBe365Days()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(365));
    }

    [Fact(DisplayName = "Cache key should be neba:awards:high-block:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:awards:high-block:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards")]
    public void Cache_Tags_ShouldContainAwardsTag()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:awards");
    }

    [Fact(DisplayName = "Cache tags should contain neba:awards:high-block")]
    public void Cache_Tags_ShouldContainHighBlockTag()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:awards:high-block");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new ListHighBlockAwardsQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }
}
