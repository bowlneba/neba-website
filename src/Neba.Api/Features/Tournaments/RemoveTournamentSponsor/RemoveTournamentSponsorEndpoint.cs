using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorEndpoint(Messaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted> commandHandler)
    : Endpoint<RemoveTournamentSponsorRequest>
{
    private readonly Messaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}/sponsors/{sponsorId}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentSponsors.PolicyName);

        Description(description => description
            .WithName("RemoveTournamentSponsor")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(RemoveTournamentSponsorRequest req, CancellationToken ct)
    {
        var command = new RemoveTournamentSponsorCommand
        {
            TournamentId = new TournamentId(req.TournamentId),
            SponsorId = new SponsorId(req.SponsorId)
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}