using ErrorOr;

using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Domain.Tests.Awards;

[UnitTest]
[Component("Awards.Season")]
public sealed class SeasonTests
{
    // ── AddOpenBowlerOfTheYearWinner ──────────────────────────────────────────

    [Fact(DisplayName = "AddOpenBowlerOfTheYearWinner should return an error when season is not complete")]
    public void AddOpenBowlerOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddOpenBowlerOfTheYearWinner(BowlerId.New());

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddOpenBowlerOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddOpenBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddOpenBowlerOfTheYearWinner(BowlerId.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddOpenBowlerOfTheYearWinner should add award when inputs are valid")]
    public void AddOpenBowlerOfTheYearWinner_ShouldAddAward_WhenInputsAreValid()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddOpenBowlerOfTheYearWinner(bowlerId);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Open);
    }

    [Fact(DisplayName = "AddOpenBowlerOfTheYearWinner should allow multiple bowlers to win the same category")]
    public void AddOpenBowlerOfTheYearWinner_ShouldAddAward_WhenSameCategoryAwardedToMultipleBowlers()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId1 = BowlerId.New();
        var bowlerId2 = BowlerId.New();

        season.AddOpenBowlerOfTheYearWinner(bowlerId1).ShouldBe(Result.Success);

        var result = season.AddOpenBowlerOfTheYearWinner(bowlerId2);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);
        season.BowlerOfTheYearAwards.Count.ShouldBe(2);
    }

    // ── AddWomanOfTheYearWinner ───────────────────────────────────────────────

    [Fact(DisplayName = "AddWomanOfTheYearWinner should return an error when season is not complete")]
    public void AddWomanOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddWomanOfTheYearWinner(BowlerId.New(), Gender.Female);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddWomanOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddWomanOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddWomanOfTheYearWinner(BowlerId.Empty, Gender.Female);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddWomanOfTheYearWinner should return an error when gender is not female")]
    public void AddWomanOfTheYearWinner_ShouldReturnError_WhenGenderIsNotFemale()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddWomanOfTheYearWinner(BowlerId.New(), Gender.Male);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.NotFemale);
    }

    [Fact(DisplayName = "AddWomanOfTheYearWinner should add award when bowler is female")]
    public void AddWomanOfTheYearWinner_ShouldAddAward_WhenBowlerIsFemale()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddWomanOfTheYearWinner(bowlerId, Gender.Female);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Woman);
    }

    // ── AddSeniorBowlerOfTheYearWinner ────────────────────────────────────────

    [Fact(DisplayName = "AddSeniorBowlerOfTheYearWinner should return an error when season is not complete")]
    public void AddSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddSeniorBowlerOfTheYearWinner(BowlerId.New(), age: 55);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddSeniorBowlerOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddSeniorBowlerOfTheYearWinner(BowlerId.Empty, age: 55);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddSeniorBowlerOfTheYearWinner should return an error when age is below 50")]
    public void AddSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenAgeIsBelow50()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddSeniorBowlerOfTheYearWinner(BowlerId.New(), age: 49);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.InsufficientAgeForSenior);
    }

    [Fact(DisplayName = "AddSeniorBowlerOfTheYearWinner should add award when age is at least 50")]
    public void AddSeniorBowlerOfTheYearWinner_ShouldAddAward_WhenAgeIsAtLeast50()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddSeniorBowlerOfTheYearWinner(bowlerId, age: 50);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Senior);
    }

    // ── AddSuperSeniorBowlerOfTheYearWinner ───────────────────────────────────

    [Fact(DisplayName = "AddSuperSeniorBowlerOfTheYearWinner should return an error when season is not complete")]
    public void AddSuperSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddSuperSeniorBowlerOfTheYearWinner(BowlerId.New(), age: 65);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddSuperSeniorBowlerOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddSuperSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddSuperSeniorBowlerOfTheYearWinner(BowlerId.Empty, age: 65);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddSuperSeniorBowlerOfTheYearWinner should return an error when age is below 60")]
    public void AddSuperSeniorBowlerOfTheYearWinner_ShouldReturnError_WhenAgeIsBelow60()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddSuperSeniorBowlerOfTheYearWinner(BowlerId.New(), age: 59);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.InsufficientAgeForSuperSenior);
    }

    [Fact(DisplayName = "AddSuperSeniorBowlerOfTheYearWinner should add award when age is at least 60")]
    public void AddSuperSeniorBowlerOfTheYearWinner_ShouldAddAward_WhenAgeIsAtLeast60()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddSuperSeniorBowlerOfTheYearWinner(bowlerId, age: 60);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.SuperSenior);
    }

    // ── AddRookieBowlerOfTheYearWinner ────────────────────────────────────────

    [Fact(DisplayName = "AddRookieBowlerOfTheYearWinner should return an error when season is not complete")]
    public void AddRookieBowlerOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddRookieBowlerOfTheYearWinner(BowlerId.New(), isRookie: true);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddRookieBowlerOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddRookieBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddRookieBowlerOfTheYearWinner(BowlerId.Empty, isRookie: true);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddRookieBowlerOfTheYearWinner should return an error when bowler is not a rookie")]
    public void AddRookieBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIsNotARookie()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddRookieBowlerOfTheYearWinner(BowlerId.New(), isRookie: false);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.NotARookie);
    }

    [Fact(DisplayName = "AddRookieBowlerOfTheYearWinner should add award when bowler is a new member")]
    public void AddRookieBowlerOfTheYearWinner_ShouldAddAward_WhenBowlerIsARookie()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddRookieBowlerOfTheYearWinner(bowlerId, isRookie: true);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Rookie);
    }

    // ── AddYouthBowlerOfTheYearWinner ─────────────────────────────────────────

    [Fact(DisplayName = "AddYouthBowlerOfTheYearWinner should return an error when season is not complete")]
    public void AddYouthBowlerOfTheYearWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        var season = SeasonFactory.Create(complete: false);

        var result = season.AddYouthBowlerOfTheYearWinner(BowlerId.New(), age: 16);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddYouthBowlerOfTheYearWinner should return an error when bowler ID is empty")]
    public void AddYouthBowlerOfTheYearWinner_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddYouthBowlerOfTheYearWinner(BowlerId.Empty, age: 16);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddYouthBowlerOfTheYearWinner should return an error when age is 18 or older")]
    public void AddYouthBowlerOfTheYearWinner_ShouldReturnError_WhenAgeIs18OrOlder()
    {
        var season = SeasonFactory.Create(complete: true);

        var result = season.AddYouthBowlerOfTheYearWinner(BowlerId.New(), age: 18);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.AgeExceedsYouthLimit);
    }

    [Fact(DisplayName = "AddYouthBowlerOfTheYearWinner should add award when age is under 18")]
    public void AddYouthBowlerOfTheYearWinner_ShouldAddAward_WhenAgeIsUnder18()
    {
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();

        var result = season.AddYouthBowlerOfTheYearWinner(bowlerId, age: 17);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.BowlerOfTheYearAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Youth);
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
        result.FirstError.ShouldBe(SeasonErrors.BowlerAlreadyAwardedHighBlock);
    }

    [Fact(DisplayName = "AddHighBlockWinner should return an error when HighBlockAward creation fails")]
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

    [Fact(DisplayName = "AddHighAverageWinner should return an error when season is not complete")]
    public void AddHighAverageWinner_ShouldReturnError_WhenSeasonNotComplete()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: false);
        var bowlerId = BowlerId.New();

        // Act
        var result = season.AddHighAverageWinner(bowlerId, average: 210m, totalGames: 20, tournamentsParticipated: 4, statEligibleTournamentCount: 4);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.SeasonNotComplete);
    }

    [Fact(DisplayName = "AddHighAverageWinner should return an error when averages mismatch")]
    public void AddHighAverageWinner_ShouldReturnError_WhenAverageMismatch()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);
        const int totalGames = 20;
        const int tournamentsParticipated = 4;
        const int statEligibleTournamentCount = 4;

        season.AddHighAverageWinner(BowlerId.New(), average: 210m, totalGames, tournamentsParticipated, statEligibleTournamentCount).ShouldBe(Result.Success);

        // Act
        var result = season.AddHighAverageWinner(BowlerId.New(), average: 200m, totalGames, tournamentsParticipated, statEligibleTournamentCount);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.HighAverageMismatch);
    }

    [Fact(DisplayName = "AddHighAverageWinner should return an error when bowler has already received a High Average award")]
    public void AddHighAverageWinner_ShouldReturnError_WhenBowlerAlreadyAwarded()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();
        const decimal average = 210m;
        const int totalGames = 20;
        const int tournamentsParticipated = 4;
        const int statEligibleTournamentCount = 4;

        season.AddHighAverageWinner(bowlerId, average, totalGames, tournamentsParticipated, statEligibleTournamentCount).ShouldBe(Result.Success);

        // Act
        var result = season.AddHighAverageWinner(bowlerId, average, totalGames, tournamentsParticipated, statEligibleTournamentCount);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeasonErrors.BowlerAlreadyAwardedHighAverage);
    }

    [Fact(DisplayName = "AddHighAverageWinner should return an error when bowler has insufficient games")]
    public void AddHighAverageWinner_ShouldReturnError_WhenInsufficientGames()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();
        // 6 stat-eligible tournaments → minimum = floor(4.5 × 6) = 27 games
        const int statEligibleTournamentCount = 6;
        const int totalGames = 26; // one short of the 27-game minimum

        // Act
        var result = season.AddHighAverageWinner(bowlerId, average: 210m, totalGames, tournamentsParticipated: 6, statEligibleTournamentCount);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Season.HighAverageInsufficientGames");
        result.FirstError.Metadata!["MinimumGames"].ShouldBe(27);
    }

    [Fact(DisplayName = "AddHighAverageWinner should return an error when HighAverageAward creation fails")]
    public void AddHighAverageWinner_ShouldReturnError_WhenAwardCreationFails()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);
        var invalidBowlerId = BowlerId.Empty;
        // 4 stat-eligible tournaments → minimum = floor(4.5 × 4) = 18 games; totalGames must clear this before reaching Create()
        const int statEligibleTournamentCount = 4;
        const int totalGames = 20;

        // Act
        var result = season.AddHighAverageWinner(invalidBowlerId, average: 210m, totalGames, tournamentsParticipated: 4, statEligibleTournamentCount);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(HighAverageAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "AddHighAverageWinner should add award when inputs are valid")]
    public void AddHighAverageWinner_ShouldAddAward_WhenInputsAreValid()
    {
        // Arrange
        var season = SeasonFactory.Create(complete: true);
        var bowlerId = BowlerId.New();
        const decimal average = 210m;
        // 4 stat-eligible tournaments → minimum = floor(4.5 × 4) = 18 games
        const int statEligibleTournamentCount = 4;
        const int totalGames = 20;
        const int tournamentsParticipated = 4;

        // Act
        var result = season.AddHighAverageWinner(bowlerId, average, totalGames, tournamentsParticipated, statEligibleTournamentCount);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Success);

        var award = season.HighAverageAwards.ShouldHaveSingleItem();
        award.BowlerId.ShouldBe(bowlerId);
        award.Average.ShouldBe(average);
        award.TotalGames.ShouldBe(totalGames);
        award.TournamentsParticipated.ShouldBe(tournamentsParticipated);
    }
}