using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// One bowler's score for a single game within a Squad. Always constructed and mutated through
/// the owning ScoreCard — never directly.
/// </summary>
public sealed class SquadScore
{
    /// <summary>
    /// Gets the unique identifier for this squad score.
    /// </summary>
    public required SquadScoreId Id { get; init; }

    /// <summary>
    /// Gets the Squad this game was bowled in.
    /// </summary>
    public required SquadId SquadId { get; init; }

    /// <summary>
    /// Gets the bowler who bowled this game.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigations, needed for the real foreign keys configured in SquadScoreConfiguration.
    // Same pattern as HighBlockAward.Bowler — never referenced outside EF configuration.
    internal Squad Squad { get; init; } = null!;
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>
    /// Gets the game number within the Squad (1-based).
    /// </summary>
    public short GameNumber { get; private set; }

    /// <summary>
    /// Gets the number of pins knocked down, 0-300 inclusive.
    /// </summary>
    public int Score { get; private set; }

    internal static ErrorOr<SquadScore> Create(SquadId squadId, BowlerId bowlerId, short gameNumber, int score)
    {
        var validated = ValidateScore(score);
        return validated.IsError
            ? validated.Errors
            : new SquadScore
            {
                Id = SquadScoreId.New(),
                SquadId = squadId,
                BowlerId = bowlerId,
                GameNumber = gameNumber,
                Score = score
            };
    }

    internal ErrorOr<Updated> UpdateValue(int score)
    {
        var validated = ValidateScore(score);
        if (validated.IsError)
        {
            return validated.Errors;
        }

        Score = score;
        return Result.Updated;
    }

    private static ErrorOr<Success> ValidateScore(int score)
        => score is < 0 or > 300
            ? SquadScoreErrors.InvalidValue(score)
            : Result.Success;
}