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

    [Fact(DisplayName = "ExpiryOf should parse a token once and reuse the result for the same token")]
    public void ExpiryOf_ShouldParseOnce_ForSameToken()
    {
        // Arrange
        var sut = new SessionTokenCache();
        var expiry = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        var parses = 0;
        DateTimeOffset? Parse(string _)
        {
            parses++;
            return expiry;
        }

        // Act
        var first = sut.ExpiryOf("token-1", Parse);
        var second = sut.ExpiryOf("token-1", Parse);

        // Assert
        first.ShouldBe(expiry);
        second.ShouldBe(expiry);
        parses.ShouldBe(1);
    }

    [Fact(DisplayName = "ExpiryOf should parse again when the token changes")]
    public void ExpiryOf_ShouldParseAgain_ForDifferentToken()
    {
        // Arrange
        var sut = new SessionTokenCache();
        var parsed = new List<string>();
        DateTimeOffset? Parse(string jwt)
        {
            parsed.Add(jwt);
            return null;
        }

        // Act
        sut.ExpiryOf("token-1", Parse);
        sut.ExpiryOf("token-2", Parse);

        // Assert
        parsed.ShouldBe(["token-1", "token-2"]);
    }

    [Fact(DisplayName = "ExpiryOf should keep separate results per cache, so users sharing a handler don't evict each other")]
    public void ExpiryOf_ShouldNotShareResultsAcrossCaches()
    {
        // Arrange
        var first = new SessionTokenCache();
        var second = new SessionTokenCache();
        var parses = 0;
        DateTimeOffset? Parse(string _)
        {
            parses++;
            return null;
        }

        // Act
        first.ExpiryOf("token-a", Parse);
        second.ExpiryOf("token-b", Parse);
        first.ExpiryOf("token-a", Parse);
        second.ExpiryOf("token-b", Parse);

        // Assert
        parses.ShouldBe(2);
    }
}