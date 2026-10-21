using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.Session;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.SessionTokenCache")]
public sealed class SessionTokenCacheTests
{
    [Fact(DisplayName = "A new cache should be empty and unseeded")]
    public void NewCache_ShouldBeUnseeded()
    {
        // Arrange & Act
        var sut = new SessionTokenCache();

        // Assert
        sut.IsSeeded.ShouldBeFalse();
        sut.UserId.ShouldBeNull();
        sut.AccessToken.ShouldBeNull();
        sut.RefreshToken.ShouldBeNull();
    }

    [Fact(DisplayName = "Set should store the tokens and mark the cache seeded")]
    public void Set_ShouldStoreTokens()
    {
        // Arrange
        var sut = new SessionTokenCache();

        // Act
        sut.Set(new SessionTokens("user-123", "admin@bowlneba.com", "access-1", "refresh-1"));

        // Assert
        sut.IsSeeded.ShouldBeTrue();
        sut.UserId.ShouldBe("user-123");
        sut.AccessToken.ShouldBe("access-1");
        sut.RefreshToken.ShouldBe("refresh-1");
    }

    [Fact(DisplayName = "Set(null) should mark the cache seeded with no tokens, so an anonymous visitor isn't re-read")]
    public void Set_ShouldSeedWithoutTokens_WhenNull()
    {
        // Arrange
        var sut = new SessionTokenCache();

        // Act
        sut.Set(null);

        // Assert
        sut.IsSeeded.ShouldBeTrue();
        sut.UserId.ShouldBeNull();
        sut.AccessToken.ShouldBeNull();
        sut.RefreshToken.ShouldBeNull();
    }

    [Fact(DisplayName = "Set(null) should clear tokens stored earlier")]
    public void Set_ShouldClearTokens_WhenCalledWithNullAfterTokens()
    {
        // Arrange
        var sut = new SessionTokenCache();
        sut.Set(new SessionTokens("user-123", "admin@bowlneba.com", "access-1", "refresh-1"));

        // Act
        sut.Set(null);

        // Assert
        sut.IsSeeded.ShouldBeTrue();
        sut.AccessToken.ShouldBeNull();
        sut.RefreshToken.ShouldBeNull();
        sut.UserId.ShouldBeNull();
    }
}