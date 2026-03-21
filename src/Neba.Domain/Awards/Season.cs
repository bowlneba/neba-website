using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Awards;

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

    /// <summary>
    /// Whether the season has been closed and awards may be assigned.
    /// Once <see langword="true"/>, a season may not be reopened.
    /// </summary>
    public bool Complete { get; init; }

    private readonly List<BowlerOfTheYearAward> _bowlerOfTheYearAwards = [];

    /// <summary>
    /// Bowler of the Year awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<BowlerOfTheYearAward> BowlerOfTheYearAwards
        => _bowlerOfTheYearAwards.AsReadOnly();

    /// <summary>
    /// Assigns a Bowler of the Year award to the specified bowler in the specified category.
    /// </summary>
    /// <param name="bowlerId">The unique identifier of the bowler.</param>
    /// <param name="category">The category of the Bowler of the Year award.</param>
    /// <returns>A result indicating success or failure.</returns>
    public ErrorOr<Success> AddBowlerOfTheYearWinner(BowlerId bowlerId, BowlerOfTheYearCategory category)
    {
        if (!Complete)
        {
            return SeasonErrors.SeasonNotComplete;
        }

        var awardResult = BowlerOfTheYearAward.Create(bowlerId, category);

        if (awardResult.IsError)
        {
            return awardResult.Errors;
        }

        _bowlerOfTheYearAwards.Add(awardResult.Value);

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

        if (_highAverageAwards.Any(award => award.BowlerId == bowlerId))
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

        if (_highBlockAwards.Any(award => award.BowlerId == bowlerId))
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

internal static class SeasonErrors
{
    public static readonly Error SeasonNotComplete = Error.Validation(
        code: "Season.SeasonNotComplete",
        description: "Season must be marked complete before awards can be assigned.");

    public static readonly Error HighBlockScoreMismatch = Error.Validation(
        code: "Season.HighBlockScoreMismatch",
        description: "All High Block awards for a season must have the same block score.");

    public static readonly Error BowlerAlreadyAwardedHighBlock = Error.Validation(
        code: "Season.BowlerAlreadyAwardedHighBlock",
        description: "A bowler cannot receive more than one High Block award in the same season.");

    public static readonly Error HighAverageMismatch = Error.Validation(
        code: "Season.HighAverageMismatch",
        description: "All High Average awards for a season must have the same average.");

    public static readonly Error BowlerAlreadyAwardedHighAverage = Error.Validation(
        code: "Season.BowlerAlreadyAwardedHighAverage",
        description: "A bowler cannot receive more than one High Average award in the same season.");

    public static Error HighAverageInsufficientGames(int minimumGames) => Error.Validation(
        code: "Season.HighAverageInsufficientGames",
        description: $"A bowler must have completed at least {minimumGames} games in Stat-Eligible Tournaments during the season to qualify for a High Average award.",
        metadata: new Dictionary<string, object> { { "MinimumGames", minimumGames } });
}
