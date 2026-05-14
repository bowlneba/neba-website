using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Seasons.Domain;

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
        if (bowlerId.Equals(default))
        {
            return HighBlockAwardErrors.BowlerIdRequired;
        }

        if (blockScore <= 0)
        {
            return HighBlockAwardErrors.InvalidBlockScore;
        }

        if (blockScore > games * 300)
        {
            return HighBlockAwardErrors.BlockScoreExceedsMaximum(games);
        }

        var id = SeasonAwardId.New();
        return new HighBlockAward { Id = id, BowlerId = bowlerId, BlockScore = blockScore };
    }
}
