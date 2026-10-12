using Neba.Api.Security.ListRoles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.ListRoles;

[UnitTest]
[Component("Security")]
public sealed class ListRolesQueryTests
{
    [Fact(DisplayName = "Expiry should be 1 hour")]
    public void Expiry_ShouldBe1Hour()
    {
        // Act
        var query = new ListRolesQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact(DisplayName = "Cache key should be neba:security:roles:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListRolesQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:security:roles:list");
    }

    [Fact(DisplayName = "Cache tags should be neba, neba:security and neba:security:roles")]
    public void Cache_Tags_ShouldContainExpectedTags()
    {
        // Act
        var query = new ListRolesQuery();

        // Assert
        query.Cache.Tags.ShouldBe(["neba", "neba:security", "neba:security:roles"], ignoreOrder: true);
    }
}
