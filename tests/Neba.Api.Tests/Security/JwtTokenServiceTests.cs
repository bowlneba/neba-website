using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;

using Neba.Api.Contracts.Security;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class JwtTokenServiceTests
{
    private static readonly JwtSettings ValidSettings = new()
    {
        Issuer = "https://api.bowlneba.com",
        Audience = "https://bowlneba.com",
        SigningKey = "super-secret-signing-key-that-is-long-enough-for-hmac-sha256",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    };

    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero));
        _sut = new JwtTokenService(ValidSettings, _fakeTimeProvider);
    }

    private static JwtSecurityToken ReadToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
#pragma warning disable CA5404 // Lifetime validation intentionally disabled — tests use a fixed fake time
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ValidSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = ValidSettings.Audience,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidSettings.SigningKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        handler.ValidateToken(accessToken, validationParams, out var validatedToken);
#pragma warning restore CA5404
        return (JwtSecurityToken)validatedToken;
    }

    [Fact(DisplayName = "CreateTokenPair should return a non-empty AccessToken")]
    public void CreateTokenPair_ShouldReturnNonEmptyAccessToken()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);

        // Assert
        result.AccessToken.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "CreateTokenPair should return a non-empty RefreshToken")]
    public void CreateTokenPair_ShouldReturnNonEmptyRefreshToken()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);

        // Assert
        result.RefreshToken.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "CreateTokenPair should return ExpiresAt equal to now plus AccessTokenExpiryMinutes")]
    public void CreateTokenPair_ShouldReturnCorrectExpiresAt()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();
        var now = _fakeTimeProvider.GetUtcNow();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);

        // Assert
        result.ExpiresAt.ShouldBe(now.AddMinutes(ValidSettings.AccessTokenExpiryMinutes));
    }

    [Fact(DisplayName = "CreateTokenPair should produce a JWT with the user ID as the sub claim")]
    public void CreateTokenPair_ShouldIncludeSubClaim()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var user = ApplicationUserFactory.Create(id: userId);

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Subject.ShouldBe(userId.ToString());
    }

    [Fact(DisplayName = "CreateTokenPair should produce a JWT with the user email as the email claim")]
    public void CreateTokenPair_ShouldIncludeEmailClaim()
    {
        // Arrange
        const string email = "test@bowlneba.com";
        var user = ApplicationUserFactory.Create(email: email);

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    }

    [Fact(DisplayName = "CreateTokenPair should produce a JWT with an iat claim matching the current time")]
    public void CreateTokenPair_ShouldIncludeIatClaim()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();
        var expectedIat = _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Iat && c.Value == expectedIat);
    }

    [Fact(DisplayName = "CreateTokenPair should include a role claim for each provided role")]
    public void CreateTokenPair_ShouldIncludeRoleClaims()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();
        string[] roles = [Roles.Admin, Roles.Member];

        // Act
        var result = _sut.CreateTokenPair(user, roles, []);
        var token = ReadToken(result.AccessToken);
        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        // Assert
        roleClaims.ShouldContain(Roles.Admin);
        roleClaims.ShouldContain(Roles.Member);
    }

    [Fact(DisplayName = "CreateTokenPair should not include any role claims when roles are empty")]
    public void CreateTokenPair_ShouldNotIncludeRoleClaims_WhenRolesEmpty()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldNotContain(c => c.Type == ClaimTypes.Role);
    }

    [Fact(DisplayName = "CreateTokenPair should include a permission claim for each provided permission")]
    public void CreateTokenPair_ShouldIncludePermissionClaims()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();
        Permissions[] permissions = [Permissions.Read, Permissions.Write];

        // Act
        var result = _sut.CreateTokenPair(user, [], permissions);
        var token = ReadToken(result.AccessToken);
        var permissionClaims = token.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        // Assert
        permissionClaims.ShouldContain("Read");
        permissionClaims.ShouldContain("Write");
    }

    [Fact(DisplayName = "CreateTokenPair should not include any permission claims when permissions are empty")]
    public void CreateTokenPair_ShouldNotIncludePermissionClaims_WhenPermissionsEmpty()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldNotContain(c => c.Type == "permission");
    }

    [Fact(DisplayName = "CreateTokenPair should include the usbc_id claim when the user has a UsbcId")]
    public void CreateTokenPair_ShouldIncludeUsbcIdClaim_WhenUserHasUsbcId()
    {
        // Arrange
        const string usbcId = "12345-67890";
        var user = ApplicationUserFactory.Create(usbcId: usbcId);

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldContain(c => c.Type == "usbc_id" && c.Value == usbcId);
    }

    [Fact(DisplayName = "CreateTokenPair should not include the usbc_id claim when the user has no UsbcId")]
    public void CreateTokenPair_ShouldNotIncludeUsbcIdClaim_WhenUserHasNoUsbcId()
    {
        // Arrange
        var user = ApplicationUserFactory.Create(usbcId: null);

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Claims.ShouldNotContain(c => c.Type == "usbc_id");
    }

    [Fact(DisplayName = "CreateTokenPair should produce a JWT with the configured issuer")]
    public void CreateTokenPair_ShouldSetIssuer()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);
        var token = ReadToken(result.AccessToken);

        // Assert
        token.Issuer.ShouldBe(ValidSettings.Issuer);
    }

    [Fact(DisplayName = "CreateTokenPair should produce a JWT whose signature validates against the configured signing key")]
    public void CreateTokenPair_ShouldProduceValidlySignedJwt()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);

        // Assert — ReadToken validates signature; an exception means the test fails
        Should.NotThrow(() => ReadToken(result.AccessToken));
    }

    [Fact(DisplayName = "CreateTokenPair should return a RefreshToken that decodes to 64 bytes of base64")]
    public void CreateTokenPair_ShouldReturnBase64RefreshTokenOf64Bytes()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var result = _sut.CreateTokenPair(user, [], []);

        // Assert
        var bytes = Should.NotThrow(() => Convert.FromBase64String(result.RefreshToken));
        bytes.Length.ShouldBe(64);
    }

    [Fact(DisplayName = "CreateTokenPair should produce unique RefreshTokens on successive calls")]
    public void CreateTokenPair_ShouldProduceUniqueRefreshTokens()
    {
        // Arrange
        var user = ApplicationUserFactory.Create();

        // Act
        var first = _sut.CreateTokenPair(user, [], []);
        var second = _sut.CreateTokenPair(user, [], []);

        // Assert
        first.RefreshToken.ShouldNotBe(second.RefreshToken);
    }
}