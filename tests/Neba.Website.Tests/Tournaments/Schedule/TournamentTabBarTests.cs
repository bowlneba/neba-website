using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
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
        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, false)
            .Add(p => p.ActiveSeason, "2024")
            .Add(p => p.AvailableSeasons, ["2024", "2023"])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, _ => { }))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<string>(this, _ => { })));

        cut.FindAll("button")[0].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should emit tab and season change callbacks when controls are used")]
    public async Task Render_ShouldEmitCallbacks_WhenTabAndSeasonChanged()
    {
        TournamentTab? observedTab = null;
        string? observedSeason = null;

        var cut = _ctx.Render<TournamentTabBar>(parameters => parameters
            .Add(p => p.ActiveTab, TournamentTab.Past)
            .Add(p => p.IsCurrentSeason, true)
            .Add(p => p.ActiveSeason, "2024")
            .Add(p => p.AvailableSeasons, ["2024", "2023"])
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<TournamentTab>(this, value => observedTab = value))
            .Add(p => p.OnSeasonChanged, EventCallback.Factory.Create<string>(this, value => observedSeason = value)));

        await cut.FindAll("button").First(b => b.TextContent.Contains("Upcoming", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "2023" });

        observedTab.ShouldBe(TournamentTab.Upcoming);
        observedSeason.ShouldBe("2023");
    }
}
