using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;

internal sealed class EditTournamentSponsorAmountEndpoint(Messaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success> commandHandler)
    : Endpoint<EditTournamentSponsorAmountRequest>
{
    private readonly Messaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/sponsors/{sponsorId}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentSponsors.PolicyName);

        Description(description => description
            .WithName("EditTournamentSponsorAmount")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditTournamentSponsorAmountRequest req, CancellationToken ct)
    {
        var command = new EditTournamentSponsorAmountCommand
        {
            TournamentId = new TournamentId(req.Id),
            SponsorId = new SponsorId(req.SponsorId),
            SponsorshipAmount = req.Sponsor.SponsorshipAmount
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