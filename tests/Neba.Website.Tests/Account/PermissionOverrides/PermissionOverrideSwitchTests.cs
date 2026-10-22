using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.PermissionOverrides;

namespace Neba.Website.Tests.Account.PermissionOverrides;

[UnitTest]
[Component("Website.Account.PermissionOverrides")]
public sealed class PermissionOverrideSwitchTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<PermissionOverrideSwitch> Render(
        PermissionOverrideState value = PermissionOverrideState.Default,
        Action<PermissionOverrideState>? onChanged = null,
        bool disabled = false,
        IReadOnlySet<PermissionOverrideState>? disabledStates = null)
        => _ctx.Render<PermissionOverrideSwitch>(parameters => parameters
            .Add(p => p.Label, "Clear Cache")
            .Add(p => p.Value, value)
            .Add(p => p.Disabled, disabled)
            .Add(p => p.DisabledStates, disabledStates ?? new HashSet<PermissionOverrideState>())
            .Add(p => p.ValueChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<PermissionOverrideState>(
                this, state => onChanged?.Invoke(state))));

    [Fact(DisplayName = "Render should show three radios in the order Added, Default, Removed")]
    public void Render_ShouldShowThreeRadiosInOrder()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        cut.FindAll("input[type=radio]").Select(r => r.GetAttribute("value"))
            .ShouldBe(["added", "default", "removed"]);
    }

    [Theory(DisplayName = "Render should check the radio that matches the value and show its label")]
    [InlineData(PermissionOverrideState.Added, "added", "Added", TestDisplayName = "Added")]
    [InlineData(PermissionOverrideState.Default, "default", "Default", TestDisplayName = "Default")]
    [InlineData(PermissionOverrideState.Removed, "removed", "Removed", TestDisplayName = "Removed")]
    public void Render_ShouldCheckMatchingRadio(PermissionOverrideState value, string key, string text)
    {
        // Arrange & Act
        var cut = Render(value);

        // Assert
        cut.FindAll("input[type=radio]").Single(r => r.HasAttribute("checked")).GetAttribute("value").ShouldBe(key);
        cut.Find(".override-state-label").TextContent.ShouldBe(text);
    }

    [Fact(DisplayName = "Choosing a radio should raise ValueChanged with that state")]
    public void Change_ShouldRaiseValueChanged()
    {
        // Arrange
        PermissionOverrideState? chosen = null;
        var cut = Render(onChanged: state => chosen = state);

        // Act
        cut.Find("input[value=removed]").Change("removed");

        // Assert
        chosen.ShouldBe(PermissionOverrideState.Removed);
    }

    [Fact(DisplayName = "Disabled should disable all three radios")]
    public void Render_ShouldDisableAll_WhenDisabled()
    {
        // Arrange & Act
        var cut = Render(disabled: true);

        // Assert
        cut.FindAll("input[type=radio]").ShouldAllBe(r => r.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "DisabledStates should disable only the listed positions")]
    public void Render_ShouldDisableOnlyListedStates()
    {
        // Arrange & Act
        var cut = Render(disabledStates: new HashSet<PermissionOverrideState> { PermissionOverrideState.Added });

        // Assert
        cut.Find("input[value=added]").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("input[value=default]").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("input[value=removed]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Render should name the radio group after the permission")]
    public void Render_ShouldNameRadioGroupAfterPermission()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        cut.Find("[role=radiogroup]").GetAttribute("aria-label").ShouldBe("Clear Cache override");
    }
}