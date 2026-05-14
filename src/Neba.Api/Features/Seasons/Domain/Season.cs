using ErrorOr;

using Neba.Api.Domain;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Seasons.Domain;


/// <summary>
/// The temporal boundary over which awards are calculated and assigned.
/// Must be marked <see cref="Complete"/> before any awards may be assigned to bowlers.
/// </summary>
public sealed class Season
    : AggregateRoot
{
    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public required SeasonId Id { get; init; }

    /// <summary>
    /// Human-readable label for the season (e.g., "2022 Season", "2020–2021 Season").
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The first date of the season. Inclusive.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The last date of the season. Inclusive.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    private readonly List<BowlerSeasonStats> _bowlerStats = [];
    internal IReadOnlyCollection<BowlerSeasonStats> BowlerStats
        => _bowlerStats.AsReadOnly();

    /// <summary>
    /// Whether the season has been closed and awards may be assigned.
    /// Once <see langword="true"/>, a season may not be reopened.
    /// </summary>
    public bool Complete { get; init; }

    private readonly List<Tournament> _tournaments = [];
    internal IReadOnlyCollection<Tournament> Tournaments
        => _tournaments.AsReadOnly();

    private readonly List<BowlerOfTheYearAward> _bowlerOfTheYearAwards = [];

    /// <summary>
    /// Bowler of the Year awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<BowlerOfTheYearAward> BowlerOfTheYearAwards
        => _bowlerOfTheYearAwards.AsReadOnly();

    /// <summary>
    /// Assigns an Open Bowler of the Year award to the specified bowler.
    /// </summary>
    public ErrorOr<Success> AddOpenBowlerOfTheYearWinner(BowlerId bowlerId)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateOpen(bowlerId);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    /// <summary>
    /// Assigns a Woman Bowler of the Year award to the specified bowler.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="gender">The bowler's gender; must be <see cref="Gender.Female"/>.</param>
    public ErrorOr<Success> AddWomanOfTheYearWinner(BowlerId bowlerId, Gender gender)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateWoman(bowlerId, gender);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    /// <summary>
    /// Assigns a Senior Bowler of the Year award to the specified bowler.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="age">The bowler's age; must be at least 50.</param>
    public ErrorOr<Success> AddSeniorBowlerOfTheYearWinner(BowlerId bowlerId, int age)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateSenior(bowlerId, age);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    /// <summary>
    /// Assigns a Super Senior Bowler of the Year award to the specified bowler.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="age">The bowler's age; must be at least 60.</param>
    public ErrorOr<Success> AddSuperSeniorBowlerOfTheYearWinner(BowlerId bowlerId, int age)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateSuperSenior(bowlerId, age);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    /// <summary>
    /// Assigns a Rookie Bowler of the Year award to the specified bowler.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="isRookie">Whether the bowler holds a New Member membership in the current season.</param>
    public ErrorOr<Success> AddRookieBowlerOfTheYearWinner(BowlerId bowlerId, bool isRookie)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateRookie(bowlerId, isRookie);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    /// <summary>
    /// Assigns a Youth Bowler of the Year award to the specified bowler.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="age">The bowler's age; must be under 18.</param>
    public ErrorOr<Success> AddYouthBowlerOfTheYearWinner(BowlerId bowlerId, int age)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var award = BowlerOfTheYearAward.CreateYouth(bowlerId, age);

        if (award.IsError)
        {
            return award.Errors;
        }

        _bowlerOfTheYearAwards.Add(award.Value);

        return Result.Success;
    }

    private readonly List<HighAverageAward> _highAverageAwards = [];

    /// <summary>
    /// High Average awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<HighAverageAward> HighAverageAwards
        => _highAverageAwards.AsReadOnly();

    private static int ComputeMinimumNumberOfGamesForHighAverage(int statElgibleTournamentCount)
        => (int)Math.Floor(4.5m * statElgibleTournamentCount);

    /// <summary>
    /// Assigns a High Average award to the specified bowler with the specified average, total games, and tournaments participated.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="average">The average score of the bowler.</param>
    /// <param name="totalGames">The total number of games played by the bowler.</param>
    /// <param name="tournamentsParticipated">The number of tournaments the bowler participated in.</param>
    /// <param name="statEligibleTournamentCount">The number of stat-eligible tournaments in the season.</param>
    /// <returns>A result indicating success or failure.</returns>
    public ErrorOr<Success> AddHighAverageWinner(
        BowlerId bowlerId,
        decimal average,
        int totalGames,
        int tournamentsParticipated,
        int statEligibleTournamentCount)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        if (_highAverageAwards.Count > 0 && _highAverageAwards[0].Average != average)
        {
            return SeasonErrors.HighAverageMismatch;
        }

        if (_highAverageAwards.Any(award => award.BowlerId.Equals(bowlerId)))
        {
            return SeasonErrors.BowlerAlreadyAwardedHighAverage;
        }

        var minimumNumberOfGames = ComputeMinimumNumberOfGamesForHighAverage(statEligibleTournamentCount);
        if (totalGames < minimumNumberOfGames)
        {
            return SeasonErrors.HighAverageInsufficientGames(minimumNumberOfGames);
        }

        var awardResult = HighAverageAward.Create(bowlerId, average, totalGames, tournamentsParticipated);

        if (awardResult.IsError)
        {
            return awardResult.Errors;
        }

        _highAverageAwards.Add(awardResult.Value);

        return Result.Success;
    }

    private readonly List<HighBlockAward> _highBlockAwards = [];

    /// <summary>
    /// High Block awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<HighBlockAward> HighBlockAwards
        => _highBlockAwards.AsReadOnly();

    /// <summary>
    /// Assigns a High Block award to the specified bowler with the specified block score.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="score">The block score achieved by the bowler.</param>
    /// <param name="games">The number of games in the block.</param>
    /// <returns>A result indicating success or failure.</returns>
    public ErrorOr<Success> AddHighBlockWinner(BowlerId bowlerId, int score, int games)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        if (_highBlockAwards.Count > 0 && _highBlockAwards[0].BlockScore != score)
        {
            return SeasonErrors.HighBlockScoreMismatch;
        }

        if (_highBlockAwards.Any(award => award.BowlerId.Equals(bowlerId)))
        {
            return SeasonErrors.BowlerAlreadyAwardedHighBlock;
        }

        var awardResult = HighBlockAward.Create(bowlerId, score, games);

        if (awardResult.IsError)
        {
            return awardResult.Errors;
        }

        _highBlockAwards.Add(awardResult.Value);

        return Result.Success;
    }
}
