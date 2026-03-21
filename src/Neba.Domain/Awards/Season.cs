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
            return SeasonErrors.BowlerAlreadyAwarded;
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

    public static readonly Error BowlerAlreadyAwarded = Error.Validation(
        code: "Season.BowlerAlreadyAwarded",
        description: "A bowler cannot receive more than one High Block award in the same season.");
}
