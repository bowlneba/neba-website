using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Awards;

[UnitTest]
[Component("Awards.HighBlockAward")]
public sealed class HighBlockAwardTests
{
    [Fact(DisplayName = "Create should return an error when bowler ID is empty")]
    public void Create_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Arrange
        var bowlerId = BowlerId.Empty;
        const int blockScore = 1200;
        const int games = 5;

        // Act
        var result = HighBlockAward.Create(bowlerId, blockScore, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighBlockAwardErrors.BowlerIdRequired);
    }

    [Theory(DisplayName = "Create should return an error when block score is invalid")]
    [InlineData(-1, TestDisplayName = "Block score of -1 should be invalid")]
    [InlineData(0, TestDisplayName = "Block score of 0 should be invalid")]
    public void Create_ShouldReturnError_WhenBlockScoreIsInvalid(int blockScore)
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const int games = 5;

        // Act
        var result = HighBlockAward.Create(bowlerId, blockScore, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighBlockAwardErrors.InvalidBlockScore);
    }

    [Fact(DisplayName = "Create should return an error when block score exceeds maximum possible score")]
    public void Create_ShouldReturnError_WhenBlockScoreExceedsMaximum()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const int blockScore = 1501;
        const int games = 5;

        // Act
        var result = HighBlockAward.Create(bowlerId, blockScore, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("HighBlockAward.BlockScoreExceedsMaximum");
        result.FirstError.Metadata!["MaximumBlockScore"].ShouldBe(1500);
    }

    [Fact(DisplayName = "Create should return a HighBlockAward when block score equals the maximum possible score")]
    public void Create_ShouldReturnAward_WhenBlockScoreEqualsMaximum()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const int games = 5;
        const int blockScore = games * 300; // exactly 1500 — boundary value; > is the invariant, not >=

        // Act
        var result = HighBlockAward.Create(bowlerId, blockScore, games);

        // Assert
        result.IsError.ShouldBeFalse();

        var award = result.Value;
        award.BowlerId.ShouldBe(bowlerId);
        award.BlockScore.ShouldBe(blockScore);
    }
}