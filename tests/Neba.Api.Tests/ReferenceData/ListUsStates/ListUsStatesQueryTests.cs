using Neba.Api.ReferenceData.ListUsStates;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ReferenceData.ListUsStates;

[UnitTest]
[Component("ReferenceData")]
public sealed class ListUsStatesQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        // Act
        var query = new ListUsStatesQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact(DisplayName = "Cache key should be neba:reference-data:us-states:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListUsStatesQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:reference-data:us-states:list");
    }

    [Fact(DisplayName = "Cache tags should contain neba:reference-data")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Act
        var query = new ListUsStatesQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba:reference-data");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListUsStatesQuery();

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        // Act
        var query = new ListUsStatesQuery();

        // Assert
        query.Cache.Tags.Count.ShouldBe(2);
    }
}