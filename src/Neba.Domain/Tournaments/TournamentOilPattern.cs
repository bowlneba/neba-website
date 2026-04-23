using ErrorOr;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Represents the association between a tournament and the oil pattern used for a specific round or rounds of the tournament. This entity captures the relationship between a tournament and its oil pattern(s), allowing us to track which oil patterns were used for which rounds in a tournament. The TournamentOilPattern entity includes references to the Tournament, the OilPattern, and the specific TournamentRounds that the oil pattern was applied to, enabling us to maintain a detailed record of the oil conditions for each round of a tournament. This information can be valuable for historical records, analytics, and providing context for tournament results based on the oil patterns used.
/// </summary>
public sealed class TournamentOilPattern
{
    /// <summary>
    /// The tournament associated with this oil pattern. This is a reference to the Tournament entity, allowing us to access tournament details such as name, dates, and location when needed. It is marked as internal to restrict access to the domain layer, ensuring that external layers interact with tournaments through their respective repositories and services rather than directly through this association entity.
    /// </summary>
    internal Tournament Tournament { get; init; } = null!;

    /// <summary>
    /// The unique identifier of the oil pattern associated with this tournament oil pattern. This is a foreign key that links to the OilPattern entity, allowing us to retrieve oil pattern details such as name, length, and volume when needed. It is required to establish the relationship between the tournament and its oil pattern in the database.
    /// </summary>
    public required OilPatternId OilPatternId { get; init; }

    /// <summary>
    /// The oil pattern associated with this tournament oil pattern. This navigation property allows us to access the oil pattern details from the tournament oil pattern entity. It is marked as internal to restrict access to the domain layer, ensuring that external layers interact with tournaments and oil patterns through their respective repositories and services rather than directly through this association entity.
    /// </summary>
    internal OilPattern OilPattern { get; init; } = null!;

    private readonly List<TournamentRound> _tournamentRounds = [];

    /// <summary>
    /// The specific tournament rounds that this oil pattern was applied to. This collection allows us to track which rounds of the tournament used this oil pattern, providing valuable context for tournament results and historical records. It is required to capture the relationship between the oil pattern and the tournament rounds in the database, as different rounds may use different oil patterns based on tournament format and rules.
    /// </summary>
    public IReadOnlyCollection<TournamentRound> TournamentRounds
        => _tournamentRounds;

    /// <summary>
    /// Associates a tournament round with this oil pattern. This method allows us to specify which rounds of the tournament used this oil pattern, ensuring that we maintain an accurate record of the oil conditions for each round. If the specified tournament round is already associated with this oil pattern, an appropriate error is returned to prevent duplicate associations. Otherwise, the tournament round is added successfully to the collection of associated rounds.
    /// </summary>
    /// <param name="tournamentRound">The tournament round to associate with this oil pattern.</param>
    /// <returns>An <see cref="ErrorOr{Updated}"/> indicating the result of the operation.</returns>
    public ErrorOr<Updated> AddTournamentRound(TournamentRound tournamentRound)
    {
        ArgumentNullException.ThrowIfNull(tournamentRound);

        if (TournamentRounds.Contains(tournamentRound))
        {
            return TournamentOilPatternErrors.TournamentRoundAlreadyAssociatedWithOilPattern(tournamentRound.Name);
        }

        _tournamentRounds.Add(tournamentRound);

        return Result.Updated;
    }
}

internal static class TournamentOilPatternErrors
{
    public static Error TournamentRoundAlreadyAssociatedWithOilPattern(string tournamentRoundName)
        => Error.Validation(
            code: "Tournaments.TournamentOilPattern.TournamentRoundAlreadyAssociated",
            description: "Tournament round is already associated with this oil pattern.",
            metadata: new Dictionary<string, object>
            {
                { "TournamentRoundName", tournamentRoundName }
            });
}