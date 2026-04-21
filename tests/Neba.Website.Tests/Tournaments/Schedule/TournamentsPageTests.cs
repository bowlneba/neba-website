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

        _dataService = new FakeTournamentDataService();
        _ctx.Services.AddSingleton<ITournamentDataService>(_dataService);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render hero and upcoming tournament card for current season route")]
    public void Render_ShouldShowHeroAndUpcomingCard_WhenCurrentSeasonLoaded()
    {
        // Arrange
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var upcoming = TournamentSummaryViewModelFactory.Create(
            id: "upcoming",
            name: "Spring Classic",
            season: currentYear,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)));
        var past = TournamentSummaryViewModelFactory.Create(
            id: "past",
            name: "Last Winter Open",
            season: currentYear,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-40)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-38)));

        _dataService.AvailableSeasons = [currentYear, (DateTime.Today.Year + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)];
        _dataService.SeasonData[currentYear] = [upcoming, past];

        // Act
        var cut = _ctx.Render<TournamentsPage>(parameters => parameters
            .Add(p => p.Season, currentYear));

        // Assert
        cut.Markup.ShouldContain("Next upcoming tournament");
        cut.Markup.ShouldContain("Spring Classic");
        _dataService.RequestedSeasons.ShouldContain(currentYear);
    }

    [Fact(DisplayName = "Should default to past tab and render merged season note when season route is 2020-21")]
    public void Render_ShouldShowPastState_WhenMergedSeasonRouteSelected()
    {
        // Arrange
        _dataService.AvailableSeasons = ["2020-21", DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        _dataService.SeasonData["2020-21"] =
        [
            TournamentSummaryViewModelFactory.Create(
                id: "merged-past",
                season: "2020-21",
                startDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddYears(-5).AddDays(1))) with
            {
                Winners = ["Merged Winner"],
            },
        ];

        // Act
        var cut = _ctx.Render<TournamentsPage>(parameters => parameters
            .Add(p => p.Season, "2020-21"));

        // Assert
        cut.Markup.ShouldContain("Merged Winner");
        cut.Markup.ShouldContain("Combined Season");
        cut.Markup.ShouldNotContain("Next upcoming tournament");
    }

    [Fact(DisplayName = "Should redirect to base tournaments route when requested season is unavailable")]
    public void Render_ShouldNavigateToBaseRoute_WhenSeasonIsInvalid()
    {
        // Arrange
        _dataService.AvailableSeasons = [DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        // Act
        _ctx.Render<TournamentsPage>(parameters => parameters
            .Add(p => p.Season, "1999"));

        // Assert
        var uri = _ctx.Services.GetRequiredService<NavigationManager>().Uri;
        uri.ShouldEndWith("/tournaments");
    }

    [Fact(DisplayName = "Should use next season upcoming event as hero when current season has only past tournaments")]
    public void Render_ShouldUseNextSeasonHero_WhenCurrentSeasonHasNoUpcoming()
    {
        // Arrange
        var currentYear = DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var nextYear = (DateTime.Today.Year + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        _dataService.AvailableSeasons = [currentYear, nextYear];
        _dataService.SeasonData[currentYear] =
        [
            TournamentSummaryViewModelFactory.Create(
                id: "all-past",
                name: "Finished Event",
                season: currentYear,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-20)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-19))),
        ];

        _dataService.SeasonData[nextYear] =
        [
            TournamentSummaryViewModelFactory.Create(
                id: "next-hero",
                name: "Future Kickoff",
                season: nextYear,
                startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(35)),
                endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(35))),
        ];

        // Act
        var cut = _ctx.Render<TournamentsPage>();

        // Assert
        cut.Markup.ShouldContain("Future Kickoff");
        _dataService.RequestedSeasons.ShouldContain(currentYear);
        _dataService.RequestedSeasons.ShouldContain(nextYear);
    }

    private sealed class FakeTournamentDataService : ITournamentDataService
    {
        public List<string> AvailableSeasons { get; set; } = [];

        public Dictionary<string, List<TournamentSummaryViewModel>> SeasonData { get; } =
            new(StringComparer.Ordinal);

        public List<string> RequestedSeasons { get; } = [];

        Task<List<string>> ITournamentDataService.GetAvailableSeasonsAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled<List<string>>(ct);
            }

            return Task.FromResult(AvailableSeasons);
        }

        Task<List<TournamentSummaryViewModel>> ITournamentDataService.GetTournamentsForSeasonAsync(string season, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled<List<TournamentSummaryViewModel>>(ct);
            }

            RequestedSeasons.Add(season);
            return Task.FromResult(SeasonData.TryGetValue(season, out var data) ? data : []);
        }
    }
}