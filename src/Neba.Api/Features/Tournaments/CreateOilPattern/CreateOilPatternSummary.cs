using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternSummary : Summary<CreateOilPatternEndpoint>
{
    public CreateOilPatternSummary()
    {
        Summary = "Creates an oil pattern.";
        Description = "Creates a reusable oil pattern catalog entry with its length, volume, and left/right ratios. Length and ratio categories are derived automatically. Requires the Tournaments.CreateTournament permission.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "Oil pattern created.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreatedOilPatternResponse
            {
                OilPatternId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                Name = "Kegel Main Street",
                Length = 40,
                LengthCategory = "Medium",
                RatioCategory = "Sport"
            });
#pragma warning restore S1075 // URIs should not be hardcoded

        Response(400, "Name, length, volume, or a ratio field failed structural validation (e.g. missing, too long, or negative).");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.CreateTournament permission.");
        Response(409, "A pattern with the given Kegel ID already exists.");
        Response(422, "Name, length, or volume failed a domain validation rule.");
    }
}