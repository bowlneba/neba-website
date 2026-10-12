using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.ListRoles;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Account.PermissionOverrides;

namespace Neba.Website.Tests.Account.PermissionOverrides;

[UnitTest]
[Component("Website.Account.PermissionOverrides")]
public sealed class PermissionOverridesSectionTests : IDisposable
{
    private static readonly RoleResponse Journalist = RoleResponseFactory.Create(
        "Journalist", [Permissions.CreateArticle.Value, Permissions.EditArticle.Value]);

    private static readonly RoleResponse Manager = RoleResponseFactory.Create(
        "Manager", [Permissions.CreateArticle.Value, Permissions.CreateSponsor.Value]);

    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private static IReadOnlyCollection<string> AllPermissionValues
        => [.. Permissions.List.Select(permission => permission.Value)];

    private IRenderedComponent<PermissionOverridesSection> Render(
        PermissionOverrideSet? overrides = null,
        PermissionOverrideSet? original = null,
        IReadOnlyCollection<string>? selectedRoles = null,
        IReadOnlyCollection<string>? callerPermissions = null,
        bool disabled = false,
        Action? onChanged = null)
        => _ctx.Render<PermissionOverridesSection>(parameters => parameters
            .Add(p => p.Roles, [Journalist, Manager])
            .Add(p => p.SelectedRoles, selectedRoles ?? [])
            .Add(p => p.Overrides, overrides ?? new PermissionOverrideSet())
            .Add(p => p.Original, original)
            .Add(p => p.CallerPermissions, callerPermissions ?? AllPermissionValues)
            .Add(p => p.Disabled, disabled)
            .Add(p => p.OnChanged, EventCallback.Factory.Create(this, () => onChanged?.Invoke())));

    private static AngleSharp.Dom.IElement RowFor(IRenderedComponent<PermissionOverridesSection> cut, Permissions permission)
        => cut.FindAll("tbody tr").Single(row => row.Children.Length > 0 && row.Children[0].TextContent == permission.Name);

    [Fact(DisplayName = "Render should show one row per permission")]
    public void Render_ShouldShowOneRowPerPermission()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        cut.FindAll("tbody tr:not(.permission-overrides-area)").Count.ShouldBe(Permissions.List.Count);
    }

    [Fact(DisplayName = "Render should group permissions by area with System first, then News, Sponsors, Tournaments")]
    public void Render_ShouldOrderAreas()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        var areas = cut.FindAll(".permission-overrides-area th").Select(h => h.TextContent).ToList();
        areas.Take(4).ShouldBe(["System", "News", "Sponsors", "Tournaments"]);
    }

    [Fact(DisplayName = "Render should list every selected role that grants a permission")]
    public void Render_ShouldListGrantingRoles()
    {
        // Arrange & Act
        var cut = Render(selectedRoles: ["Journalist", "Manager"]);

        // Assert
        RowFor(cut, Permissions.CreateArticle).QuerySelector(".permission-overrides-via")!
            .TextContent.Trim().ShouldBe("Journalist, Manager");
    }

    [Fact(DisplayName = "Render should ignore roles that are not selected")]
    public void Render_ShouldIgnoreUnselectedRoles()
    {
        // Arrange & Act
        var cut = Render(selectedRoles: ["Manager"]);

        // Assert
        RowFor(cut, Permissions.EditArticle).QuerySelector(".permission-overrides-via")!
            .TextContent.Trim().ShouldBe("—");
        RowFor(cut, Permissions.CreateSponsor).QuerySelector(".permission-overrides-via")!
            .TextContent.Trim().ShouldBe("Manager");
    }

    [Fact(DisplayName = "Render should show Result from the override and the role grant")]
    public void Render_ShouldShowResult()
    {
        // Arrange
        var overrides = new PermissionOverrideSet(
            added: [Permissions.CreateSponsor.Value],
            removed: [Permissions.EditArticle.Value]);

        // Act
        var cut = Render(overrides, selectedRoles: ["Journalist"]);

        // Assert
        RowFor(cut, Permissions.CreateSponsor).QuerySelector(".permission-overrides-result")!.TextContent.Trim().ShouldBe("Allowed");
        RowFor(cut, Permissions.EditArticle).QuerySelector(".permission-overrides-result")!.TextContent.Trim().ShouldBe("Not allowed");
        RowFor(cut, Permissions.CreateArticle).QuerySelector(".permission-overrides-result")!.TextContent.Trim().ShouldBe("Allowed");
        RowFor(cut, Permissions.DeleteArticle).QuerySelector(".permission-overrides-result")!.TextContent.Trim().ShouldBe("Not allowed");
    }

    [Fact(DisplayName = "Changing a switch should update the overrides and raise OnChanged")]
    public void Change_ShouldUpdateOverridesAndRaiseOnChanged()
    {
        // Arrange
        var overrides = new PermissionOverrideSet();
        var raised = 0;
        var cut = Render(overrides, onChanged: () => raised++);

        // Act
        RowFor(cut, Permissions.CreateSponsor).QuerySelector("input[value=added]")!.Change("added");

        // Assert
        overrides.StateOf(Permissions.CreateSponsor.Value).ShouldBe(PermissionOverrideState.Added);
        raised.ShouldBe(1);
    }

    [Fact(DisplayName = "Render should disable Added for a permission the caller does not hold")]
    public void Render_ShouldDisableAdded_WhenCallerDoesNotHoldPermission()
    {
        // Arrange & Act
        var cut = Render(callerPermissions: [Permissions.CreateArticle.Value]);

        // Assert
        var row = RowFor(cut, Permissions.ClearCache);
        row.QuerySelector("input[value=added]")!.HasAttribute("disabled").ShouldBeTrue();
        row.QuerySelector("input[value=removed]")!.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Render should disable lifting a removal the caller cannot grant")]
    public void Render_ShouldDisableLifting_WhenCallerDoesNotHoldPermission()
    {
        // Arrange
        var loaded = new PermissionOverrideSet([], [Permissions.ClearCache.Value]);

        // Act
        var cut = Render(loaded.Clone(), loaded, callerPermissions: [Permissions.CreateArticle.Value]);

        // Assert
        var row = RowFor(cut, Permissions.ClearCache);
        row.QuerySelector("input[value=default]")!.HasAttribute("disabled").ShouldBeTrue();
        row.QuerySelector("input[value=added]")!.HasAttribute("disabled").ShouldBeTrue();
        row.QuerySelector("input[value=removed]")!.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Render should leave every position enabled for a permission the caller holds")]
    public void Render_ShouldEnableAll_WhenCallerHoldsPermission()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        RowFor(cut, Permissions.ClearCache).QuerySelectorAll("input[type=radio]")
            .ShouldAllBe(radio => !radio.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Disabled should disable every switch")]
    public void Render_ShouldDisableEverySwitch_WhenDisabled()
    {
        // Arrange & Act
        var cut = Render(disabled: true);

        // Assert
        cut.FindAll("input[type=radio]").ShouldAllBe(radio => radio.HasAttribute("disabled"));
    }
}
