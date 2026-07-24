using Bunit;

using ErrorOr;

using Neba.Api.Contracts.OilPatterns;
using Neba.Api.Contracts.OilPatterns.CreateOilPattern;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.OilPatterns;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.OilPatternPicker")]
public sealed class OilPatternPickerTests : IDisposable
{
    private readonly BunitContext _ctx;

    public OilPatternPickerTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<OilPatternPicker> Render(
        IReadOnlyCollection<OilPatternSummaryResponse>? patterns = null,
        string? initialPatternLengthCategory = null,
        string? initialPatternRatioCategory = null,
        Action<OilPatternSelection>? onSelectionChanged = null)
        => _ctx.Render<OilPatternPicker>(p =>
        {
            p.Add(x => x.Patterns, patterns ?? []);
            p.Add(x => x.OnCreatePatternRequestedAsync,
                (Func<CreateOilPatternRequest, CancellationToken, Task<ErrorOr<CreatedOilPatternResponse>>>)((_, _) =>
                    throw new InvalidOperationException("Not expected to be called in these tests.")));

            if (initialPatternLengthCategory is not null)
            {
                p.Add(x => x.InitialPatternLengthCategory, initialPatternLengthCategory);
            }

            if (initialPatternRatioCategory is not null)
            {
                p.Add(x => x.InitialPatternRatioCategory, initialPatternRatioCategory);
            }

            if (onSelectionChanged is not null)
            {
                p.Add(x => x.SelectionChanged, onSelectionChanged);
            }
        });

    [Fact(DisplayName = "Should pre-fill the manual length and ratio category selects when initial categories are provided")]
    public void Render_ShouldPrefillManualCategorySelects_WhenInitialCategoriesProvided()
    {
        // Act
        var cut = Render(initialPatternLengthCategory: "Long", initialPatternRatioCategory: "Sport");

        // Assert
        cut.Find("#manual-length-category").GetAttribute("value").ShouldBe("Long");
        cut.Find("#manual-ratio-category").GetAttribute("value").ShouldBe("Sport");
    }

    [Fact(DisplayName = "Should emit the pre-filled categories via SelectionChanged on first render, without any user interaction")]
    public void Render_ShouldEmitPrefilledCategories_OnFirstRender()
    {
        // Arrange
        OilPatternSelection? emitted = null;

        // Act
        Render(
            initialPatternLengthCategory: "Long",
            initialPatternRatioCategory: "Sport",
            onSelectionChanged: selection => emitted = selection);

        // Assert
        emitted.ShouldNotBeNull();
        emitted.PatternLengthCategory.ShouldBe("Long");
        emitted.PatternRatioCategory.ShouldBe("Sport");
        emitted.OilPatternId.ShouldBeNull();
    }

    [Fact(DisplayName = "Should leave the manual category selects at their default when no initial categories are provided")]
    public void Render_ShouldLeaveSelectsAtDefault_WhenNoInitialCategoriesProvided()
    {
        // Act
        var cut = Render();

        // Assert
        cut.Find("#manual-length-category").GetAttribute("value").ShouldBe(string.Empty);
        cut.Find("#manual-ratio-category").GetAttribute("value").ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Should not re-seed the manual categories on a later parent re-render, once the user has changed them")]
    public void Rerender_ShouldNotClobberUserEdit_AfterInitialSeed()
    {
        // Arrange
        var cut = Render(initialPatternLengthCategory: "Long", initialPatternRatioCategory: "Sport");
        cut.Find("#manual-length-category").Change("Short");

        // Act — a parent re-render passing the same initial-category parameters again (e.g. the
        // hosting page re-rendering for an unrelated reason) must not clobber the user's own edit.
        cut.Render();

        // Assert
        cut.Find("#manual-length-category").GetAttribute("value").ShouldBe("Short");
    }

    [Fact(DisplayName = "Should default to the No Pattern mode with the manual categories visible")]
    public void Render_ShouldDefaultToNoPatternMode()
    {
        // Act
        var cut = Render();

        // Assert
        cut.Find(".neba-segment-selected").TextContent.ShouldBe("No Pattern");
        cut.FindAll("#manual-length-category").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should switch to the existing-pattern picker when Pick Existing is clicked")]
    public void Click_ShouldSwitchToPickExistingMode()
    {
        // Arrange
        var pattern = OilPatternSummaryResponseFactory.Create(oilPatternId: "01J7ZK8X6ZQJ8V3F8N9T9C9R2E");
        var cut = Render(patterns: [pattern]);

        // Act
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Pick Existing").Click();

        // Assert
        cut.FindAll("#pattern-select").Count.ShouldBe(1);
        cut.FindAll("#manual-length-category").ShouldBeEmpty();
    }
}
