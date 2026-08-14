using ErrorOr;

using Neba.Api.Domain;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Seasons.Domain;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

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
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the format classification of the tournament, which governs team size, eligibility
    /// restrictions, and match play structure.
    /// See <see cref="TournamentType"/> for valid values.
    /// </summary>
    public TournamentType TournamentType { get; private set; } = TournamentType.Singles;

    /// <summary>
    /// Gets the date on which the first qualifying squad of the tournament is held.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Gets the date on which the final round of competition concludes.
    /// For single-day tournaments this will equal <see cref="StartDate"/>.
    /// </summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Gets the USBC certification number of the bowling center where the tournament is held,
    /// or <see langword="null"/> if the venue has not yet been assigned.
    /// </summary>
    public CertificationNumber? BowlingCenterId { get; private set; }

    internal BowlingCenter? BowlingCenter { get; init; }

    /// <summary>
    /// Whether this tournament counts toward season statistics and awards calculations.
    /// </summary>
    public bool StatsEligible { get; private set; }

    /// <summary>
    /// Gets the oil-to-dry ratio category of the lane condition used in this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternRatioCategory"/> for valid values.
    /// </summary>
    public PatternRatioCategory? PatternRatioCategory { get; private set; }

    /// <summary>
    /// Gets the length category of the oil pattern applied to the lanes for this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternLengthCategory"/> for valid values.
    /// </summary>
    public PatternLengthCategory? PatternLengthCategory { get; private set; }

    /// <summary>
    /// Gets the legacy numeric identifier for this tournament, carried over from the previous
    /// system. <see langword="null"/> for tournaments created after the system migration.
    /// </summary>
    public int? LegacyId { get; internal set; }

    /// <summary>
    /// Gets the unique identifier of the season in which this tournament takes place.
    /// </summary>
    public SeasonId SeasonId { get; private set; }

    internal Season Season { get; init; } = null!;

    /// <summary>
    /// Gets the entry fee amount for this tournament, which is the cost for a team to participate.
    /// </summary>
    public decimal EntryFee { get; private set; }

    /// <summary>
    /// Gets the amount NEBA itself has contributed to the tournament's prize fund, independent of
    /// any sponsor contributions.
    /// </summary>
    public decimal NebaAddedMoney { get; private set; }

    /// <summary>
    /// Gets the URL where teams can register for the tournament, or <see langword="null"/> if registration
    /// </summary>
    public Uri? ExternalRegistrationUrl { get; private set; }

    /// <summary>
    /// Optional logo image for promotional display; null if not uploaded.
    /// </summary>
    public StoredFile? Logo { get; private set; }

    private readonly List<TournamentSponsor> _sponsors = [];

    /// <summary>
    /// The sponsors associated with this tournament, including title sponsorship designation and
    /// sponsorship amount for each.
    /// </summary>
    public IReadOnlyCollection<TournamentSponsor> Sponsors
        => _sponsors;

    /// <summary>
    /// Gets the date/time at which full oil pattern details become visible to callers who lack
    /// the tournament management permission, or <see langword="null"/> if there is no reveal
    /// restriction (full details are always visible). Callers holding the tournament management
    /// permission always see full details regardless of this value.
    /// </summary>
    public DateTimeOffset? OilPatternRevealDateTime { get; private set; }

    private readonly List<Squad> _squads = [];
    
    /// <summary>
    /// The squads scheduled for this tournament.
    /// </summary>
    public IReadOnlyCollection<Squad> Squads
        => _squads.AsReadOnly();

    /// <summary>
    /// Schedules a new squad; returns an error if the date/time falls outside the tournament's
    /// date range or collides with an existing squad's date/time.
    /// </summary>
    public ErrorOr<Success> AddSquad(DateTimeOffset bowlingDateTime, int? maxEntries = null, int? legacyId = null)
    {
        var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
        if (rangeCheck.IsError)
        {
            return rangeCheck.Errors;
        }

        if (_squads.Any(squad => squad.BowlingDateTime == bowlingDateTime))
        {
            return TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime);
        }

        var squad = Squad.Create(bowlingDateTime, maxEntries, legacyId);
        if (squad.IsError)
        {
            return squad.Errors;
        }
        
        _squads.Add(squad.Value);
        
        return Result.Success;
    }

    /// <summary>
    /// Reschedules or edits a squad; returns an error if the squad doesn't exist, the new
    /// date/time falls outside the tournament's date range, or it collides with another squad.
    /// </summary>
    public ErrorOr<Updated> UpdateSquad(SquadId squadId, DateTimeOffset bowlingDateTime, int? maxEntries)
    {
        var squad = _squads.SingleOrDefault(s => s.Id == squadId);
        if (squad is null)
        {
            return TournamentErrors.SquadNotFound(squadId);
        }

        var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
        if (rangeCheck.IsError)
        {
            return rangeCheck.Errors;
        }

        return _squads.Any(s => s.Id != squadId && s.BowlingDateTime == bowlingDateTime)
            ? TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime)
            : squad.UpdateDetails(bowlingDateTime, maxEntries);
    }

    /// <summary>
    /// Removes a squad; returns an error if it doesn't exist.
    /// </summary>
    public ErrorOr<Deleted> RemoveSquad(SquadId squadId)
    {
        var squad = _squads.SingleOrDefault(s => s.Id == squadId);
        if (squad is null)
        {
            return TournamentErrors.SquadNotFound(squadId);
        }

        _squads.Remove(squad);

        return Result.Deleted;
    }

    private ErrorOr<Success> ValidateSquadDateInRange(DateTimeOffset bowlingDateTime)
    {
        var bowlingDate = DateOnly.FromDateTime(bowlingDateTime.DateTime);

        return bowlingDate < StartDate || bowlingDate > EndDate
            ? TournamentErrors.SquadDateOutOfRange(bowlingDateTime, StartDate, EndDate)
            : Result.Success;
    }

    /// <summary>
    /// Creates a new tournament, validating name, dates, and entry fee.
    /// </summary>
    public static ErrorOr<Tournament> Create(
        string name,
        TournamentType tournamentType,
        DateOnly startDate,
        DateOnly endDate,
        SeasonId seasonId,
        bool statsEligible,
        decimal entryFee,
        CertificationNumber? bowlingCenterId = null,
        Uri? externalRegistrationUrl = null,
        StoredFile? logo = null,
        PatternLengthCategory? patternLengthCategory = null,
        PatternRatioCategory? patternRatioCategory = null,
        DateTimeOffset? oilPatternRevealDateTime = null,
        decimal nebaAddedMoney = 0,
        TournamentId? id = null,
        int? legacyId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TournamentErrors.NameRequired;
        }

        if (startDate > endDate)
        {
            return TournamentErrors.EndDateBeforeStartDate(startDate, endDate);
        }

        if (entryFee < 0)
        {
            return TournamentErrors.InvalidEntryFee(entryFee);
        }

        if (nebaAddedMoney < 0)
        {
            return TournamentErrors.InvalidNebaAddedMoney(nebaAddedMoney);
        }

        var tournament = new Tournament
        {
            Id = id ?? TournamentId.New(),
            Name = name,
            TournamentType = tournamentType,
            StartDate = startDate,
            EndDate = endDate,
            SeasonId = seasonId,
            StatsEligible = statsEligible,
            EntryFee = entryFee,
            NebaAddedMoney = nebaAddedMoney,
            BowlingCenterId = bowlingCenterId,
            ExternalRegistrationUrl = externalRegistrationUrl,
            Logo = logo,
            PatternLengthCategory = patternLengthCategory,
            PatternRatioCategory = patternRatioCategory,
            OilPatternRevealDateTime = oilPatternRevealDateTime,
            LegacyId = legacyId
        };

        return tournament;
    }

    /// <summary>
    /// Replaces this tournament's editable fields, re-validating the same invariants <see cref="Create"/> enforces.
    /// </summary>
    public ErrorOr<Updated> Update(
        string name,
        TournamentType tournamentType,
        DateOnly startDate,
        DateOnly endDate,
        SeasonId seasonId,
        bool statsEligible,
        decimal entryFee,
        decimal nebaAddedMoney,
        CertificationNumber? bowlingCenterId,
        Uri? externalRegistrationUrl,
        StoredFile? logo,
        PatternLengthCategory? patternLengthCategory,
        PatternRatioCategory? patternRatioCategory,
        DateTimeOffset? oilPatternRevealDateTime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TournamentErrors.NameRequired;
        }

        if (startDate > endDate)
        {
            return TournamentErrors.EndDateBeforeStartDate(startDate, endDate);
        }

        if (entryFee < 0)
        {
            return TournamentErrors.InvalidEntryFee(entryFee);
        }

        if (nebaAddedMoney < 0)
        {
            return TournamentErrors.InvalidNebaAddedMoney(nebaAddedMoney);
        }

        Name = name;
        TournamentType = tournamentType;
        StartDate = startDate;
        EndDate = endDate;
        SeasonId = seasonId;
        StatsEligible = statsEligible;
        EntryFee = entryFee;
        NebaAddedMoney = nebaAddedMoney;
        BowlingCenterId = bowlingCenterId;
        ExternalRegistrationUrl = externalRegistrationUrl;
        Logo = logo;
        PatternLengthCategory = patternLengthCategory;
        PatternRatioCategory = patternRatioCategory;
        OilPatternRevealDateTime = oilPatternRevealDateTime;

        return Result.Updated;
    }

    /// <summary>
    /// Adds a sponsor; returns an error if already added or a title sponsor conflict exists.
    /// </summary>
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
        if (sponsor.IsError)
        {
            return sponsor.Errors;
        }

        _sponsors.Add(sponsor.Value);

        return Result.Success;
    }

    /// <summary>
    /// Removes a sponsor; returns an error if the sponsor isn't currently attached.
    /// </summary>
    public ErrorOr<Deleted> RemoveSponsor(SponsorId sponsorId)
    {
        var sponsor = _sponsors.SingleOrDefault(tournamentSponsor => tournamentSponsor.SponsorId == sponsorId);

        if (sponsor is null)
        {
            return TournamentErrors.SponsorNotAttached(sponsorId);
        }

        _sponsors.Remove(sponsor);

        return Result.Deleted;
    }

    private readonly List<Article> _articles = [];

    internal IReadOnlyCollection<Article> Articles
        => _articles.AsReadOnly();

    private readonly List<TournamentOilPattern> _oilPatterns = [];

    /// <summary>
    /// Oil patterns used in this tournament and the rounds each was applied to.
    /// </summary>
    public IReadOnlyCollection<TournamentOilPattern> OilPatterns
        => _oilPatterns;

    /// <summary>
    /// Associates an oil pattern for the given rounds; appends rounds if pattern already exists.
    /// </summary>
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