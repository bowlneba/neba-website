using System.Security.Claims;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Domain;

[UnitTest]
[Component("Security")]
public sealed class UserPermissionOverridesTests
{
    private const string CreateArticle = "News.CreateArticle";
    private const string EditArticle = "News.EditArticle";
    private const string CreateSponsor = "Sponsors.CreateSponsor";

    [Fact(DisplayName = "Create should return the overrides when every value is a known permission")]
    public void Create_ShouldReturnOverrides_WhenValuesAreKnown()
    {
        // Arrange & Act
        var result = UserPermissionOverrides.Create([CreateArticle], [EditArticle]);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Added.ShouldBe([CreateArticle]);
        result.Value.Removed.ShouldBe([EditArticle]);
    }

    [Fact(DisplayName = "Create should reject a value that is not a known permission")]
    public void Create_ShouldReturnValidationError_WhenValueIsUnknown()
    {
        // Arrange & Act
        var result = UserPermissionOverrides.Create(["Nope.Nothing"], []);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("UserPermissionOverrides.UnknownPermission");
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
    }

    [Fact(DisplayName = "Create should reject a value that appears in both the added and removed sets")]
    public void Create_ShouldReturnValidationError_WhenValueIsInBothSets()
    {
        // Arrange & Act
        var result = UserPermissionOverrides.Create([CreateArticle], [CreateArticle]);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("UserPermissionOverrides.AddedAndRemoved");
    }

    [Fact(DisplayName = "FromStoredClaims should read added and removed values by claim type")]
    public void FromStoredClaims_ShouldReadAddedAndRemoved()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(Permissions.ClaimType, CreateArticle),
            new Claim(Permissions.DeniedClaimType, EditArticle)
        };

        // Act
        var overrides = UserPermissionOverrides.FromStoredClaims(claims);

        // Assert
        overrides.Added.ShouldBe([CreateArticle]);
        overrides.Removed.ShouldBe([EditArticle]);
    }

    [Fact(DisplayName = "FromStoredClaims should skip a stored value that is no longer a known permission")]
    public void FromStoredClaims_ShouldSkipUnknownValue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(Permissions.ClaimType, "Retired.Permission"),
            new Claim(Permissions.DeniedClaimType, "Retired.Permission")
        };

        // Act
        var overrides = UserPermissionOverrides.FromStoredClaims(claims);

        // Assert
        overrides.Added.ShouldBeEmpty();
        overrides.Removed.ShouldBeEmpty();
    }

    [Fact(DisplayName = "FromStoredClaims should treat a value stored as both added and removed as removed")]
    public void FromStoredClaims_ShouldTreatValueInBothSetsAsRemoved()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(Permissions.ClaimType, CreateArticle),
            new Claim(Permissions.DeniedClaimType, CreateArticle)
        };

        // Act
        var overrides = UserPermissionOverrides.FromStoredClaims(claims);

        // Assert
        overrides.Added.ShouldBeEmpty();
        overrides.Removed.ShouldBe([CreateArticle]);
    }

    [Fact(DisplayName = "FromStoredClaims should ignore claims of other types")]
    public void FromStoredClaims_ShouldIgnoreOtherClaimTypes()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Role, CreateArticle) };

        // Act
        var overrides = UserPermissionOverrides.FromStoredClaims(claims);

        // Assert
        overrides.Added.ShouldBeEmpty();
        overrides.Removed.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyTo should keep the role permissions when there are no overrides")]
    public void ApplyTo_ShouldKeepRolePermissions_WhenNoOverrides()
    {
        // Arrange
        var rolePermissions = new HashSet<string> { CreateArticle };

        // Act
        var effective = UserPermissionOverrides.None.ApplyTo(rolePermissions);

        // Assert
        effective.ShouldBe([CreateArticle]);
    }

    [Fact(DisplayName = "ApplyTo should grant an added permission the roles do not give")]
    public void ApplyTo_ShouldGrantAddedPermission()
    {
        // Arrange
        var overrides = UserPermissionOverrides.Create([CreateSponsor], []).Value;

        // Act
        var effective = overrides.ApplyTo(new HashSet<string> { CreateArticle });

        // Assert
        effective.Count.ShouldBe(2);
        effective.ShouldContain(CreateArticle);
        effective.ShouldContain(CreateSponsor);
    }

    [Fact(DisplayName = "ApplyTo should strip a removed permission a role grants")]
    public void ApplyTo_ShouldStripRemovedPermission()
    {
        // Arrange
        var overrides = UserPermissionOverrides.Create([], [CreateArticle]).Value;

        // Act
        var effective = overrides.ApplyTo(new HashSet<string> { CreateArticle, EditArticle });

        // Assert
        effective.ShouldBe([EditArticle]);
    }

    [Fact(DisplayName = "ApplyTo should let removed beat added for the same value")]
    public void ApplyTo_ShouldLetRemovedBeatAdded()
    {
        // Arrange
        var stored = new[]
        {
            new Claim(Permissions.ClaimType, CreateArticle),
            new Claim(Permissions.DeniedClaimType, CreateArticle)
        };
        var overrides = UserPermissionOverrides.FromStoredClaims(stored);

        // Act
        var effective = overrides.ApplyTo(new HashSet<string>());

        // Assert
        effective.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GainsOver should count a newly added permission")]
    public void GainsOver_ShouldCountNewAdd()
    {
        // Arrange
        var previous = UserPermissionOverrides.None;
        var next = UserPermissionOverrides.Create([CreateArticle], []).Value;

        // Act
        var gains = next.GainsOver(previous);

        // Assert
        gains.ShouldBe([CreateArticle]);
    }

    [Fact(DisplayName = "GainsOver should count a lifted removal")]
    public void GainsOver_ShouldCountLiftedRemoval()
    {
        // Arrange
        var previous = UserPermissionOverrides.Create([], [CreateArticle]).Value;
        var next = UserPermissionOverrides.None;

        // Act
        var gains = next.GainsOver(previous);

        // Assert
        gains.ShouldBe([CreateArticle]);
    }

    [Fact(DisplayName = "GainsOver should not count unchanged values or new removals")]
    public void GainsOver_ShouldNotCountUnchangedValuesOrNewRemovals()
    {
        // Arrange
        var previous = UserPermissionOverrides.Create([CreateArticle], [EditArticle]).Value;
        var next = UserPermissionOverrides.Create([CreateArticle], [EditArticle, CreateSponsor]).Value;

        // Act
        var gains = next.GainsOver(previous);

        // Assert
        gains.ShouldBeEmpty();
    }
}