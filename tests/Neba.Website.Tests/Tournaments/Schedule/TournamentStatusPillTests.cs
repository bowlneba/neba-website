using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentStatusPill")]
public sealed class TournamentStatusPillTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render open styling and pulse dot when status is open")]
    public void Render_ShouldShowOpenStyling_WhenStatusIsOpen()
    {
        var cut = _ctx.Render<TournamentStatusPill>(parameters => parameters
            .Add(p => p.Status, RegistrationStatus.Open));

        cut.Markup.ShouldContain("tournament-status-pill--open");
        cut.Markup.ShouldContain("Registration open");
        cut.FindAll(".tournament-status-pill__dot").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should render default soon status when status is null")]
    public void Render_ShouldShowSoonCopy_WhenStatusIsNull()
    {
        var cut = _ctx.Render<TournamentStatusPill>();

        cut.Markup.ShouldContain("tournament-status-pill--soon");
        cut.Markup.ShouldContain("Registration opens soon");
    }
}