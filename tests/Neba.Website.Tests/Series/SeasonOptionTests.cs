using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeasonOptionTests
{
    private static readonly SeasonOption Past = SeasonOptionFactory.Create(id: "past", startDate: new(2024, 1, 1), endDate: new(2024, 12, 31));
    private static readonly SeasonOption Current = SeasonOptionFactory.Create(id: "current", startDate: new(2025, 1, 1), endDate: new(2025, 12, 31));
    private static readonly SeasonOption Next = SeasonOptionFactory.Create(id: "next", startDate: new(2026, 1, 1), endDate: new(2026, 12, 31));
    private static readonly SeasonOption Later = SeasonOptionFactory.Create(id: "later", startDate: new(2027, 1, 1), endDate: new(2027, 12, 31));

    [Fact(DisplayName = "Default should pick the season that contains today")]
    public void Default_ShouldPickCurrentSeason()
    {
        // Arrange
        IReadOnlyList<SeasonOption> seasons = [Later, Next, Current, Past];

        // Act
        var result = SeasonOption.Default(seasons, new DateOnly(2025, 6, 1));

        // Assert
        result.ShouldBe(Current);
    }

    [Fact(DisplayName = "Default should pick the next season to start when none contains today")]
    public void Default_ShouldPickNextSeason_WhenNoneCurrent()
    {
        // Arrange
        IReadOnlyList<SeasonOption> seasons = [Later, Next, Past];

        // Act
        var result = SeasonOption.Default(seasons, new DateOnly(2025, 6, 1));

        // Assert
        result.ShouldBe(Next);
    }

    [Fact(DisplayName = "Default should pick the newest season when all have ended")]
    public void Default_ShouldPickNewest_WhenAllEnded()
    {
        // Arrange
        IReadOnlyList<SeasonOption> seasons = [Past, Current];

        // Act
        var result = SeasonOption.Default(seasons, new DateOnly(2030, 1, 1));

        // Assert
        result.ShouldBe(Current);
    }

    [Fact(DisplayName = "Default should be null when there are no seasons")]
    public void Default_ShouldBeNull_WhenEmpty()
    {
        // Arrange & Act
        var result = SeasonOption.Default([], new DateOnly(2025, 6, 1));

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "From should label the season with its date range")]
    public void From_ShouldLabelWithRange()
    {
        // Arrange
        var response = SeasonResponseFactory.Create();

        // Act
        var option = SeasonOption.From(response);

        // Assert
        option.Id.ShouldBe(response.Id);
        option.StartDate.ShouldBe(response.StartDate);
        option.EndDate.ShouldBe(response.EndDate);
        option.Label.ShouldBe(response.Description + " (" + SeriesDisplay.SeasonRange(response.StartDate, response.EndDate) + ")");
    }
}
