using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaSkeletonLoader")]
public sealed class NebaSkeletonLoaderTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Defaults to Text type when no type is specified")]
    public void Render_ShouldDefaultToText_WhenNoTypeSpecified()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>();

        cut.Find(".space-y-2").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Card type renders neba-card wrapper with three skeleton lines")]
    public void Render_ShouldRenderCardWithThreeLines_WhenTypeIsCard()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Card));

        cut.Find(".neba-card").ShouldNotBeNull();
        cut.FindAll(".neba-skeleton").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Card type first skeleton line is wide (w-3/4)")]
    public void Render_ShouldRenderWideFirstLine_WhenTypeIsCard()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Card));

        var skeletons = cut.FindAll(".neba-skeleton");
        skeletons[0].ClassList.ShouldContain("w-3/4");
    }

    [Fact(DisplayName = "Table type renders the default three rows")]
    public void Render_ShouldRenderThreeRows_WhenTypeIsTableWithDefaultRows()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table));

        cut.Find(".space-y-3").ShouldNotBeNull();
        cut.FindAll(".flex.gap-4").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Table type renders custom row count")]
    public void Render_ShouldRenderCustomRowCount_WhenTypeIsTableWithCustomRows()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table)
            .Add(x => x.Rows, 5));

        cut.FindAll(".flex.gap-4").Count.ShouldBe(5);
    }

    [Fact(DisplayName = "Table type renders three cells per row")]
    public void Render_ShouldRenderThreeCellsPerRow_WhenTypeIsTable()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table)
            .Add(x => x.Rows, 1));

        var row = cut.Find(".flex.gap-4");
        row.QuerySelectorAll(".neba-skeleton").Length.ShouldBe(3);
    }

    [Fact(DisplayName = "Text type renders the default three rows")]
    public void Render_ShouldRenderThreeRows_WhenTypeIsTextWithDefaultRows()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text));

        cut.Find(".space-y-2").ShouldNotBeNull();
        cut.FindAll(".neba-skeleton").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Text type last row is shorter (w-4/5)")]
    public void Render_ShouldRenderLastRowShorter_WhenTypeIsText()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text)
            .Add(x => x.Rows, 3));

        var skeletons = cut.FindAll(".neba-skeleton");
        skeletons[0].ClassList.ShouldContain("w-full");
        skeletons[1].ClassList.ShouldContain("w-full");
        skeletons[2].ClassList.ShouldContain("w-4/5");
    }

    [Fact(DisplayName = "Text type renders custom row count")]
    public void Render_ShouldRenderCustomRowCount_WhenTypeIsTextWithCustomRows()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text)
            .Add(x => x.Rows, 6));

        cut.FindAll(".neba-skeleton").Count.ShouldBe(6);
    }

    [Fact(DisplayName = "Avatar type renders skeleton circle")]
    public void Render_ShouldRenderCircle_WhenTypeIsAvatar()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar));

        cut.Find(".neba-skeleton.rounded-full").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Avatar type shows text lines by default")]
    public void Render_ShouldShowTextLines_WhenAvatarShowTextIsTrue()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar));

        cut.Find(".flex-1.space-y-2").ShouldNotBeNull();
        cut.FindAll(".flex-1.space-y-2 .neba-skeleton").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Avatar type hides text lines when ShowText is false")]
    public void Render_ShouldHideTextLines_WhenAvatarShowTextIsFalse()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar)
            .Add(x => x.ShowText, false));

        cut.FindAll(".flex-1.space-y-2").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Avatar type applies Size to circle inline style")]
    public void Render_ShouldApplySize_ToAvatarCircleStyle()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar)
            .Add(x => x.Size, 64));

        var circle = cut.Find(".neba-skeleton.rounded-full");
        var style = circle.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 64px");
        style.ShouldContain("height: 64px");
    }

    [Fact(DisplayName = "Custom type renders single skeleton with default dimensions")]
    public void Render_ShouldRenderDefaultDimensions_WhenTypeIsCustomWithNoParams()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom));

        var skeleton = cut.Find(".neba-skeleton");
        var style = skeleton.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 100%");
        style.ShouldContain("height: 1rem");
    }

    [Fact(DisplayName = "Custom type renders single skeleton with specified dimensions")]
    public void Render_ShouldRenderSpecifiedDimensions_WhenTypeIsCustomWithParams()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom)
            .Add(x => x.Width, "50%")
            .Add(x => x.Height, "600px"));

        var skeleton = cut.Find(".neba-skeleton");
        var style = skeleton.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 50%");
        style.ShouldContain("height: 600px");
    }

    [Fact(DisplayName = "Custom type renders a single skeleton element")]
    public void Render_ShouldRenderSingleElement_WhenTypeIsCustom()
    {
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom));

        cut.FindAll(".neba-skeleton").Count.ShouldBe(1);
    }
}
