using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentTabBar")]
public sealed class TournamentTabBarTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should disable upcoming tab when season is not current")]
    public void Render_ShouldDisableUpcomingTab_WhenNotCurrentSeason()
    {
        var season2024 = MakeSeason(2024);
        var season2023 = MakeSeason(2023);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, false)
            .Add(p => p.ActiveSeason, season2024)
            .Add(p => p.AvailableSeasons, [season2024, season2023])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        cut.FindAll("button")[0].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not show season selector when upcoming tab is active")]
    public void Render_ShouldHideSeasonSelector_WhenUpcomingTabActive()
    {
        var season = MakeSeason(DateTime.Today.Year);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Upcoming)
            .Add(p => p.IsCurrentSeason, true)
            .Add(p => p.ActiveSeason, season)
            .Add(p => p.AvailableSeasons, [season])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        cut.FindAll("select").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should display season descriptions in selector when past tab is active")]
    public void Render_ShouldShowSeasonDescriptions_WhenPastTabActive()
    {
        var season2024 = MakeSeason(2024);
        var season2023 = MakeSeason(2023);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, false)
            .Add(p => p.ActiveSeason, season2024)
            .Add(p => p.AvailableSeasons, [season2024, season2023])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        cut.Markup.ShouldContain("2024 Season");
        cut.Markup.ShouldContain("2023 Season");
    }

    [Fact(DisplayName = "Should display merged season description in selector")]
    public void Render_ShouldShowMergedSeasonDescription_WhenMergedSeasonInList()
    {
        var mergedSeason = SeasonViewModelFactory.Create(
            description: "2020-21 Season",
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2021, 12, 31));
        var season2019 = MakeSeason(2019);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, false)
            .Add(p => p.ActiveSeason, mergedSeason)
            .Add(p => p.AvailableSeasons, [mergedSeason, season2019])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        cut.Markup.ShouldContain("2020-21 Season");
    }

    [Fact(DisplayName = "Should emit tab and season change callbacks when controls are used")]
    public async Task Render_ShouldEmitCallbacks_WhenTabAndSeasonChanged()
    {
        TournamentTab? observedTab = null;
        int? observedYear = null;

        var season2024 = MakeSeason(2024);
        var season2023 = MakeSeason(2023);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, true)
            .Add(p => p.ActiveSeason, season2024)
            .Add(p => p.AvailableSeasons, [season2024, season2023])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, value => observedTab = value))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, value => observedYear = value)));

        await cut.FindAll("button").First(b => b.TextContent.Contains("Upcoming", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "2023" });

        observedTab.ShouldBe(TournamentTab.Upcoming);
        observedYear.ShouldBe(2023);
    }

    [Fact(DisplayName = "Should use start year as option value for merged season")]
    public async Task Render_ShouldEmitStartYear_WhenMergedSeasonSelected()
    {
        int? observedYear = null;
        var mergedSeason = SeasonViewModelFactory.Create(
            description: "2020-21 Season",
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2021, 12, 31));
        var season2019 = MakeSeason(2019);

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, false)
            .Add(p => p.ActiveSeason, season2019)
            .Add(p => p.AvailableSeasons, [mergedSeason, season2019])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<int>(this, value => observedYear = value)));

        // Merged season option value is StartDate.Year = 2020
        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "2020" });

        observedYear.ShouldBe(2020);
    }

    private static SeasonViewModel MakeSeason(int year) => SeasonViewModelFactory.Create(
        description: $"{year} Season",
        startDate: new DateOnly(year, 1, 1),
        endDate: new DateOnly(year, 12, 31));
}