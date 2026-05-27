using Neba.Api.Features.Tournaments.ListChampions;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListChampions;

[UnitTest]
[Component("Tournaments")]
public sealed class ListChampionsQueryTests
{
    [Fact(DisplayName = "Expiry should be 14 days")]
    public void Expiry_ShouldBe14Days()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact(DisplayName = "Cache key should be neba:tournaments:champions:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:tournaments:champions:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments")]
    public void Cache_Tags_ShouldContainTournamentsTag()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments:champions")]
    public void Cache_Tags_ShouldContainChampionsTag()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments:champions");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new ListChampionsQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }
}