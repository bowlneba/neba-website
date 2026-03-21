using ErrorOr;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Awards;

namespace Neba.Domain.Tests.Awards;

public sealed class SeasonTests
{
    [Fact(DisplayName = "AddBowlerOfTheYearWinner should return an error when season is not complete")]
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

    [Fact(DisplayName = "AddBowlerOfTheYearWinner should return an error when BowlerOfTheYearAward creation fails")]
    public void AddBowlerOfTheYearWinner_ShouldReturnAnError_WhenAwardCreationFails()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var invalidBowlerId = BowlerId.Empty;
        var category = BowlerOfTheYearCategory.Open;

        // Act
        var result = season.AddBowlerOfTheYearWinner(invalidBowlerId, category);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddBowlerOfTheYearWinner should add award when season is complete")]
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

    [Fact(DisplayName = "AddHighBlockWinner should return an error when season is not complete")]
    public void AddHighBlockWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: false);
        var bowlerId = BowlerId.New();
        const int score = 1200;
        const int games = 5;

        // Act
        var result = season.AddHighBlockWinner(bowlerId, score, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddHighBlockWinner should return an error when block scores mismatch")]
    public void AddHighBlockWinner_ShouldReturnError_WhenBlockScoresMismatch()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var bowlerId1 = BowlerId.New();
        const int score1 = 1200;
        const int games = 5;

        var bowlerId2 = BowlerId.New();
        const int score2 = 1100;

        season.AddHighBlockWinner(bowlerId1, score1, games).ShouldBe(Result.Success);

        // Act
        var result = season.AddHighBlockWinner(bowlerId2, score2, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.HighBlockScoreMismatch);
    }

    [Fact(DisplayName = "AddHighBlockWinner should return an error when bowler has already received a High Block award")]
    public void AddHighBlockWinner_ShouldReturnError_WhenBowlerAlreadyAwarded()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var bowlerId = BowlerId.New();
        const int score1 = 1200;
        const int score2 = 1200;
        const int games = 5;

        season.AddHighBlockWinner(bowlerId, score1, games).ShouldBe(Result.Success);

        // Act
        var result = season.AddHighBlockWinner(bowlerId, score2, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.BowlerAlreadyAwarded);
    }

    [Fact]
    public void AddHighBlockWinner_ShouldReturnError_WhenHighBlockAwardCreationFails()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var bowlerId = BowlerId.New();
        const int invalidScore = -1;
        const int games = 5;

        // Act
        var result = season.AddHighBlockWinner(bowlerId, invalidScore, games);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighBlockAwardErrors.InvalidBlockScore);
    }

    [Fact(DisplayName = "AddHighBlockWinner should add award when inputs are valid")]
    public void AddHighBlockWinner_ShouldAddAward_WhenInputsAreValid()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);

        var bowlerId = BowlerId.New();
        const int score = 1200;
        const int games = 5;

        // Act
        var result = season.AddHighBlockWinner(bowlerId, score, games);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.HighBlockAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.BlockScore.ShouldBe(score);
    }
}