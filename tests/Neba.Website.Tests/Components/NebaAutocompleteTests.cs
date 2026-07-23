using Bunit;

using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaAutocomplete")]
public sealed class NebaAutocompleteTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly IReadOnlyList<Item> _items =
    [
        new Item("a", "Alpha Lanes"),
        new Item("b", "Bravo Bowl"),
        new Item("c", "Charlie Center")
    ];

    public NebaAutocompleteTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should display the empty label placeholder when no value is selected")]
    public void Render_ShouldShowEmptyLabelPlaceholder_WhenNoValueSelected()
    {
        // Act
        var cut = RenderAutocomplete();

        // Assert
        cut.Find("input").GetAttribute("placeholder").ShouldBe("Not yet assigned");
        cut.Find("input").GetAttribute("value").ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Should display the selected item's display text when a value is provided")]
    public void Render_ShouldShowSelectedDisplayText_WhenValueProvided()
    {
        // Act
        var cut = RenderAutocomplete(p => p.Add(x => x.Value, "b"));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe("Bravo Bowl");
    }

    [Fact(DisplayName = "Should not render a clear button when no value is selected")]
    public void Render_ShouldNotRenderClearButton_WhenNoValueSelected()
    {
        // Act
        var cut = RenderAutocomplete();

        // Assert
        cut.FindAll(".neba-autocomplete-clear").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render all items in the panel when the input receives focus")]
    public void Focus_ShouldShowAllItems()
    {
        // Arrange
        var cut = RenderAutocomplete();

        // Act
        cut.Find("input").TriggerEvent("onfocus", new Microsoft.AspNetCore.Components.Web.FocusEventArgs());

        // Assert
        cut.FindAll(".neba-autocomplete-option").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should filter the panel to items whose display text matches the typed text")]
    public void Input_ShouldFilterItems_WhenTextTyped()
    {
        // Arrange
        var cut = RenderAutocomplete();

        // Act
        cut.Find("input").Input("bravo");

        // Assert
        var options = cut.FindAll(".neba-autocomplete-option");
        options.Count.ShouldBe(1);
        options[0].TextContent.ShouldBe("Bravo Bowl");
    }

    [Fact(DisplayName = "Should show the no-results message when nothing matches the typed text")]
    public void Input_ShouldShowNoResultsMessage_WhenNothingMatches()
    {
        // Arrange
        var cut = RenderAutocomplete();

        // Act
        cut.Find("input").Input("zzz");

        // Assert
        cut.Find(".neba-autocomplete-empty").TextContent.ShouldBe("No matches found");
    }

    [Fact(DisplayName = "Should raise ValueChanged with the item's value and close the panel when an option is clicked")]
    public void Click_ShouldRaiseValueChangedAndClosePanel_WhenOptionClicked()
    {
        // Arrange
        string? selected = null;
        var cut = RenderAutocomplete(p => p.Add(x => x.ValueChanged, (string? v) => selected = v));
        cut.Find("input").Input("bravo");

        // Act
        cut.Find(".neba-autocomplete-option").Click();

        // Assert
        selected.ShouldBe("b");
        cut.FindAll(".neba-autocomplete-panel").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should raise ValueChanged with null when the clear button is clicked")]
    public void Click_ShouldRaiseValueChangedWithNull_WhenClearButtonClicked()
    {
        // Arrange
        string? selected = "unchanged";
        var cut = RenderAutocomplete(p => p
            .Add(x => x.Value, "b")
            .Add(x => x.ValueChanged, (string? v) => selected = v));

        // Act
        cut.Find(".neba-autocomplete-clear").Click();

        // Assert
        selected.ShouldBeNull();
    }

    private IRenderedComponent<NebaAutocomplete<string, Item>> RenderAutocomplete(
        Action<ComponentParameterCollectionBuilder<NebaAutocomplete<string, Item>>>? configure = null)
        => _ctx.Render<NebaAutocomplete<string, Item>>(p =>
        {
            p.Add(x => x.Id, "test-autocomplete");
            p.Add(x => x.Items, _items);
            p.Add(x => x.DisplayText, (Item i) => i.Name);
            p.Add(x => x.ItemValue, (Item i) => i.Id);
            configure?.Invoke(p);
        });

    private sealed record Item(string Id, string Name);
}
