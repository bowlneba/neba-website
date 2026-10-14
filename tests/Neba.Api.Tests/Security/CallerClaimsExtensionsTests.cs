using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Neba.Api.Contracts.Security;
using Neba.Api.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class CallerClaimsExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    [Fact(DisplayName = "CallerUserId returns the ULID from the sub claim")]
    public void CallerUserId_ShouldReturnSubClaim_WhenSubIsUlid()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        // Act
        var result = principal.CallerUserId;

        // Assert
        result.ShouldBe(id);
    }

    [Fact(DisplayName = "CallerUserId falls back to the NameIdentifier claim when sub is missing")]
    public void CallerUserId_ShouldFallBackToNameIdentifier_WhenSubMissing()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        // Act
        var result = principal.CallerUserId;

        // Assert
        result.ShouldBe(id);
    }

    [Fact(DisplayName = "CallerUserId prefers the sub claim over NameIdentifier")]
    public void CallerUserId_ShouldPreferSub_WhenBothPresent()
    {
        // Arrange
        var subId = Ulid.NewUlid();
        var nameId = Ulid.NewUlid();
        var principal = CreatePrincipal(
            new Claim(JwtRegisteredClaimNames.Sub, subId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, nameId.ToString()));

        // Act
        var result = principal.CallerUserId;

        // Assert
        result.ShouldBe(subId);
    }

    [Fact(DisplayName = "CallerUserId returns null when no user ID claim exists")]
    public void CallerUserId_ShouldReturnNull_WhenNoClaim()
    {
        // Arrange
        var principal = CreatePrincipal();

        // Act
        var result = principal.CallerUserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "CallerUserId returns null when the claim is not a ULID")]
    public void CallerUserId_ShouldReturnNull_WhenClaimIsNotUlid()
    {
        // Arrange
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, "not-a-ulid"));

        // Act
        var result = principal.CallerUserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "CallerPermissions returns only the permission claim values")]
    public void CallerPermissions_ShouldReturnPermissionValues_WhenClaimsPresent()
    {
        // Arrange
        var principal = CreatePrincipal(
            new Claim(Permissions.ClaimType, Permissions.EditArticle.Value),
            new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value),
            new Claim(Permissions.DeniedClaimType, Permissions.DeleteArticle.Value),
            new Claim("other", "value"));

        // Act
        var result = principal.CallerPermissions;

        // Assert
        result.ShouldBe([Permissions.EditArticle.Value, Permissions.CreateArticle.Value], ignoreOrder: true);
    }

    [Fact(DisplayName = "CallerPermissions returns an empty collection when the caller has no permission claims")]
    public void CallerPermissions_ShouldReturnEmpty_WhenNoClaims()
    {
        // Arrange
        var principal = CreatePrincipal();

        // Act
        var result = principal.CallerPermissions;

        // Assert
        result.ShouldBeEmpty();
    }
}
