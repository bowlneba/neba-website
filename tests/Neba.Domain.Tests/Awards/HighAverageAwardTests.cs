using Neba.Domain.Awards;
using Neba.Domain.Bowlers;

namespace Neba.Domain.Tests.Awards;

public sealed class HighAverageAwardTests
{
    [Fact(DisplayName = "Create should return an error when bowler ID is empty")]
    public void Create_ShouldReturnAnError_WhenBowlerIdIsEmpty()
    {
        // Arrange
        var emptyBowlerId = BowlerId.Empty;
        const decimal average = 200.0m;
        const int totalGames = 50;
        const int tournamentsParticipated = 5;

        // Act
        var result = HighAverageAward.Create(emptyBowlerId, average, totalGames, tournamentsParticipated);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighAverageAwardErrors.BowlerIdRequired);
    }

    [Theory(DisplayName = "Create should return an error when average is invalid")]
    [InlineData(-1.0, TestDisplayName = "Average of -1.0 should be invalid")]
    [InlineData(0.0, TestDisplayName = "Average of 0.0 should be invalid")]
    public void Create_ShouldReturnAnError_WhenAverageIsInvalid(double average)
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const int totalGames = 50;
        const int tournamentsParticipated = 5;

        // Act
        var result = HighAverageAward.Create(bowlerId, (decimal)average, totalGames, tournamentsParticipated);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighAverageAwardErrors.InvalidAverage);
    }

    [Theory(DisplayName = "Create should return an error when total games is invalid")]
    [InlineData(-1, TestDisplayName = "Total games of -1 should be invalid")]
    [InlineData(0, TestDisplayName = "Total games of 0 should be invalid")]
    public void Create_ShouldReturnAnError_WhenTotalGamesIsInvalid(int totalGames)
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const decimal average = 200.0m;
        const int tournamentsParticipated = 5;

        // Act
        var result = HighAverageAward.Create(bowlerId, average, totalGames, tournamentsParticipated);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighAverageAwardErrors.InvalidTotalGames);
    }

    [Theory(DisplayName = "Create should return an error when tournaments participated is invalid")]
    [InlineData(-1, TestDisplayName = "Tournaments participated of -1 should be invalid")]
    [InlineData(0, TestDisplayName = "Tournaments participated of 0 should be invalid")]
    public void Create_ShouldReturnAnError_WhenTournamentsParticipatedIsInvalid(int tournamentsParticipated)
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const decimal average = 200.0m;
        const int totalGames = 50;

        // Act
        var result = HighAverageAward.Create(bowlerId, average, totalGames, tournamentsParticipated);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighAverageAwardErrors.InvalidTournamentsParticipated);
    }

    [Fact(DisplayName = "Create should return a HighAverageAward when inputs are valid")]
    public void Create_ShouldReturnAward_WhenInputsAreValid()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        const decimal average = 200.0m;
        const int totalGames = 50;
        const int tournamentsParticipated = 5;

        // Act
        var result = HighAverageAward.Create(bowlerId, average, totalGames, tournamentsParticipated);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Average.ShouldBe(average);
        result.Value.TotalGames.ShouldBe(totalGames);
        result.Value.TournamentsParticipated.ShouldBe(tournamentsParticipated);
    }
}