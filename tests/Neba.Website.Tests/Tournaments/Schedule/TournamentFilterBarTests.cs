using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentFilterBar")]
public sealed class TournamentFilterBarTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should emit SearchTermChanged when search input changes")]
    public void Search_ShouldEmitSearchTermChanged_WhenInputChanges()
    {
        string? observed = null;

        var cut = _ctx.Render<TournamentFilterBar>(parameters => parameters
            .Add(p => p.SearchTerm, string.Empty)
            .Add(p => p.SearchTermChanged, EventCallback.Factory.Create<string>(this, value => observed = value))
            .Add(p => p.TypeFilterChanged, EventCallback.Factory.Create<TournamentType?>(this, _ => { }))
            .Add(p => p.EligibilityFilterChanged, EventCallback.Factory.Create<TournamentEligibility?>(this, _ => { })));

        cut.Find("input[type='search']").Input("classic");

        observed.ShouldBe("classic");
    }

    [Fact(DisplayName = "Should emit TypeFilterChanged when type button is clicked")]
    public async Task TypeFilter_ShouldEmitTypeFilterChanged_WhenButtonClicked()
    {
        TournamentType? observed = null;

        var cut = _ctx.Render<TournamentFilterBar>(parameters => parameters
            .Add(p => p.SearchTerm, string.Empty)
            .Add(p => p.SearchTermChanged, EventCallback.Factory.Create<string>(this, _ => { }))
            .Add(p => p.TypeFilterChanged, EventCallback.Factory.Create<TournamentType?>(this, value => observed = value))
            .Add(p => p.EligibilityFilterChanged, EventCallback.Factory.Create<TournamentEligibility?>(this, _ => { })));

        await cut.FindAll("button").First(b => b.TextContent.Contains("Doubles", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

        observed.ShouldBe(TournamentType.Doubles);
    }

    [Fact(DisplayName = "Should emit EligibilityFilterChanged with parsed enum value when select changes")]
    public void EligibilityFilter_ShouldEmitParsedValue_WhenSelectChanges()
    {
        TournamentEligibility? observed = null;

        var cut = _ctx.Render<TournamentFilterBar>(parameters => parameters
            .Add(p => p.SearchTerm, string.Empty)
            .Add(p => p.SearchTermChanged, EventCallback.Factory.Create<string>(this, _ => { }))
            .Add(p => p.TypeFilterChanged, EventCallback.Factory.Create<TournamentType?>(this, _ => { }))
            .Add(p => p.EligibilityFilterChanged, EventCallback.Factory.Create<TournamentEligibility?>(this, value => observed = value)));

        cut.Find("select").Change("Women");

        observed.ShouldBe(TournamentEligibility.Women);
    }
}
