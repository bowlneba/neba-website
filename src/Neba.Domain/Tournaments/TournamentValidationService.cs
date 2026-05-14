using ErrorOr;

using Neba.Domain.Seasons;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Provides validation logic for ensuring that a tournament's properties are consistent with the rules and constraints
/// </summary>
public sealed class TournamentValidationService(ISeasonRepository seasonRepository)
    : ITournamentValidationService
{
    private readonly ISeasonRepository _seasonRepository = seasonRepository;

    ///<inheritdoc />
    public async Task<ErrorOr<Success>> IsTournamentValidForSeasonAsync(Tournament tournament, SeasonId seasonId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tournament);

        var season = await _seasonRepository.GetSeasonByIdAsync(seasonId, trackChanges: false, cancellationToken);

        if (season is null)
        {
            return SeasonErrors.SeasonNotFound(seasonId);
        }

        return tournament.StartDate < season.StartDate
            || tournament.EndDate > season.EndDate
            ? TournamentErrors.InvalidTournamentDatesForSeason(season.StartDate, season.EndDate)
            : Result.Success;
    }
}

/// <summary>
/// Defines the contract for validating that a tournament's properties are consistent with the rules and constraints
/// of the season it is associated with.
/// </summary>
public interface ITournamentValidationService
{
    /// <summary>
    /// Validates that the tournament's dates fall within the specified season's start and end dates.
    /// </summary>
    /// <param name="tournament">The tournament to validate.</param>
    /// <param name="seasonId">The unique identifier of the season.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the tournament is valid for the season.</returns>
    Task<ErrorOr<Success>> IsTournamentValidForSeasonAsync(Tournament tournament, SeasonId seasonId, CancellationToken cancellationToken);
}