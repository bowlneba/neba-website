using Bunit;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.PreviousTournamentPicker")]
public sealed class PreviousTournamentPickerTests : IDisposable
{
    private static readonly SeasonViewModel Season2025 = SeasonViewModelFactory.Create(
        id: "season-2025", description: "2025 Season", startDate: new DateOnly(2024, 9, 1), endDate: new DateOnly(2025, 8, 31));
    private static readonly SeasonViewModel Season2024 = SeasonViewModelFactory.Create(
        id: "season-2024", description: "2024 Season", startDate: new DateOnly(2023, 9, 1), endDate: new DateOnly(2024, 8, 31));
    private static readonly SeasonViewModel Season2023 = SeasonViewModelFactory.Create(
        id: "season-2023", description: "2023 Season", startDate: new DateOnly(2022, 9, 1), endDate: new DateOnly(2023, 8, 31));
    private static readonly SeasonViewModel Season2022 = SeasonViewModelFactory.Create(
        id: "season-2022", description: "2022 Season", startDate: new DateOnly(2021, 9, 1), endDate: new DateOnly(2022, 8, 31));
    private static readonly SeasonViewModel Season2026 = SeasonViewModelFactory.Create(
        id: "season-2026", description: "2026 Season", startDate: new DateOnly(2025, 9, 1), endDate: new DateOnly(2026, 8, 31));

    private readonly BunitContext _ctx = new();
    private readonly Mock<ITournamentApiService> _service = new(MockBehavior.Strict);

    public PreviousTournamentPickerTests()
        => _ctx.Services.AddSingleton(_service.Object);

    public void Dispose() => _ctx.Dispose();

    private static SeasonTournamentViewModel Tournament(
        string id, string name, string type, DateOnly start, string status = "Completed")
        => SeasonTournamentViewModelFactory.Create(
            id: id, name: name, tournamentType: type, startDate: start, endDate: start.AddDays(1), status: status);

    private void SetupSeasons(params SeasonViewModel[] seasons)
        => _service.Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(seasons.ToList());

    private void SetupSeasonTournaments(SeasonViewModel season, params SeasonTournamentViewModel[] tournaments)
        => _service
            .Setup(x => x.GetTournamentsForSeasonAsync(season, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tournaments.ToList());

    private IRenderedComponent<PreviousTournamentPicker> Render(
        string type = "Doubles",
        DateOnly? start = null,
        string? value = null,
        string? selfId = null,
        Action<string?>? onChanged = null)
        => _ctx.Render<PreviousTournamentPicker>(p =>
        {
            p.Add(x => x.Id, "previous-tournament");
            p.Add(x => x.TournamentType, type);
            p.Add(x => x.StartDate, start ?? new DateOnly(2026, 3, 7));
            p.Add(x => x.Value, value);
            p.Add(x => x.SelfId, selfId);
            p.Add(x => x.ValueChanged, (string? v) => onChanged?.Invoke(v));
        });

    private static string[] OptionLabels(IRenderedComponent<PreviousTournamentPicker> cut)
        => [.. cut.FindAll("option").Select(option => option.TextContent.Trim())];

    [Fact(DisplayName = "Should offer None and the same-format tournaments that ended before the start date, newest first, labelled with month and year")]
    public async Task Render_ShouldOfferMatchingTournaments_NewestFirst()
    {
        // Arrange
        SetupSeasons(Season2025, Season2024, Season2023, Season2022, Season2026);
        SetupSeasonTournaments(Season2025,
            Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)),
            Tournament("b", "Joe's Singles", "Singles", new DateOnly(2025, 2, 8)));
        SetupSeasonTournaments(Season2024, Tournament("c", "Joe's Pizza Open", "Doubles", new DateOnly(2024, 2, 3)));
        SetupSeasonTournaments(Season2023, Tournament("d", "Joe's Pizza Open", "Doubles", new DateOnly(2023, 2, 4)));

