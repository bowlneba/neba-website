using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Features.Tournaments.UploadTournamentLogo;

internal sealed class UploadTournamentLogoSummary : Summary<UploadTournamentLogoEndpoint>
{
    public UploadTournamentLogoSummary()
    {
        Summary = "Uploads a tournament logo.";
        Description = "Stages an image file in blob storage ahead of tournament creation and returns a pointer to it. The pointer is orphaned (and later swept) unless it's included as Logo in a subsequent CreateTournament command. Requires the Tournaments.CreateTournament permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.CreateTournament permission.");
    }
}
