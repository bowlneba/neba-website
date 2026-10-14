using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.EditTournamentLogo;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed class EditTournamentLogoEndpoint(Messaging.ICommandHandler<EditTournamentLogoCommand, Updated> commandHandler)
    : Endpoint<EditTournamentLogoRequest>
{
    private readonly Messaging.ICommandHandler<EditTournamentLogoCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}/logo");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditTournamentLogo.PolicyName);

        Description(description => description
            .WithName("EditTournamentLogo")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditTournamentLogoRequest req, CancellationToken ct)
    {
        var command = new EditTournamentLogoCommand
        {
            TournamentId = new TournamentId(req.Id),
            Logo = TournamentInputMapper.ToLogo(req.Input.Logo)
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