        // Act
        var cut = Render();
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Assert
        OptionLabels(cut).ShouldBe(["None", "Joe's Pizza Open (02/2025)", "Joe's Pizza Open (02/2024)", "Joe's Pizza Open (02/2023)"]);
    }

    [Fact(DisplayName = "Should look at only the three seasons before the start date")]
    public async Task Render_ShouldLookAtThreeSeasons_Only()
    {
        // Arrange
        SetupSeasons(Season2025, Season2024, Season2023, Season2022);
        SetupSeasonTournaments(Season2025, Tournament("a", "Open 2025", "Doubles", new DateOnly(2025, 2, 1)));
        SetupSeasonTournaments(Season2024, Tournament("b", "Open 2024", "Doubles", new DateOnly(2024, 2, 1)));
        SetupSeasonTournaments(Season2023, Tournament("c", "Open 2023", "Doubles", new DateOnly(2023, 2, 1)));

        // Act
        var cut = Render();
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Assert
        OptionLabels(cut).Length.ShouldBe(4);
        OptionLabels(cut).ShouldNotContain(label => label.Contains("2022", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Should leave out cancelled tournaments, tournaments that end on or after the start date, and the tournament being edited")]
    public async Task Render_ShouldFilterOutIneligibleTournaments()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025,
            Tournament("ok", "Fine Open", "Doubles", new DateOnly(2025, 2, 1)),
            Tournament("cancelled", "Cancelled Open", "Doubles", new DateOnly(2025, 3, 1), status: "Cancelled"),
            Tournament("self", "Self Open", "Doubles", new DateOnly(2025, 4, 1)),
            Tournament("late", "Late Open", "Doubles", new DateOnly(2026, 3, 7)));

        // Act
        var cut = Render(selfId: "self");
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Assert
        OptionLabels(cut).ShouldBe(["None", "Fine Open (02/2025)"]);
    }

    [Fact(DisplayName = "Should raise ValueChanged with the chosen tournament's ID, and with null for None")]
    public async Task Change_ShouldRaiseValueChanged()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        var changes = new List<string?>();
        var cut = Render(onChanged: changes.Add);
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Act
        await cut.Find("select").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "a" });
        await cut.Find("select").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = string.Empty });

        // Assert
        changes.ShouldBe(["a", null]);
    }

    [Fact(DisplayName = "Should keep a saved value outside the window when the form opens, as 'Saved selection'")]
    public async Task Render_ShouldKeepSavedValue_WhenOutsideWindow()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        var changes = new List<string?>();

        // Act
        var cut = Render(value: "old-one", onChanged: changes.Add);
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Assert
        OptionLabels(cut).ShouldContain("Saved selection");
        changes.ShouldBeEmpty();
        cut.Find("select").InnerHtml.ShouldContain("selected");
    }

    [Fact(DisplayName = "Should clear the value with a notice when the format changes to one the previous tournament does not match")]
    public async Task Parameters_ShouldClearValue_WhenTypeChanges()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        var changes = new List<string?>();
        var cut = Render(value: "a", onChanged: changes.Add);
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Act
        cut.Render(p => p.Add(x => x.TournamentType, "Trios"));

        // Assert
        changes.ShouldBe([null]);
        cut.Find("[role=status]").TextContent.ShouldContain("same format");
    }

    [Fact(DisplayName = "Should clear the value with a notice when the start date moves to or before the previous tournament's end")]
    public async Task Parameters_ShouldClearValue_WhenStartDateMovesBeforePreviousEnd()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        var changes = new List<string?>();
        var cut = Render(value: "a", onChanged: changes.Add);
        await cut.WaitForStateAsync(() => cut.FindAll("option").Count > 1);

        // Act
        cut.Render(p => p.Add(x => x.StartDate, new DateOnly(2025, 2, 2)));

        // Assert
        changes.ShouldBe([null]);
        cut.Find("[role=status]").TextContent.ShouldContain("end before");
    }

    [Fact(DisplayName = "Should render nothing and clear a saved value for a Non-Champions tournament")]
    public void Render_ShouldBeHiddenAndClearValue_WhenNonChampions()
    {
        // Arrange
        var changes = new List<string?>();

        // Act
        var cut = Render(type: "Non-Champions", value: "a", onChanged: changes.Add);

        // Assert
        cut.FindAll("select").ShouldBeEmpty();
        changes.ShouldBe([null]);
    }

    [Fact(DisplayName = "Should show only None when there is no start date yet")]
    public void Render_ShouldShowOnlyNone_WhenNoStartDate()
    {
        // Arrange & Act
        var cut = _ctx.Render<PreviousTournamentPicker>(p =>
        {
            p.Add(x => x.Id, "previous-tournament");
            p.Add(x => x.TournamentType, "Doubles");
        });

        // Assert
        OptionLabels(cut).ShouldBe(["None"]);
    }

    [Fact(DisplayName = "Should show an alert when the seasons cannot be loaded")]
    public void Render_ShouldShowAlert_WhenSeasonsFail()
    {
        // Arrange
        _service.Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Error.Failure("x", "failed"));

        // Act
        var cut = Render();

        // Assert
        cut.Find("[role=alert]").TextContent.ShouldContain("couldn't load earlier tournaments");
    }

    [Fact(DisplayName = "Should show an alert when a season's tournaments cannot be loaded")]
    public void Render_ShouldShowAlert_WhenSeasonTournamentsFail()
    {
        // Arrange
        SetupSeasons(Season2025);
        _service
            .Setup(x => x.GetTournamentsForSeasonAsync(Season2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("x", "failed"));

        // Act
        var cut = Render();

        // Assert
        cut.Find("[role=alert]").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should keep a saved previous tournament when a season load fails and the start date changes")]
    public void Render_ShouldKeepValue_WhenLoadFailsAndStartDateChanges()
    {
        // Arrange
        var changes = new List<string?>();
        SetupSeasons(Season2025);
        _service
            .Setup(x => x.GetTournamentsForSeasonAsync(Season2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("x", "failed"));
        var cut = Render(value: "a", onChanged: changes.Add);

        // Act
        cut.Render(p => p.Add(x => x.StartDate, new DateOnly(2026, 4, 4)));

        // Assert
        changes.ShouldBeEmpty();
        cut.Find("[role=alert]").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should fetch only the failed season again, not the ones it already has")]
    public void Render_ShouldRefetchOnlyFailedSeason_WhenOneSeasonFailsFirst()
    {
        // Arrange
        var failFirst = true;
        SetupSeasons(Season2025, Season2024);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        _service
            .Setup(x => x.GetTournamentsForSeasonAsync(Season2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => failFirst
                ? Error.Failure("x", "failed")
                : new List<SeasonTournamentViewModel> { Tournament("b", "Old Open", "Doubles", new DateOnly(2024, 2, 1)) });
        var cut = Render();
        cut.Find("[role=alert]").ShouldNotBeNull();
        failFirst = false;

        // Act
        cut.Render(p => p.Add(x => x.StartDate, new DateOnly(2026, 4, 4)));

        // Assert
        cut.FindAll("[role=alert]").ShouldBeEmpty();
        _service.Verify(x => x.GetTournamentsForSeasonAsync(Season2025, It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(x => x.GetTournamentsForSeasonAsync(Season2024, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "Should fetch each season once when the parameters change but the seasons stay the same")]
    public void Render_ShouldFetchEachSeasonOnce_WhenParametersChange()
    {
        // Arrange
        SetupSeasons(Season2025);
        SetupSeasonTournaments(Season2025, Tournament("a", "Joe's Pizza Open", "Doubles", new DateOnly(2025, 2, 1)));
        var cut = Render();

        // Act
        cut.Render(p => p.Add(x => x.StartDate, new DateOnly(2026, 4, 4)));

        // Assert
        _service.Verify(x => x.GetTournamentsForSeasonAsync(Season2025, It.IsAny<CancellationToken>()), Times.Once);
    }
}
