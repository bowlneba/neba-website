using Neba.Api.Features.Sponsors.ListActiveSponsors;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.ListActiveSponsors;

[UnitTest]
[Component("Sponsors")]
public sealed class ListActiveSponsorsQueryTests
{
    [Fact(DisplayName = "Expiry should be 30 days")]
    public void Expiry_ShouldBe30Days()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact(DisplayName = "Cache key should be scoped to public when caller lacks management permission")]
    public void Cache_Key_ShouldBeScopedToPublic_WhenCallerLacksManagementPermission()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };

        // Act
        var key = query.Cache.Key;

        // Assert
        key.ShouldBe("neba:sponsors:list:scope:public");
    }

    [Fact(DisplayName = "Cache key should be scoped to management when caller has management permission")]
    public void Cache_Key_ShouldBeScopedToManagement_WhenCallerHasManagementPermission()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = true };

        // Act
        var key = query.Cache.Key;

        // Assert
        key.ShouldBe("neba:sponsors:list:scope:management");
    }

    [Fact(DisplayName = "Cache key should differ between caller with and without management permission")]
    public void Cache_Key_ShouldDifferByCallerManagementPermission()
    {
        // Arrange
        var publicQuery = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };
        var managementQuery = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = true };

        // Act + Assert
        publicQuery.Cache.Key.ShouldNotBe(managementQuery.Cache.Key);
    }

    [Fact(DisplayName = "Cache tags should contain neba:sponsors")]
    public void Cache_Tags_ShouldContainCategoryTag()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba:sponsors");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 2 tags")]
    public void Cache_Tags_ShouldContainExactly2Tags()
    {
        // Arrange
        var query = new ListActiveSponsorsQuery { CallerHasSponsorManagementPermission = false };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.Count.ShouldBe(2);
    }
}