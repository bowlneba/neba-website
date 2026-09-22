using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.TruncateTournament;

internal sealed class TruncateTournamentEndpoint(Messaging.ICommandHandler<TruncateTournamentCommand, Success> commandHandler)
    : Endpoint<TruncateTournamentRequest>
{
    private readonly Messaging.ICommandHandler<TruncateTournamentCommand, Success> _commandHandler = commandHandler;

    public override void Configure()
    {
        Patch("{id}/truncate");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentStatus.PolicyName);

        Description(description => description
            .WithName("TruncateTournament")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(TruncateTournamentRequest req, CancellationToken ct)
    {
        var command = new TruncateTournamentCommand { TournamentId = new TournamentId(req.Id) };
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