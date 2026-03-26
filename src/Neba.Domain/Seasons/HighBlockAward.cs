using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Seasons;

/// <summary>
/// Recognizes the single highest 5-game pinfall total from a qualifying block
/// (before match play) in any Stat-Eligible Tournament during the Season.
/// </summary>
public sealed class HighBlockAward
{
    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public required SeasonAwardId Id { get; init; }

    /// <summary>
    /// The bowler receiving the award.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The winning 5-game pinfall total.
    /// </summary>
    public required int BlockScore { get; init; }

    /// <summary>
    /// Navigation to the bowler. Internal — for EF Core query projections only.
    /// </summary>
    internal Bowler Bowler { get; init; } = null!;

    internal static ErrorOr<HighBlockAward> Create(BowlerId bowlerId, int blockScore, int games)
    {
        if (bowlerId == BowlerId.Empty)
        {
            return HighBlockAwardErrors.BowlerIdRequired;
        }

        if (blockScore <= 0)
        {
            return HighBlockAwardErrors.InvalidBlockScore;
        }

        return blockScore > games * 300
            ? HighBlockAwardErrors.BlockScoreExceedsMaximum(games)
            : new HighBlockAward
            {
                Id = SeasonAwardId.New(),
                BowlerId = bowlerId,
                BlockScore = blockScore
            };
    }
}

internal static class HighBlockAwardErrors
{
    public static readonly Error BowlerIdRequired = Error.Validation(
        code: "HighBlockAward.BowlerIdRequired",
        description: "Bowler ID is required.");

    public static readonly Error InvalidBlockScore = Error.Validation(
        code: "HighBlockAward.InvalidBlockScore",
        description: "Block score must be a positive integer.");

    public static Error BlockScoreExceedsMaximum(int games) => Error.Validation(
        code: "HighBlockAward.BlockScoreExceedsMaximum",
        description: $"Block score cannot exceed the maximum possible score of {games * 300} for {games} games.",
        metadata: new Dictionary<string, object> { { "MaximumBlockScore", games * 300 } });
}