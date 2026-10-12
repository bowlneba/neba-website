using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.PermissionOverrides;

namespace Neba.Website.Tests.Account.PermissionOverrides;

[UnitTest]
[Component("Website.Account.PermissionOverrides")]
public sealed class PermissionOverrideRulesTests
{
    [Theory(DisplayName = "IsGain should be true only when a move gives the user more access")]
    [InlineData(PermissionOverrideState.Default, PermissionOverrideState.Added, true, TestDisplayName = "Default to Added")]
    [InlineData(PermissionOverrideState.Removed, PermissionOverrideState.Default, true, TestDisplayName = "Removed to Default")]
    [InlineData(PermissionOverrideState.Removed, PermissionOverrideState.Added, true, TestDisplayName = "Removed to Added")]
    [InlineData(PermissionOverrideState.Default, PermissionOverrideState.Removed, false, TestDisplayName = "Default to Removed")]
    [InlineData(PermissionOverrideState.Added, PermissionOverrideState.Default, false, TestDisplayName = "Added to Default")]
    [InlineData(PermissionOverrideState.Added, PermissionOverrideState.Removed, false, TestDisplayName = "Added to Removed")]
    [InlineData(PermissionOverrideState.Added, PermissionOverrideState.Added, false, TestDisplayName = "Added unchanged")]
    [InlineData(PermissionOverrideState.Removed, PermissionOverrideState.Removed, false, TestDisplayName = "Removed unchanged")]
    [InlineData(PermissionOverrideState.Default, PermissionOverrideState.Default, false, TestDisplayName = "Default unchanged")]
    public void IsGain_ShouldMatchTruthTable(PermissionOverrideState original, PermissionOverrideState next, bool expected)
    {
        // Arrange & Act
        var result = PermissionOverrideRules.IsGain(original, next);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory(DisplayName = "IsAllowed should follow the override and fall back to the role grant for Default")]
    [InlineData(PermissionOverrideState.Added, false, true, TestDisplayName = "Added without a role")]
    [InlineData(PermissionOverrideState.Removed, true, false, TestDisplayName = "Removed with a role")]
    [InlineData(PermissionOverrideState.Default, true, true, TestDisplayName = "Default with a role")]
    [InlineData(PermissionOverrideState.Default, false, false, TestDisplayName = "Default without a role")]
    public void IsAllowed_ShouldFollowOverride(PermissionOverrideState state, bool grantedByRole, bool expected)
    {
        // Arrange & Act
        var result = PermissionOverrideRules.IsAllowed(state, grantedByRole);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "AreaOf should return the prefix before the first dot")]
    public void AreaOf_ShouldReturnPrefix()
    {
        // Arrange & Act & Assert
        PermissionOverrideRules.AreaOf("Tournaments.EditTournament").ShouldBe("Tournaments");
        PermissionOverrideRules.AreaOf("NoDot").ShouldBe("NoDot");
    }

    [Fact(DisplayName = "AreaSortKey should order System, News, Sponsors, Tournaments, then the rest")]
    public void AreaSortKey_ShouldOrderKnownAreasFirst()
    {
        // Arrange
        var areas = new[] { "Cache", "Tournaments", "System", "Sponsors", "News" };

        // Act
        var ordered = areas.OrderBy(PermissionOverrideRules.AreaSortKey).ToList();

        // Assert
        ordered.Take(4).ShouldBe(["System", "News", "Sponsors", "Tournaments"]);
        ordered[4].ShouldBe("Cache");
    }

    [Fact(DisplayName = "AreaDisplayName should split a PascalCase area into words")]
    public void AreaDisplayName_ShouldSplitWords()
    {
        // Arrange & Act
        var name = PermissionOverrideRules.AreaDisplayName("BackgroundJobs");

        // Assert
        name.ShouldBe("Background Jobs");
    }
}
