using Neba.Api.Features.Tournaments.ListOilPatterns;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListOilPatterns;

[UnitTest]
[Component("Tournaments")]
public sealed class ListOilPatternsQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        // Act
        var query = new ListOilPatternsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact(DisplayName = "Cache key should be neba:oil-patterns:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListOilPatternsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:oil-patterns:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:oil-patterns")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListOilPatternsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:oil-patterns");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListOilPatternsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        // Act
        var query = new ListOilPatternsQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(2);
    }
}
