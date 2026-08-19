using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// One bowler's outcome in a completed Tournament: finishing place, prize money earned, and
/// points earned. Constructed and mutated only through the owning Tournament, once
/// <see cref="Tournament.Complete"/> is <see langword="true"/>.
/// </summary>
public sealed class TournamentResult
{
    /// <summary>
    /// Gets the unique identifier for this result.
    /// </summary>
    public required TournamentResultId Id { get; init; }

    /// <summary>
    /// Gets the bowler this result belongs to.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigation, needed for the real foreign key configured in TournamentResultConfiguration.
    // Same pattern as HighBlockAward.Bowler / SquadScore.Bowler — never referenced outside EF configuration.
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>
    /// Gets the bowler's finishing place among the full field. Unlike the legacy historical
    /// data, this is never <see langword="null"/> — bowlers who didn't advance past qualifying
    /// are still ranked, by best qualifying score, below the match play finishers, and a DNF
    /// bowler is always included and ranked (typically last), never omitted or treated as a
    /// null/placeholder case. Not guaranteed unique within a tournament — ties, and
    /// doubles/trios partners in team events, share the same <see cref="Place"/> value.
    /// </summary>
    public int Place { get; private set; }

    /// <summary>
    /// Gets the prize money earned, in dollars. Zero if none earned.
    /// </summary>
    public decimal PrizeMoney { get; private set; }

    /// <summary>
    /// Gets the points earned toward season standings. Includes the tournament's base
    /// points-for-entering plus any additional points earned by placement. Never negative.
    /// </summary>
    public int Points { get; private set; }

    internal static ErrorOr<TournamentResult> Create(
        BowlerId bowlerId, int place, decimal prizeMoney, int points)
    {
        var validated = Validate(place, prizeMoney, points);
        return validated.IsError
            ? validated.Errors
            : new TournamentResult
            {
                Id = TournamentResultId.New(),
                BowlerId = bowlerId,
                Place = place,
                PrizeMoney = prizeMoney,
                Points = points
            };
    }

    private static ErrorOr<Success> Validate(int place, decimal prizeMoney, int points)
    {
        if (place <= 0)
        {
            return TournamentResultErrors.InvalidPlace(place);
        }

        if (prizeMoney < 0)
        {
            return TournamentResultErrors.InvalidPrizeMoney(prizeMoney);
        }

        return points < 0
            ? TournamentResultErrors.InvalidPoints(points)
            : Result.Success;
    }
}