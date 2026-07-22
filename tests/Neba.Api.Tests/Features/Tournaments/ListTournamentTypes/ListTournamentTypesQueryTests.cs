using Neba.Api.Features.Tournaments.ListTournamentTypes;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentTypes;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentTypesQueryTests
{
    [Fact(DisplayName = "Expiry should be 90 days")]
    public void Expiry_ShouldBe90Days()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(90));
    }

    [Fact(DisplayName = "Cache key should be neba:tournaments:types:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:tournaments:types:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments:types")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments:types");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments")]
    public void Cache_Tags_ShouldContainResourceTag()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new ListTournamentTypesQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }
}
