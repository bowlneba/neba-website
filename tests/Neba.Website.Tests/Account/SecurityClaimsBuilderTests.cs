using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;

namespace Neba.Website.Tests.Account;

[UnitTest]
[Component("Website.Account.SecurityClaimsBuilder")]
public sealed class SecurityClaimsBuilderTests
{
    [Fact(DisplayName = "Should include the NameIdentifier and Email claims")]
    public void BuildPrincipal_ShouldIncludeIdentityClaims()
    {
        // Arrange
        var jwt = BuildJwt(new { sub = "user-1" });

        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(jwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.ShouldBe("user-1");
        principal.FindFirst(ClaimTypes.Email)!.Value.ShouldBe("bowler@bowlneba.com");
    }

    [Fact(DisplayName = "Should not include token claims on the principal")]
    public void BuildPrincipal_ShouldNotIncludeTokenClaims()
    {
        // Arrange
        var jwt = BuildJwt(new { sub = "user-1" });

        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(jwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.FindFirst("access_token").ShouldBeNull();
        principal.FindFirst("refresh_token").ShouldBeNull();
    }

    [Fact(DisplayName = "Should pick up role claims from the JWT payload")]
    public void BuildPrincipal_ShouldIncludeRoleClaims_FromJwtPayload()
    {
        // Arrange
        var jwt = BuildJwt(new { sub = "user-1", role = new[] { "Admin", "Editor" } });

        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(jwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ShouldBe(["Admin", "Editor"], ignoreOrder: true);
    }

    [Fact(DisplayName = "Should pick up the usbc_id claim from the JWT payload")]
    public void BuildPrincipal_ShouldIncludeUsbcId_FromJwtPayload()
    {
        // Arrange
        var jwt = BuildJwt(new { sub = "user-1", usbc_id = "12345" });

        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(jwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.FindFirst("usbc_id")!.Value.ShouldBe("12345");
    }

    [Fact(DisplayName = "Should ignore JWT payload claims that are not role or usbc_id")]
    public void BuildPrincipal_ShouldIgnoreOtherJwtClaims()
    {
        // Arrange
        var jwt = BuildJwt(new { sub = "user-1", exp = 1234567890, iss = "neba-api" });

        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(jwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.FindFirst("exp").ShouldBeNull();
        principal.FindFirst("iss").ShouldBeNull();
    }

    [Theory(DisplayName = "Should produce only identity claims when the JWT is malformed")]
    [InlineData("not-a-jwt", TestDisplayName = "Single segment")]
    [InlineData("a.b", TestDisplayName = "Two segments")]
    [InlineData("a.!!!notbase64!!!.c", TestDisplayName = "Invalid base64 payload segment")]
    public void BuildPrincipal_ShouldFallBackToIdentityClaimsOnly_WhenJwtIsMalformed(string malformedJwt)
    {
        // Act
        var principal = SecurityClaimsBuilder.BuildPrincipal(malformedJwt, "user-1", "bowler@bowlneba.com");

        // Assert
        principal.Claims.Count().ShouldBe(2);
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.ShouldBe("user-1");
        principal.FindFirst(ClaimTypes.Email)!.Value.ShouldBe("bowler@bowlneba.com");
    }

    [Fact(DisplayName = "Should store the access and refresh tokens via AuthenticationProperties")]
    public void BuildAuthenticationProperties_ShouldStoreBothTokens()
    {
        // Act
        var properties = SecurityClaimsBuilder.BuildAuthenticationProperties("access-123", "refresh-456");

        // Assert
        properties.GetTokenValue(SecurityClaimsBuilder.AccessTokenName).ShouldBe("access-123");
        properties.GetTokenValue(SecurityClaimsBuilder.RefreshTokenName).ShouldBe("refresh-456");
    }

    private static string BuildJwt(object payload)
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        var body = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        return $"{header}.{body}.signature";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
