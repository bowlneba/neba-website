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
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>();

        // Assert
        cut.Find(".space-y-2").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Card type renders neba-card wrapper with three skeleton lines")]
    public void Render_ShouldRenderCardWithThreeLines_WhenTypeIsCard()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Card));

        // Assert
        cut.Find(".neba-card").ShouldNotBeNull();
        cut.FindAll(".neba-skeleton").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Card type first skeleton line is wide (w-3/4)")]
    public void Render_ShouldRenderWideFirstLine_WhenTypeIsCard()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Card));

        // Assert
        var skeletons = cut.FindAll(".neba-skeleton");
        skeletons[0].ClassList.ShouldContain("w-3/4");
    }

    [Fact(DisplayName = "Table type renders the default three rows")]
    public void Render_ShouldRenderThreeRows_WhenTypeIsTableWithDefaultRows()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table));

        // Assert
        cut.Find(".space-y-3").ShouldNotBeNull();
        cut.FindAll(".flex.gap-4").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Table type renders custom row count")]
    public void Render_ShouldRenderCustomRowCount_WhenTypeIsTableWithCustomRows()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table)
            .Add(x => x.Rows, 5));

        // Assert
        cut.FindAll(".flex.gap-4").Count.ShouldBe(5);
    }

    [Fact(DisplayName = "Table type renders three cells per row")]
    public void Render_ShouldRenderThreeCellsPerRow_WhenTypeIsTable()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Table)
            .Add(x => x.Rows, 1));

        // Assert
        var row = cut.Find(".flex.gap-4");
        row.QuerySelectorAll(".neba-skeleton").Length.ShouldBe(3);
    }

    [Fact(DisplayName = "Text type renders the default three rows")]
    public void Render_ShouldRenderThreeRows_WhenTypeIsTextWithDefaultRows()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text));

        // Assert
        cut.Find(".space-y-2").ShouldNotBeNull();
        cut.FindAll(".neba-skeleton").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Text type last row is shorter (w-4/5)")]
    public void Render_ShouldRenderLastRowShorter_WhenTypeIsText()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text)
            .Add(x => x.Rows, 3));

        // Assert
        var skeletons = cut.FindAll(".neba-skeleton");
        skeletons[0].ClassList.ShouldContain("w-full");
        skeletons[1].ClassList.ShouldContain("w-full");
        skeletons[2].ClassList.ShouldContain("w-4/5");
    }

    [Fact(DisplayName = "Text type renders custom row count")]
    public void Render_ShouldRenderCustomRowCount_WhenTypeIsTextWithCustomRows()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Text)
            .Add(x => x.Rows, 6));

        // Assert
        cut.FindAll(".neba-skeleton").Count.ShouldBe(6);
    }

    [Fact(DisplayName = "Avatar type renders skeleton circle")]
    public void Render_ShouldRenderCircle_WhenTypeIsAvatar()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar));

        // Assert
        cut.Find(".neba-skeleton.rounded-full").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Avatar type shows text lines by default")]
    public void Render_ShouldShowTextLines_WhenAvatarShowTextIsTrue()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar));

        // Assert
        cut.Find(".flex-1.space-y-2").ShouldNotBeNull();
        cut.FindAll(".flex-1.space-y-2 .neba-skeleton").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Avatar type hides text lines when ShowText is false")]
    public void Render_ShouldHideTextLines_WhenAvatarShowTextIsFalse()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar)
            .Add(x => x.ShowText, false));

        // Assert
        cut.FindAll(".flex-1.space-y-2").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Avatar type applies Size to circle inline style")]
    public void Render_ShouldApplySize_ToAvatarCircleStyle()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Avatar)
            .Add(x => x.Size, 64));

        // Assert
        var circle = cut.Find(".neba-skeleton.rounded-full");
        var style = circle.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 64px");
        style.ShouldContain("height: 64px");
    }

    [Fact(DisplayName = "Custom type renders single skeleton with default dimensions")]
    public void Render_ShouldRenderDefaultDimensions_WhenTypeIsCustomWithNoParams()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom));

        // Assert
        var skeleton = cut.Find(".neba-skeleton");
        var style = skeleton.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 100%");
        style.ShouldContain("height: 1rem");
    }

    [Fact(DisplayName = "Custom type renders single skeleton with specified dimensions")]
    public void Render_ShouldRenderSpecifiedDimensions_WhenTypeIsCustomWithParams()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom)
            .Add(x => x.Width, "50%")
            .Add(x => x.Height, "600px"));

        // Assert
        var skeleton = cut.Find(".neba-skeleton");
        var style = skeleton.GetAttribute("style") ?? string.Empty;
        style.ShouldContain("width: 50%");
        style.ShouldContain("height: 600px");
    }

    [Fact(DisplayName = "Custom type renders a single skeleton element")]
    public void Render_ShouldRenderSingleElement_WhenTypeIsCustom()
    {
        // Act
        var cut = _ctx.Render<NebaSkeletonLoader>(p => p
            .Add(x => x.Type, SkeletonType.Custom));

        // Assert
        cut.FindAll(".neba-skeleton").Count.ShouldBe(1);
    }
}
