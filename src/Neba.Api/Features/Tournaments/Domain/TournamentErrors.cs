using System.Globalization;

using ErrorOr;

using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class TournamentErrors
{
    public static Error InvalidTournamentDatesForSeason(DateOnly seasonStartDate, DateOnly seasonEndDate)
    {
        return Error.Validation(
            code: "Tournament.InvalidDatesForSeason",
            description: "Tournament dates must fall within the season dates.",
            metadata: new Dictionary<string, object>
            {
                { "SeasonStartDate", seasonStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                { "SeasonEndDate", seasonEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
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
}