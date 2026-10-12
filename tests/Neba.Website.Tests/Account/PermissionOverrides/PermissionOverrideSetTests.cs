using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.PermissionOverrides;

namespace Neba.Website.Tests.Account.PermissionOverrides;

[UnitTest]
[Component("Website.Account.PermissionOverrides")]
public sealed class PermissionOverrideSetTests
{
    private const string Added = "News.CreateArticle";
    private const string Removed = "News.EditArticle";

    [Fact(DisplayName = "StateOf should return Default for a value in neither set")]
    public void StateOf_ShouldReturnDefault_WhenValueIsInNeitherSet()
    {
        // Arrange
        var set = new PermissionOverrideSet();

        // Act
        var state = set.StateOf(Added);

        // Assert
        state.ShouldBe(PermissionOverrideState.Default);
    }

    [Fact(DisplayName = "StateOf should return Added and Removed for values loaded into each set")]
    public void StateOf_ShouldReturnLoadedStates()
    {
        // Arrange
        var set = new PermissionOverrideSet([Added], [Removed]);

        // Act & Assert
        set.StateOf(Added).ShouldBe(PermissionOverrideState.Added);
        set.StateOf(Removed).ShouldBe(PermissionOverrideState.Removed);
    }

    [Fact(DisplayName = "Set should move a value from one set to the other")]
    public void Set_ShouldMoveValueBetweenSets()
    {
        // Arrange
        var set = new PermissionOverrideSet([Added], []);

        // Act
        set.Set(Added, PermissionOverrideState.Removed);

        // Assert
        set.Added.ShouldBeEmpty();
        set.Removed.ShouldBe([Added]);
    }

    [Fact(DisplayName = "Set to Default should remove the value from both sets")]
    public void Set_ShouldRemoveValue_WhenStateIsDefault()
    {
        // Arrange
        var set = new PermissionOverrideSet([Added], [Removed]);

        // Act
        set.Set(Added, PermissionOverrideState.Default);
        set.Set(Removed, PermissionOverrideState.Default);

        // Assert
        set.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Count should add the added and removed values")]
    public void Count_ShouldSumBothSets()
    {
        // Arrange
        var set = new PermissionOverrideSet([Added], [Removed]);

        // Act & Assert
        set.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Clone should be independent of the original")]
    public void Clone_ShouldBeIndependent()
    {
        // Arrange
        var original = new PermissionOverrideSet([Added], []);

        // Act
        var clone = original.Clone();
        clone.Set(Added, PermissionOverrideState.Default);

        // Assert
        original.StateOf(Added).ShouldBe(PermissionOverrideState.Added);
        clone.StateOf(Added).ShouldBe(PermissionOverrideState.Default);
    }

    [Fact(DisplayName = "Clear should reset every value to Default")]
    public void Clear_ShouldResetEverything()
    {
        // Arrange
        var set = new PermissionOverrideSet([Added], [Removed]);

        // Act
        set.Clear();

        // Assert
        set.Count.ShouldBe(0);
    }
}