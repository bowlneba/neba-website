using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentListSkeleton")]
public sealed class TournamentListSkeletonTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render hero skeleton when ShowHeroSkeleton is true")]
    public void Render_ShouldShowHeroSkeleton_WhenEnabled()
    {
        var cut = _ctx.Render<TournamentListSkeleton>(parameters => parameters
            .Add(p => p.ShowHeroSkeleton, true));

        cut.Markup.ShouldContain("height:320px");
    }

    [Fact(DisplayName = "Should render three row skeleton cards")]
    public void Render_ShouldShowThreeSkeletonCards_WhenRendered()
    {
        var cut = _ctx.Render<TournamentListSkeleton>();

        cut.FindAll(".neba-card").Count.ShouldBe(3);
    }
}