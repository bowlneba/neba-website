using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentLogoTile")]
public sealed class TournamentLogoTileTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render expected aria label and variant class")]
    public void Render_ShouldShowAriaLabelAndVariant_WhenParametersProvided()
    {
        var cut = _ctx.Render<TournamentLogoTile>(parameters => parameters
            .Add(p => p.TournamentName, "Granite Open")
            .Add(p => p.Season, "2026")
            .Add(p => p.TournamentType, "Trios")
            .Add(p => p.Variant, LogoTileVariant.Muted)
            .Add(p => p.CssClass, "extra-class"));

        (cut.Find(".tournament-logo-tile").GetAttribute("aria-label") ?? string.Empty).ShouldContain("Granite Open");
        cut.Markup.ShouldContain("tournament-logo-tile--muted");
        cut.Markup.ShouldContain("extra-class");
    }
}