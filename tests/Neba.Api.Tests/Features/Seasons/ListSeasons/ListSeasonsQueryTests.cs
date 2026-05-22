using Neba.Api.Features.Seasons.ListSeasons;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Seasons.ListSeasons;

[UnitTest]
[Component("Seasons")]
public sealed class ListSeasonsQueryTests
{
    [Fact(DisplayName = "Expiry should be 90 days")]
    public void Expiry_ShouldBe90Days()
    {
        // Act
        var query = new ListSeasonsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(90));
    }

    [Fact(DisplayName = "Cache key should be neba:seasons:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListSeasonsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:seasons:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:seasons")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListSeasonsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:seasons");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListSeasonsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        // Act
        var query = new ListSeasonsQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(2);
    }
}
