using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Stats;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.SeasonSelector")]
public sealed class SeasonSelectorTests : IDisposable
{
    private const int s_year2024 = 2024;
    private const int s_year2023 = 2023;
    private const int s_year2022 = 2022;
    private const int s_year2021 = 2021;

    private readonly BunitContext _ctx;

    public SeasonSelectorTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render only season buttons and no jump control when three or fewer seasons are available")]
    public void Render_ShouldShowOnlyButtons_WhenThreeOrFewerSeasonsAvailable()
    {
        // Arrange
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
            [s_year2022] = "2022-2023",
        };

        // Act
        var cut = RenderSelector(seasons, s_year2024);

        // Assert
        cut.FindAll(".stats-season-btn").Count.ShouldBe(3);
        cut.FindAll(".stats-season-jump").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show only the three most recent seasons as buttons and a jump control for the rest")]
    public void Render_ShouldShowFirstThreeSeasonsAndJumpControl_WhenMoreThanThreeSeasonsAvailable()
    {
        // Arrange
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
            [s_year2022] = "2022-2023",
            [s_year2021] = "2021-2022",
        };

        // Act
        var cut = RenderSelector(seasons, s_year2024);

        // Assert
        var buttons = cut.FindAll(".stats-season-btn");
        buttons.Count.ShouldBe(3);
        buttons.ShouldAllBe(button => button.TextContent.Trim() != "2021-2022");
        cut.FindAll(".stats-season-jump").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should mark the button matching the selected year as active")]
    public void Render_ShouldMarkSelectedYearButtonActive_WhenSelectedYearIsAmongRecentSeasons()
    {
        // Arrange
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
        };

        // Act
        var cut = RenderSelector(seasons, s_year2023);

        // Assert
        cut.FindAll(".stats-season-btn.active").Single().TextContent.Trim().ShouldBe("2023-2024");
    }

    [Fact(DisplayName = "Should invoke SeasonSelected with the clicked year when a season button is clicked")]
    public async Task Click_ShouldInvokeSeasonSelected_WhenSeasonButtonClicked()
    {
        // Arrange
        int? selectedYear = null;
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
        };
        var cut = RenderSelector(seasons, s_year2024, year => selectedYear = year);

        // Act
        await cut.FindAll(".stats-season-btn")[1].ClickAsync(new());

        // Assert
        selectedYear.ShouldBe(s_year2023);
    }

    [Fact(DisplayName = "Should show the selected older season as the jump control's value when selected year is not a recent season")]
    public void Render_ShouldShowSelectedOlderSeasonInJumpControl_WhenSelectedYearIsAmongOlderSeasons()
    {
        // Arrange
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
            [s_year2022] = "2022-2023",
            [s_year2021] = "2021-2022",
        };

        // Act
        var cut = RenderSelector(seasons, s_year2021);

        // Assert
        cut.Find(".stats-season-jump input").GetAttribute("value").ShouldBe("2021-2022");
    }

    [Fact(DisplayName = "Should invoke SeasonSelected with the chosen year when an older season is selected via the jump control")]
    public async Task JumpSelection_ShouldInvokeSeasonSelectedWithChosenYear_WhenOlderSeasonSelected()
    {
        // Arrange
        int? selectedYear = null;
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
            [s_year2022] = "2022-2023",
            [s_year2021] = "2021-2022",
        };
        var cut = RenderSelector(seasons, s_year2024, year => selectedYear = year);

        // Act
        await cut.Find(".stats-season-jump input").InputAsync(new ChangeEventArgs { Value = "2021-2022" });
        await cut.Find(".stats-season-jump .neba-autocomplete-option").ClickAsync(new());

        // Assert
        selectedYear.ShouldBe(s_year2021);
    }

    [Fact(DisplayName = "Should fall back to the first recent season when the jump control selection is cleared")]
    public async Task JumpSelection_ShouldFallBackToFirstRecentSeason_WhenSelectionCleared()
    {
        // Arrange
        int? selectedYear = null;
        var seasons = new Dictionary<int, string>
        {
            [s_year2024] = "2024-2025",
            [s_year2023] = "2023-2024",
            [s_year2022] = "2022-2023",
            [s_year2021] = "2021-2022",
        };
        var cut = RenderSelector(seasons, s_year2021, year => selectedYear = year);

        // Act
        await cut.Find(".stats-season-jump .neba-autocomplete-clear").ClickAsync(new());

        // Assert
        selectedYear.ShouldBe(s_year2024);
    }

    private IRenderedComponent<SeasonSelector> RenderSelector(
        IReadOnlyDictionary<int, string> availableSeasons, int selectedYear, Action<int>? onSeasonSelected = null)
        => _ctx.Render<SeasonSelector>(p =>
        {
            p.Add(x => x.AvailableSeasons, availableSeasons);
            p.Add(x => x.SelectedYear, selectedYear);
            p.Add(x => x.SeasonSelected, onSeasonSelected ?? (_ => { }));
        });
}
