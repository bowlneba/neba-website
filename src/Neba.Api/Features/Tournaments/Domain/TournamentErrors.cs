using System.Globalization;

using ErrorOr;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class TournamentErrors
{
    private const string DateFormat = "yyyy-MM-dd";

    public static Error TournamentNotFound(TournamentId id)
        => Error.NotFound(
            code: "Tournament.NotFound",
            description: "Tournament was not found.",
            metadata: new Dictionary<string, object>
            {
                { "TournamentId", id.ToString() }
            });

    public static Error InvalidTournamentDatesForSeason(DateOnly seasonStartDate, DateOnly seasonEndDate)
    {
        return Error.Validation(
            code: "Tournament.InvalidDatesForSeason",
            description: "Tournament dates must fall within the season dates.",
            metadata: new Dictionary<string, object>
            {
                { "SeasonStartDate", seasonStartDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
                { "SeasonEndDate", seasonEndDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
            });
    }

    public static Error SponsorAlreadyAdded(SponsorId sponsorId)
    {
        return Error.Conflict(
            code: "Tournament.SponsorAlreadyAdded",
            description: "The specified sponsor has already been added to this tournament.",
            metadata: new Dictionary<string, object>
            {
                { "SponsorId", sponsorId.ToString() }
            });
    }

    public static Error TitleSponsorAlreadyAdded(SponsorId titleSponsorId)
    {
        return Error.Conflict(
            code: "Tournament.TitleSponsorAlreadyAdded",
            description: "A title sponsor has already been added to this tournament.",
            metadata: new Dictionary<string, object>
            {
                { "TitleSponsorId", titleSponsorId.ToString() }
            });
    }

    public static Error NameRequired
        => Error.Validation("Tournament.Name.Required", "Name must not be empty.");

    public static Error EndDateBeforeStartDate(DateOnly startDate, DateOnly endDate)
        => Error.Validation(
            code: "Tournament.EndDateBeforeStartDate",
            description: "End date must not be before start date.",
            metadata: new Dictionary<string, object>
            {
                { "StartDate", startDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
                { "EndDate", endDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
            });

    public static Error InvalidEntryFee(decimal entryFee)
        => Error.Validation(
            code: "Tournament.InvalidEntryFee",
            description: "Entry fee must not be negative.",
            metadata: new Dictionary<string, object>
            {
                { "EntryFee", entryFee }
            });

    public static Error InvalidNebaAddedMoney(decimal nebaAddedMoney)
        => Error.Validation(
            code: "Tournament.InvalidNebaAddedMoney",
            description: "NEBA added money must not be negative.",
            metadata: new Dictionary<string, object>
            {
                { "NebaAddedMoney", nebaAddedMoney }
            });

    public static Error NoSeasonForDates(DateOnly startDate, DateOnly endDate)
        => Error.Validation(
            code: "Tournament.NoSeasonForDates",
            description: "No season is configured that contains these tournament dates.",
            metadata: new Dictionary<string, object>
            {
                { "StartDate", startDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
                { "EndDate", endDate.ToString(DateFormat, CultureInfo.InvariantCulture) },
            });

    public static Error OilPatternNotFound(OilPatternId id)
        => Error.Conflict(
            code: "Tournament.OilPatternNotFound",
            description: "The specified oil pattern was not found.",
            metadata: new Dictionary<string, object> { { "OilPatternId", id.Value } });

    public static Error BowlingCenterNotFound(CertificationNumber id)
        => Error.Conflict(
            code: "Tournament.BowlingCenterNotFound",
            description: "The specified bowling center was not found.",
            metadata: new Dictionary<string, object> { { "CertificationNumber", id.Value } });

    public static Error SponsorNotFound(SponsorId sponsorId)
        => Error.Conflict(
            code: "Tournament.SponsorNotFound",
            description: "The specified sponsor was not found.",
            metadata: new Dictionary<string, object>
            {
                { "SponsorId", sponsorId.ToString() }
            });

    public static Error SponsorNotAttached(SponsorId sponsorId)
        => Error.Conflict(
            code: "Tournament.SponsorNotAttached",
            description: "The specified sponsor is not attached to this tournament.",
            metadata: new Dictionary<string, object>
            {
                { "SponsorId", sponsorId.ToString() }
            });

    public static Error HasHistoricalRecords(TournamentId id)
        => Error.Conflict(
            code: "Tournament.HasHistoricalRecords",
            description: "This tournament has recorded championship, entry, or result history and cannot be deleted.",
            metadata: new Dictionary<string, object>
            {
                { "TournamentId", id.ToString() }
            });
}