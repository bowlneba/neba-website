using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.BowlerPicker")]
public sealed class BowlerPickerTests : IDisposable
{
    private static readonly BowlerSearchResultViewModel Dave = new("id-dave", "Dave Smith", "4821-09317");
    private static readonly BowlerSearchResultViewModel Dana = new("id-dana", "Dana Smith", null);

    private readonly BunitContext _ctx = new();
    private readonly Mock<ITeamEligibilityService> _service = new(MockBehavior.Strict);

    public BowlerPickerTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton(_service.Object);
    }

    public void Dispose() => _ctx.Dispose();

    private void SetupSearch(string text, params BowlerSearchResultViewModel[] results)
    {
        IReadOnlyList<BowlerSearchResultViewModel> list = [.. results];
        ErrorOr<IReadOnlyList<BowlerSearchResultViewModel>> value = ErrorOrFactory.From(list);

        _service
            .Setup(x => x.SearchBowlersAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value);
    }

    private IRenderedComponent<BowlerPicker> Render(
        BowlerSearchResultViewModel? selected = null,
        bool isUnknown = false,
        IReadOnlySet<string>? excluded = null,
        Action<BowlerSearchResultViewModel?>? onSelected = null,
        Action<bool>? onUnknown = null)
        => _ctx.Render<BowlerPicker>(p =>
        {
            p.Add(x => x.Id, "bowler-1");
            p.Add(x => x.Label, "Bowler 1");
            p.Add(x => x.Selected, selected);
            p.Add(x => x.IsUnknown, isUnknown);
            p.Add(x => x.ExcludedIds, excluded ?? new HashSet<string>());
            p.Add(x => x.SelectedChanged, (BowlerSearchResultViewModel? bowler) => onSelected?.Invoke(bowler));
            p.Add(x => x.IsUnknownChanged, (bool unknown) => onUnknown?.Invoke(unknown));
        });

    private static void OpenAndType(IRenderedComponent<BowlerPicker> cut, string text)
    {
        cut.Find("input").TriggerEvent("onfocus", new FocusEventArgs());
        cut.Find("input").Input(text);
    }

    [Fact(DisplayName = "Should show the label for the search input")]
    public void Render_ShouldShowLabel()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        cut.Find("label").TextContent.ShouldBe("Bowler 1");
        cut.Find("label").GetAttribute("for").ShouldBe("bowler-1");
    }

    [Fact(DisplayName = "Should not search until three letters are typed")]
    public void Input_ShouldNotSearch_UnderThreeLetters()
    {
        // Arrange
        var cut = Render();

        // Act
        OpenAndType(cut, "sm");

        // Assert
        cut.Find(".neba-autocomplete-empty").TextContent.ShouldContain("Type at least 3 letters to search.");
        _service.Verify(x => x.SearchBowlersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Should search after a pause and list each bowler with the USBC ID when there is one")]
    public void Input_ShouldSearchAndListResults()
    {
        // Arrange
        SetupSearch("smi", Dave, Dana);
        var cut = Render();

        // Act
        OpenAndType(cut, "smi");

        // Assert
        cut.WaitForAssertion(() =>
            cut.FindAll(".neba-autocomplete-option").Select(option => option.TextContent.Trim())
                .ShouldBe(["Dave Smith (4821-09317)", "Dana Smith"]));
    }

    [Fact(DisplayName = "Should search once for the last text when typing fast")]
    public void Input_ShouldDebounce()
    {
        // Arrange
        SetupSearch("smit", Dave);
        var cut = Render();
        cut.Find("input").TriggerEvent("onfocus", new FocusEventArgs());

        // Act
        cut.Find("input").Input("smi");
        cut.Find("input").Input("smit");

        // Assert
        cut.WaitForAssertion(() => cut.FindAll(".neba-autocomplete-option").Count.ShouldBe(1));
        _service.Verify(x => x.SearchBowlersAsync("smi", It.IsAny<CancellationToken>()), Times.Never);
        _service.Verify(x => x.SearchBowlersAsync("smit", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should hide bowlers already chosen in the other slots")]
    public void Input_ShouldHideExcludedBowlers()
    {
        // Arrange
        SetupSearch("smi", Dave, Dana);
        var cut = Render(excluded: new HashSet<string> { "id-dave" });

        // Act
        OpenAndType(cut, "smi");

        // Assert
        cut.WaitForAssertion(() =>
            cut.FindAll(".neba-autocomplete-option").Select(option => option.TextContent.Trim()).ShouldBe(["Dana Smith"]));
    }

    [Fact(DisplayName = "Should raise SelectedChanged with the bowler when an option is picked")]
    public void Click_ShouldRaiseSelectedChanged()
    {
        // Arrange
        SetupSearch("smi", Dave, Dana);
        BowlerSearchResultViewModel? picked = null;
        var cut = Render(onSelected: bowler => picked = bowler);
        OpenAndType(cut, "smi");
        cut.WaitForAssertion(() => cut.FindAll(".neba-autocomplete-option").Count.ShouldBe(2));

        // Act
        cut.FindAll(".neba-autocomplete-option")[1].Click();

        // Assert
        picked.ShouldBe(Dana);
    }

    [Fact(DisplayName = "Should show the selected bowler in the input")]
    public void Render_ShouldShowSelectedBowler()
    {
        // Arrange & Act
        var cut = Render(selected: Dave);

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe("Dave Smith (4821-09317)");
    }

    [Fact(DisplayName = "Should show an unavailable message instead of the not-in-system action when the search fails")]
    public void Input_ShouldShowUnavailableMessage_WhenSearchFails()
    {
        // Arrange
        _service
            .Setup(x => x.SearchBowlersAsync("smi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("x", "failed"));
        var cut = Render();

        // Act
        OpenAndType(cut, "smi");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".neba-autocomplete-empty").TextContent.ShouldContain("Search is unavailable right now.");
            cut.FindAll(".neba-autocomplete-empty .bowler-not-found").ShouldBeEmpty();
        });
    }

    [Fact(DisplayName = "Should offer to mark the bowler as not in the system when nobody is found")]
    public void Input_ShouldOfferNotInSystemAction_WhenNobodyFound()
    {
        // Arrange
        SetupSearch("zzz");
        var cut = Render();

        // Act
        OpenAndType(cut, "zzz");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".neba-autocomplete-empty").TextContent.ShouldContain("No bowler found.");
            cut.Find(".neba-autocomplete-empty .bowler-not-found").TextContent.ShouldBe("Mark as not in our system");
        });
    }

    [Fact(DisplayName = "Should raise IsUnknownChanged and clear the selection when marked as not in the system")]
    public async Task Click_ShouldMarkAsUnknown()
    {
        // Arrange
        BowlerSearchResultViewModel? selected = Dave;
        bool? unknown = null;
        var cut = Render(selected: Dave, onSelected: bowler => selected = bowler, onUnknown: value => unknown = value);

        // Act
        await cut.Find("button.bowler-not-found").ClickAsync();

        // Assert
        unknown.ShouldBe(true);
        selected.ShouldBeNull();
    }

    [Fact(DisplayName = "Should show the not-in-system tag with an Undo action when marked, and unmark on Undo")]
    public async Task Render_ShouldShowUnknownTag_AndUndo()
    {
        // Arrange
        bool? unknown = null;
        var cut = Render(isUnknown: true, onUnknown: value => unknown = value);

        // Act
        cut.Find(".bowler-picker__tag").TextContent.ShouldContain("Not in our system");
        await cut.Find("button.bowler-not-found").ClickAsync();

        // Assert
        cut.FindAll("input").ShouldBeEmpty();
        unknown.ShouldBe(false);
    }
}