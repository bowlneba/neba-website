using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentEndpoint(Messaging.ICommandHandler<EditTournamentCommand, Updated> commandHandler)
    : Endpoint<EditTournamentRequest>
{
    private readonly Messaging.ICommandHandler<EditTournamentCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditTournament.PolicyName);

        Description(description => description
            .WithName("EditTournament")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditTournamentRequest req, CancellationToken ct)
    {
        var input = req.Tournament;

        var command = new EditTournamentCommand
        {
            TournamentId = new TournamentId(req.Id),
            Name = input.Name,
            TournamentType = TournamentType.FromName(input.TournamentType),
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StatsEligible = input.StatsEligible,
            EntryFee = input.EntryFee,
            NebaAddedMoney = input.NebaAddedMoney,
            BowlingCenterId = string.IsNullOrWhiteSpace(input.BowlingCenterCertificationNumber)
                ? null
                : new CertificationNumber { Value = input.BowlingCenterCertificationNumber },
            ExternalRegistrationUrl = input.ExternalRegistrationUrl,
            Logo = input.Logo is null
                ? null
                : new StoredFile
                {
                    Container = input.Logo.Container,
                    Path = input.Logo.Path,
                    ContentType = input.Logo.ContentType,
                    SizeInBytes = input.Logo.SizeInBytes
                },
            OilPatternId = string.IsNullOrWhiteSpace(input.OilPatternId)
                ? null
                : new OilPatternId(input.OilPatternId),
            PatternLengthCategory = string.IsNullOrWhiteSpace(input.PatternLengthCategory)
                ? null
                : PatternLengthCategory.FromName(input.PatternLengthCategory),
            PatternRatioCategory = string.IsNullOrWhiteSpace(input.PatternRatioCategory)
                ? null
                : PatternRatioCategory.FromName(input.PatternRatioCategory),
            OilPatternRevealDateTime = input.OilPatternRevealDateTime
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
