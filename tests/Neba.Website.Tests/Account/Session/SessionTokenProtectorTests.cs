using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Tests.Account.Session;

[UnitTest]
[Component("Website.Account.Session")]
public sealed class SessionTokenProtectorTests
{
    private static readonly SessionTokens Tokens = new("user-123", "admin@bowlneba.com", "access-token", "refresh-token");

    [Fact(DisplayName = "Should return the original tokens when unprotecting a blob it produced")]
    public void Unprotect_ShouldRoundTripTokens_WhenBlobIsValid()
    {
        // Arrange
        var sut = CreateProtector(new EphemeralDataProtectionProvider());

        // Act
        var blob = sut.Protect(Tokens);
        var result = sut.Unprotect(blob);

        // Assert
        result.ShouldBe(Tokens);
    }

    [Fact(DisplayName = "Should not expose the raw tokens in the protected blob")]
    public void Protect_ShouldNotContainRawTokens()
    {
        // Arrange
        var sut = CreateProtector(new EphemeralDataProtectionProvider());

        // Act
        var blob = sut.Protect(Tokens);

        // Assert
        blob.ShouldNotContain("access-token");
        blob.ShouldNotContain("refresh-token");
    }

    [Fact(DisplayName = "Should return null when the blob has been tampered with")]
    public void Unprotect_ShouldReturnNull_WhenBlobIsTampered()
    {
        // Arrange
        var sut = CreateProtector(new EphemeralDataProtectionProvider());
        var blob = sut.Protect(Tokens);
        var tampered = blob[..^4] + (blob[^4] == 'A' ? "BBBB" : "AAAA");

        // Act
        var result = sut.Unprotect(tampered);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Should return null when the blob was produced with different keys")]
    public void Unprotect_ShouldReturnNull_WhenBlobComesFromOtherKeys()
    {
        // Arrange
        var blob = CreateProtector(new EphemeralDataProtectionProvider()).Protect(Tokens);
        var sut = CreateProtector(new EphemeralDataProtectionProvider());

        // Act
        var result = sut.Unprotect(blob);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Should return null when the blob is not valid base64")]
    public void Unprotect_ShouldReturnNull_WhenBlobIsGarbage()
    {
        // Arrange
        var sut = CreateProtector(new EphemeralDataProtectionProvider());

        // Act
        var result = sut.Unprotect("not a protected payload!");

        // Assert
        result.ShouldBeNull();
    }

    private static SessionTokenProtector CreateProtector(IDataProtectionProvider provider)
        => new(provider, NullLogger<SessionTokenProtector>.Instance);
}