using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

using TournamentsPage = Neba.Website.Server.Tournaments.Schedule.Tournaments;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.Tournaments")]
public sealed class TournamentsPageTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly FakeTournamentDataService _dataService;

    public TournamentsPageTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddRouting();

        _dataService = new FakeTournamentDataService();
        _ctx.Services.AddSingleton<ITournamentDataService>(_dataService);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render hero and upcoming tournament card for current season route")]
    public void Render_ShouldShowHeroAndUpcomingCard_WhenCurrentSeasonLoaded()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);
        var nextSeason = MakeSeason(currentYear + 1);

        var upcoming = SeasonTournamentViewModelFactory.Create(
            id: "upcoming",
            name: "Spring Classic",
            season: currentSeason.Label,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)));
        var past = SeasonTournamentViewModelFactory.Create(
            id: "past",
            name: "Last Winter Open",
            season: currentSeason.Label,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-40)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-38)));

        _dataService.Seasons = [currentSeason, nextSeason];
        _dataService.SeasonData[currentSeason.Label] = [upcoming, past];

        // Act — no year param → defaults to current season
        var cut = _ctx.Render<TournamentsPage>();

        // Assert
        cut.Markup.ShouldContain("Next upcoming tournament");
        cut.Markup.ShouldContain("Spring Classic");
        _dataService.RequestedSeasons.ShouldContain(currentSeason.Label);
    }

    [Fact(DisplayName = "Should display the season description in the header eyebrow")]
    public void Render_ShouldShowSeasonDescription_WhenCurrentSeasonLoaded()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);
        _dataService.Seasons = [currentSeason];
        _dataService.SeasonData[currentSeason.Label] = [SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)))];

        // Act
        var cut = _ctx.Render<TournamentsPage>();

        // Assert — description is shown (not raw year)
        cut.Markup.ShouldContain(currentSeason.Description);
    }

    [Fact(DisplayName = "Should default to past tab and render merged season note when year matches merged season")]
    public void Render_ShouldShowPastState_WhenMergedSeasonYearSelected()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);
        var mergedSeason = MakeMergedSeason();

        _dataService.Seasons = [currentSeason, mergedSeason];
        _dataService.SeasonData[currentSeason.Label] = [];
        _dataService.SeasonData[mergedSeason.Label] =
        [
            SeasonTournamentViewModelFactory.Create(
                id: "merged-past",
                season: mergedSeason.Label,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5).AddDays(1))) with
            {
                Winners = ["Merged Winner"],
            },
        ];

        // Act — navigate to year=2020 (start year of the merged 2020-21 season)
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.Render<TournamentsPage>();
        nav.NavigateTo(nav.GetUriWithQueryParameter("year", 2020));

        // Assert
        cut.Markup.ShouldContain("Merged Winner");
        cut.Markup.ShouldContain("Combined Season");
        cut.Markup.ShouldNotContain("Next upcoming tournament");
    }

    [Fact(DisplayName = "Should match merged season when year=2021 (end year) is requested")]
    public void Render_ShouldMatchMergedSeason_WhenEndYearOfMergedSeasonRequested()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);
        var mergedSeason = MakeMergedSeason();

        _dataService.Seasons = [currentSeason, mergedSeason];
        _dataService.SeasonData[currentSeason.Label] = [];
        _dataService.SeasonData[mergedSeason.Label] =
        [
            SeasonTournamentViewModelFactory.Create(
                id: "merged-past",
                season: mergedSeason.Label,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5))) with
            {
                Winners = ["End Year Winner"],
            },
        ];

        // Act — navigate to year=2021 (end year of 2020-21 merged season)
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = _ctx.Render<TournamentsPage>();
        nav.NavigateTo(nav.GetUriWithQueryParameter("year", 2021));

        // Assert — still shows the merged season content
        cut.Markup.ShouldContain("End Year Winner");
        cut.Markup.ShouldContain("Combined Season");
    }

    [Fact(DisplayName = "Should redirect to base tournaments route when requested year matches no season")]
    public void Render_ShouldNavigateToBaseRoute_WhenYearMatchesNoSeason()
    {
        // Arrange
        _dataService.Seasons = [MakeSeason(DateTime.Today.Year)];
        _dataService.SeasonData[MakeSeason(DateTime.Today.Year).Label] = [];

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        _ctx.Render<TournamentsPage>();

        // Act
        nav.NavigateTo(nav.GetUriWithQueryParameter("year", 1999));

        // Assert
        nav.Uri.ShouldEndWith("/tournaments");
    }

    [Fact(DisplayName = "Should use next season upcoming event as hero when current season has only past tournaments")]
    public void Render_ShouldUseNextSeasonHero_WhenCurrentSeasonHasNoUpcoming()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);
        var nextSeason = MakeSeason(currentYear + 1);

        _dataService.Seasons = [currentSeason, nextSeason];
        _dataService.SeasonData[currentSeason.Label] =
        [
            SeasonTournamentViewModelFactory.Create(
                id: "all-past",
                name: "Finished Event",
                season: currentSeason.Label,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-20)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-19))),
        ];

        _dataService.SeasonData[nextSeason.Label] =
        [
            SeasonTournamentViewModelFactory.Create(
                id: "next-hero",
                name: "Future Kickoff",
                season: nextSeason.Label,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(35)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(35))),
        ];

        // Act
        var cut = _ctx.Render<TournamentsPage>();

        // Assert
        cut.Markup.ShouldContain("Future Kickoff");
        _dataService.RequestedSeasons.ShouldContain(currentSeason.Label);
        _dataService.RequestedSeasons.ShouldContain(nextSeason.Label);
    }

    [Fact(DisplayName = "Should show fallback message when seasons cannot load")]
    public void Render_ShouldShowSeasonUnavailableNotice_WhenSeasonDataIsMissing()
    {
        // Arrange
        _dataService.Seasons = null;

        // Act
        var cut = _ctx.Render<TournamentsPage>();

        // Assert
        cut.Markup.ShouldContain("Season information is currently unavailable.");
    }

    [Fact(DisplayName = "Should show fallback message when tournaments cannot load")]
    public void Render_ShouldShowTournamentUnavailableNotice_WhenTournamentDataIsMissing()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var currentSeason = MakeSeason(currentYear);

        _dataService.Seasons = [currentSeason];
        _dataService.UnavailableSeasonLabels.Add(currentSeason.Label);

        // Act
        var cut = _ctx.Render<TournamentsPage>();

        // Assert
        cut.Markup.ShouldContain("Tournament data is currently unavailable.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static SeasonViewModel MakeSeason(int year) => SeasonViewModelFactory.Create(
        id: $"01JBMS0000000000000000{year % 100:D4}",
        description: $"{year} Season",
        startDate: new DateOnly(year, 1, 1),
        endDate: new DateOnly(year, 12, 31));

    private static SeasonViewModel MakeMergedSeason() => SeasonViewModelFactory.Create(
        id: "01JBMS00000000000000FF00",
        description: "2020-21 Season",
        startDate: new DateOnly(2020, 1, 1),
        endDate: new DateOnly(2021, 12, 31));

    // ── Fake service ───────────────────────────────────────────────────────

    private sealed class FakeTournamentDataService : ITournamentDataService
    {
        public List<SeasonViewModel>? Seasons { get; set; } = [];

        public Dictionary<string, List<SeasonTournamentViewModel>> SeasonData { get; } =
            new(StringComparer.Ordinal);

        public HashSet<string> UnavailableSeasonLabels { get; } = new(StringComparer.Ordinal);

        public List<string> RequestedSeasons { get; } = [];

        Task<List<SeasonViewModel>?> ITournamentDataService.GetSeasonsAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled<List<SeasonViewModel>?>(ct);
            }

            return Task.FromResult(Seasons);
        }

        Task<List<SeasonTournamentViewModel>?> ITournamentDataService.GetTournamentsForSeasonAsync(
            SeasonViewModel season, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled<List<SeasonTournamentViewModel>?>(ct);
            }

            RequestedSeasons.Add(season.Label);
            if (UnavailableSeasonLabels.Contains(season.Label))
            {
                return Task.FromResult<List<SeasonTournamentViewModel>?>(null);
            }

            if (SeasonData.TryGetValue(season.Label, out var data))
            {
                return Task.FromResult<List<SeasonTournamentViewModel>?>(data);
            }

            return Task.FromResult<List<SeasonTournamentViewModel>?>([]);
        }
    }
}