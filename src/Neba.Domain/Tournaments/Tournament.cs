using System.Globalization;

using ErrorOr;

using Neba.Domain.BowlingCenters;
using Neba.Domain.Seasons;
using Neba.Domain.Sponsors;
using Neba.Domain.Storage;

namespace Neba.Domain.Tournaments;

/// <summary>
/// A NEBA-sanctioned scratch bowling competition consisting of one or more qualifying squads
/// followed by a single-elimination match play championship round to determine a winner.
/// Tournament format, eligibility, and team composition are governed by the associated
/// <see cref="TournamentType"/>.
/// </summary>
public sealed class Tournament
    : AggregateRoot
{
    /// <summary>
    /// Gets the unique identifier for this tournament.
    /// </summary>
    public required TournamentId Id { get; init; }

    /// <summary>
    /// Gets the publicly displayed name of the tournament as it appears in schedules and results.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the format classification of the tournament, which governs team size, eligibility
    /// restrictions, and match play structure.
    /// See <see cref="TournamentType"/> for valid values.
    /// </summary>
    public required TournamentType TournamentType { get; init; }

    /// <summary>
    /// Gets the date on which the first qualifying squad of the tournament is held.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the date on which the final round of competition concludes.
    /// For single-day tournaments this will equal <see cref="StartDate"/>.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Gets the USBC certification number of the bowling center where the tournament is held,
    /// or <see langword="null"/> if the venue has not yet been assigned.
    /// </summary>
    public CertificationNumber? BowlingCenterId { get; init; }

    internal BowlingCenter? BowlingCenter { get; init; }

    /// <summary>
    /// Gets the oil-to-dry ratio category of the lane condition used in this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternRatioCategory"/> for valid values.
    /// </summary>
    public PatternRatioCategory? PatternRatioCategory { get; init; }

    /// <summary>
    /// Gets the length category of the oil pattern applied to the lanes for this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternLengthCategory"/> for valid values.
    /// </summary>
    public PatternLengthCategory? PatternLengthCategory { get; init; }

    /// <summary>
    /// Gets the legacy numeric identifier for this tournament, carried over from the previous
    /// system. <see langword="null"/> for tournaments created after the system migration.
    /// </summary>
    public int? LegacyId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the season in which this tournament takes place.
    /// </summary>
    public required SeasonId SeasonId { get; init; }

    internal Season Season { get; init; } = null!;

    /// <summary>
    /// Gets the entry fee amount for this tournament, which is the cost for a team to participate.
    /// </summary>
    public decimal EntryFee { get; init; }

    /// <summary>
    /// Gets the URL where teams can register for the tournament, or <see langword="null"/> if registration
    /// </summary>
    public Uri? ExternalRegistrationUrl { get; init; }

    /// <summary>
    /// Gets an optional logo image for the tournament, which can be used in promotional materials and on the website. This is represented as a <see cref="StoredFile"/> value object, which contains information about the file's storage location, content type, and other relevant metadata. This allows for flexible handling of associated media while keeping the core tournament data focused on the essential attributes of the event.
    /// </summary>
    public StoredFile? Logo { get; init; }

    private readonly List<TournamentSponsor> _sponsors = [];

    /// <summary>
    /// Gets the collection of sponsors associated with this tournament, along with details about
    /// </summary>
    public IReadOnlyCollection<TournamentSponsor> Sponsors
        => _sponsors;

    /// <summary>
    /// Adds a sponsor to the tournament with the specified sponsorship amount and title sponsor status. If the sponsor has already been added to the tournament, or if a title sponsor has already been designated and the new sponsor is also marked as a title sponsor, an appropriate error is returned. Otherwise, the sponsor is added successfully.
    /// </summary>
    /// <param name="sponsorId">The unique identifier of the sponsor.</param>
    /// <param name="titleSponsor">Indicates whether the sponsor is a title sponsor.</param>
    /// <param name="sponsorshipAmount">The amount of the sponsorship.</param>
    /// <returns>An <see cref="ErrorOr{Success}"/> indicating the result of the operation.</returns>
    public ErrorOr<Success> AddSponsor(SponsorId sponsorId, bool titleSponsor, decimal sponsorshipAmount)
    {
        if (_sponsors.Any(tournamentSponsor => tournamentSponsor.SponsorId == sponsorId))
        {
            return TournamentErrors.SponsorAlreadyAdded(sponsorId);
        }

        if (titleSponsor && _sponsors.Any(tournamentSponsor => tournamentSponsor.TitleSponsor))
        {
            return TournamentErrors.TitleSponsorAlreadyAdded(_sponsors.Single(tournamentSponsor => tournamentSponsor.TitleSponsor).SponsorId);
        }

        var sponsor = TournamentSponsor.Create(sponsorId, titleSponsor, sponsorshipAmount);
        if (sponsor.IsError) return sponsor.Errors;

        _sponsors.Add(sponsor.Value);

        return Result.Success;
    }

    private readonly List<TournamentOilPattern> _oilPatterns = [];

    /// <summary>
    /// Gets the collection of oil patterns associated with this tournament, along with the specific rounds to which each pattern was applied. This allows us to track the oil conditions for each round of the tournament, providing valuable context for tournament results and historical records. Each <see cref="TournamentOilPattern"/> in the collection includes a reference to the associated <see cref="OilPattern"/>, as well as the specific <see cref="TournamentRound"/> values that indicate which rounds of the tournament used that oil pattern.
    /// </summary>
    public IReadOnlyCollection<TournamentOilPattern> OilPatterns
        => _oilPatterns;

    /// <summary>
    /// Associates an oil pattern with this tournament for the specified tournament rounds. If the oil pattern is already associated with the tournament, the specified rounds will be added to the existing association. If any of the specified rounds are already associated with the oil pattern, an appropriate error is returned to prevent duplicate associations. Otherwise, the oil pattern and round associations are added successfully.
    /// </summary>
    /// <param name="oilPatternId">The unique identifier of the oil pattern to associate with the tournament.</param>
    /// <param name="tournamentRounds">The collection of tournament rounds to associate with the oil pattern.</param>
    /// <returns>An <see cref="ErrorOr{Success}"/> indicating the result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tournamentRounds is null.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicate tournament rounds are provided for an existing oil pattern association.</exception>

    public ErrorOr<Success> AddOilPattern(OilPatternId oilPatternId, params TournamentRound[] tournamentRounds)
    {
        ArgumentNullException.ThrowIfNull(tournamentRounds);

        var existingOilPattern = _oilPatterns.SingleOrDefault(top => top.OilPatternId == oilPatternId);
        if (existingOilPattern is not null)
        {
            foreach (var round in tournamentRounds)
            {
                var result = existingOilPattern.AddTournamentRound(round);

                if (result.IsError)
                {
                    return result.Errors;
                }
            }

            return Result.Success;
        }

        var newOilPatternResult = TournamentOilPattern.Create(oilPatternId, tournamentRounds);

        if (newOilPatternResult.IsError)
        {
            return newOilPatternResult.Errors;
        }

        _oilPatterns.Add(newOilPatternResult.Value);

        return Result.Success;
    }
}

internal static class TournamentErrors
{
    public static Error InvalidTournamentDatesForSeason(DateOnly seasonStartDate, DateOnly seasonEndDate)
    {
        return Error.Validation(
            code: "Tournaments.InvalidDatesForSeason",
            description: "Tournament dates must fall within the season dates.",
            metadata: new Dictionary<string, object>
            {
                { "SeasonStartDate", seasonStartDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) },
                { "SeasonEndDate", seasonEndDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) },
            });
    }

    public static Error SponsorAlreadyAdded(SponsorId sponsorId)
    {
        return Error.Validation(
            code: "Tournaments.SponsorAlreadyAdded",
            description: "The specified sponsor has already been added to this tournament.",
            metadata: new Dictionary<string, object>
            {
                { "SponsorId", sponsorId.ToString() }
            });
    }

    public static Error TitleSponsorAlreadyAdded(SponsorId titleSponsorId)
    {
        return Error.Validation(
            code: "Tournaments.TitleSponsorAlreadyAdded",
            description: "A title sponsor has already been added to this tournament.",
            metadata: new Dictionary<string, object>
            {
                { "TitleSponsorId", titleSponsorId.ToString() }
            });
    }
}