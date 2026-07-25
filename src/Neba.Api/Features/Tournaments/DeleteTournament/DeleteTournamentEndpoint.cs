using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentEndpoint(Messaging.ICommandHandler<DeleteTournamentCommand, Deleted> commandHandler)
    : Endpoint<DeleteTournamentRequest>
{
    private readonly Messaging.ICommandHandler<DeleteTournamentCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.DeleteTournament.PolicyName);

        Description(description => description
            .WithName("DeleteTournament")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(DeleteTournamentRequest req, CancellationToken ct)
    {
        var command = new DeleteTournamentCommand { TournamentId = new TournamentId(req.Id) };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
