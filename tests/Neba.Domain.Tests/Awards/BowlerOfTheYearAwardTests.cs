using Neba.Domain.Awards;
using Neba.Domain.Bowlers;

namespace Neba.Domain.Tests.Awards;

public sealed class BowlerOfTheYearAwardTests
{
    [Fact(DisplayName = "Create should return an error when bowler ID is empty")]
    public void Create_ShouldReturnAnError_WhenBowlerIdIsEmpty()
    {
        // Arrange
        var emptyBowlerId = BowlerId.Empty;
        var category = BowlerOfTheYearCategory.Open;

        // Act
        var result = BowlerOfTheYearAward.Create(emptyBowlerId, category);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "Create should return a BowlerOfTheYearAward when bowler ID is valid")]
    public void Create_ShouldReturnAward_WhenBowlerIdIsValid()
    {
        // Arrange
        var validBowlerId = BowlerId.New();
        var category = BowlerOfTheYearCategory.Senior;

        // Act
        var result = BowlerOfTheYearAward.Create(validBowlerId, category);

        // Assert
        result.IsError.ShouldBeFalse();

        var award = result.Value;
        award.BowlerId.ShouldBe(validBowlerId);
        award.Category.ShouldBe(category);
    }
}