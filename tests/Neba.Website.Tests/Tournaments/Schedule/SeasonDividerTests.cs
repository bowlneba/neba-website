using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.SeasonDivider")]
public sealed class SeasonDividerTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show the season description and date range as a separator")]
    public void Render_ShouldShowDescriptionAndRange_AsSeparator()
    {
        // Arrange
        var season = SeasonViewModelFactory.Create(
            description: "2027 Season", startDate: new DateOnly(2027, 1, 1), endDate: new DateOnly(2027, 12, 31));

        // Act
        var cut = _ctx.Render<SeasonDivider>(parameters => parameters.Add(p => p.Season, season));

        // Assert
        cut.Find(".season-divider").GetAttribute("role").ShouldBe("separator");
        cut.Find(".season-divider").GetAttribute("aria-label").ShouldBe("2027 Season begins");
        cut.Find(".season-divider__label").TextContent.ShouldBe("2027 Season");
        cut.Find(".season-divider__range").TextContent.ShouldBe(season.FormatDateRange());
    }
}
