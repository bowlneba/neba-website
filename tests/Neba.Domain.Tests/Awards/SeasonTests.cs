using ErrorOr;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Awards;

namespace Neba.Domain.Tests.Awards;

public sealed class SeasonTests
{
    [Fact]
    public void AddBowlerOfTheYearWinner_ShouldReturnAnError_WhenSeasonNotComplete()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: false);

        var bowlerId = BowlerId.New();
        var category = BowlerOfTheYearCategory.Open;

        // Act
        var result = season.AddBowlerOfTheYearWinner(bowlerId, category);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact]
    public void AddBowlerOfTheYearWinner_ShouldAddAward_WhenSeasonComplete()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var bowlerId = BowlerId.New();
        var category = BowlerOfTheYearCategory.Senior;

        // Act
        var result = season.AddBowlerOfTheYearWinner(bowlerId, category);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(category);
    }
}