using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentEmptyState")]
public sealed class TournamentEmptyStateTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show clear filters button and invoke callback when filters are active")]
    public async Task Render_ShouldShowClearButton_WhenFiltersAreActive()
    {
        var cleared = false;

        var cut = _ctx.Render<TournamentEmptyState>(parameters => parameters
            .Add(p => p.Tab, TournamentTab.Upcoming)
            .Add(p => p.HasActiveFilters, true)
            .Add(p => p.OnClearFilters, EventCallback.Factory.Create(this, () => cleared = true)));

        cut.Markup.ShouldContain("No tournaments match your current filters.");

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        cleared.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show upcoming empty message when no filters are active")]
    public void Render_ShouldShowUpcomingMessage_WhenUpcomingTabWithoutFilters()
    {
        var cut = _ctx.Render<TournamentEmptyState>(parameters => parameters
            .Add(p => p.Tab, TournamentTab.Upcoming)
            .Add(p => p.HasActiveFilters, false)
            .Add(p => p.OnClearFilters, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        cut.Markup.ShouldContain("No upcoming tournaments are scheduled for this season.");
    }

    [Fact(DisplayName = "Should show past empty message when tab is past and no filters are active")]
    public void Render_ShouldShowPastMessage_WhenPastTabWithoutFilters()
    {
        var cut = _ctx.Render<TournamentEmptyState>(parameters => parameters
            .Add(p => p.Tab, TournamentTab.Past)
            .Add(p => p.HasActiveFilters, false)
            .Add(p => p.OnClearFilters, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        cut.Markup.ShouldContain("No tournaments have been completed this season yet.");
    }
}