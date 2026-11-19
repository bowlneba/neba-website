using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesFieldChartTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<SeriesFieldChart> Render(IReadOnlyList<FieldStep> steps, bool hasFinale, bool hasPlacementStage)
        => _ctx.Render<SeriesFieldChart>(p => p
            .Add(x => x.Steps, steps)
            .Add(x => x.HasFinale, hasFinale)
            .Add(x => x.HasPlacementStage, hasPlacementStage));

    [Fact(DisplayName = "Should render one row per step with its label, detail and count")]
    public void Render_ShouldShowRowPerStep()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps =
        [
            FieldStepFactory.Create(label: "Qualifiers", detail: "2 events, top finishers", count: 40),
            FieldStepFactory.Create(label: "Survivor", detail: "Cut by squad", count: 20, kind: StageKinds.Survivor),
        ];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: false);

        // Assert
        var rows = cut.FindAll(".series-field__row");
        rows.Count.ShouldBe(2);
        rows[0].QuerySelector(".series-field__label")!.TextContent.ShouldContain("Qualifiers");
        rows[0].QuerySelector("small")!.TextContent.ShouldBe("2 events, top finishers");
        rows[0].QuerySelector(".series-field__bar")!.TextContent.ShouldBe("40");
        rows[1].QuerySelector(".series-field__bar")!.TextContent.ShouldBe("20");
    }

    [Fact(DisplayName = "Should size each bar against the widest step and color it by Stage kind")]
    public void Render_ShouldSizeAndColorBars()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps =
        [
            FieldStepFactory.Create(count: 40),
            FieldStepFactory.Create(count: 20, kind: StageKinds.Survivor),
        ];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: false);

        // Assert
        var bars = cut.FindAll(".series-field__bar");
        bars[0].GetAttribute("style").ShouldBe("width:100%");
        bars[0].ClassList.ShouldContain("kind-qualifying");
        bars[1].GetAttribute("style").ShouldBe("width:50%");
        bars[1].ClassList.ShouldContain("kind-survivor");
    }

    [Fact(DisplayName = "Should prefix a ceiling count with Up to")]
    public void Render_ShouldPrefixCeilingCount()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps = [FieldStepFactory.Create(count: 24, isCeiling: true)];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: false);

        // Assert
        cut.Find(".series-field__bar").TextContent.ShouldBe("Up to 24");
    }

    [Fact(DisplayName = "Should mention the Finale in the intro when the Series has one")]
    public void Render_ShouldMentionFinale_WhenSeriesHasFinale()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps = [FieldStepFactory.Create()];

        // Act
        var cut = Render(steps, hasFinale: true, hasPlacementStage: true);

        // Assert
        cut.Find("p").TextContent.ShouldContain("decides the Finale");
        cut.FindAll(".series-field__caption").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should explain the final standings and caption the Placement Round when there is no Finale")]
    public void Render_ShouldCaptionPlacementRound_WhenNoFinale()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps = [FieldStepFactory.Create()];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: true);

        // Assert
        cut.Find("p").TextContent.ShouldContain("decides the final standings");
        cut.Find(".series-field__caption").TextContent.ShouldContain("No Finale");
    }

    [Fact(DisplayName = "Should leave out the caption when there is no Finale and no Placement Stage")]
    public void Render_ShouldOmitCaption_WhenNoFinaleAndNoPlacementStage()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps = [FieldStepFactory.Create()];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: false);

        // Assert
        cut.FindAll(".series-field__caption").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render no rows for an empty step list")]
    public void Render_ShouldShowNoRows_WhenNoSteps()
    {
        // Arrange
        IReadOnlyList<FieldStep> steps = [];

        // Act
        var cut = Render(steps, hasFinale: false, hasPlacementStage: false);

        // Assert
        cut.FindAll(".series-field__row").ShouldBeEmpty();
    }
}