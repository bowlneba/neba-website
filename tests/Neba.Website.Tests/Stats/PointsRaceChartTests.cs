using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;

using PointsRaceChartComponent = Neba.Website.Server.Stats.PointsRaceChart;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.PointsRaceChart")]
public sealed class PointsRaceChartTests : IDisposable
{
    private readonly BunitContext _ctx;

    public PointsRaceChartTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not render chart container when no series are provided")]
    public void Render_ShouldNotRenderContainer_WhenSeriesIsEmpty()
    {
        // Act
        var cut = _ctx.Render<PointsRaceChartComponent>(p => p
            .Add(x => x.Series, []));

        // Assert
        cut.FindAll(".points-race-chart").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render compact chart with legend items when series are provided")]
    public void Render_ShouldRenderCompactChartAndLegend_WhenSeriesProvided()
    {
        // Arrange
        var series = PointsRaceSeriesViewModelFactory.Bogus(2, seed: 1101);

        // Act
        var cut = _ctx.Render<PointsRaceChartComponent>(p => p
            .Add(x => x.Series, series));

        // Assert
        (cut.Find(".points-race-chart").ClassName ?? string.Empty).ShouldContain("points-race-chart--compact");
        cut.FindAll(".points-race-chart-legend-item").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should show reset action after hiding a series and clear it when reset is clicked")]
    public async Task Legend_ShouldShowAndClearReset_WhenSeriesVisibilityChanges()
    {
        // Arrange
        var series = PointsRaceSeriesViewModelFactory.Bogus(2, seed: 1102);
        var cut = _ctx.Render<PointsRaceChartComponent>(p => p
            .Add(x => x.Series, series)
            .Add(x => x.ShowCategoryLabels, true));

        // Act
        await cut.FindAll(".points-race-chart-legend-item")[0].ClickAsync(new());

        // Assert
        cut.FindAll(".points-race-chart-legend-reset").Count.ShouldBe(1);
        cut.FindAll(".points-race-chart-legend-item--hidden").Count.ShouldBe(1);

        // Act
        await cut.Find(".points-race-chart-legend-reset").ClickAsync(new());

        // Assert
        cut.FindAll(".points-race-chart-legend-reset").Count.ShouldBe(0);
        cut.FindAll(".points-race-chart-legend-item--hidden").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should render static non-interactive legend items when AllowSeriesToggle is false")]
    public void Render_ShouldRenderStaticLegendItems_WhenAllowSeriesToggleIsFalse()
    {
        // Arrange
        var series = PointsRaceSeriesViewModelFactory.Bogus(2, seed: 1104);

        // Act
        var cut = _ctx.Render<PointsRaceChartComponent>(p => p
            .Add(x => x.Series, series)
            .Add(x => x.AllowSeriesToggle, false));

        // Assert
        cut.FindAll(".points-race-chart-legend-item--static").Count.ShouldBe(2);
        cut.FindAll("[aria-pressed]").Count.ShouldBe(0);
        cut.FindAll(".points-race-chart-legend-reset").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should remove hidden series IDs and hide the reset button when the series list is updated to exclude them")]
    public async Task OnParametersSet_ShouldRemoveStaleHiddenSeries_WhenSeriesRemoved()
    {
        // Arrange
        var series = PointsRaceSeriesViewModelFactory.Bogus(2, seed: 1105);
        var cut = _ctx.Render<PointsRaceChartComponent>(p => p
            .Add(x => x.Series, series)
            .Add(x => x.ShowCategoryLabels, true));

        await cut.FindAll(".points-race-chart-legend-item")[0].ClickAsync(new());
        cut.FindAll(".points-race-chart-legend-reset").Count.ShouldBe(1);

        // Act — remove the series that was hidden
        cut.Render(p => p.Add(x => x.Series, [.. series.Skip(1)]));

        // Assert
        cut.FindAll(".points-race-chart-legend-reset").Count.ShouldBe(0);
        cut.FindAll(".points-race-chart-legend-item--hidden").Count.ShouldBe(0);
    }
}