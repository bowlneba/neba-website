using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentEndpoint(Messaging.ICommandHandler<CreateTournamentCommand, TournamentId> commandHandler)
    : Endpoint<CreateTournamentRequest, CreatedTournamentResponse>
{
    private readonly Messaging.ICommandHandler<CreateTournamentCommand, TournamentId> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateTournament.PolicyName);

        Description(description => description
            .WithName("CreateTournament")
            .WithTags("Admin")
            .Produces<CreatedTournamentResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateTournamentRequest req, CancellationToken ct)
    {
        var input = req.Tournament;

        var command = new CreateTournamentCommand
        {
            Name = input.Name,
            TournamentType = TournamentType.FromName(input.TournamentType),
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StatsEligible = input.StatsEligible,
            EntryFee = input.EntryFee,
            NebaAddedMoney = input.NebaAddedMoney,
            BowlingCenterId = TournamentInputMapper.ToBowlingCenterId(input.BowlingCenterCertificationNumber),
            ExternalRegistrationUrl = input.ExternalRegistrationUrl,
            Logo = TournamentInputMapper.ToLogo(input.Logo),
            OilPatternId = TournamentInputMapper.ToOilPatternId(input.OilPatternId),
            PatternLengthCategory = TournamentInputMapper.ToPatternLengthCategory(input.PatternLengthCategory),
            PatternRatioCategory = TournamentInputMapper.ToPatternRatioCategory(input.PatternRatioCategory),
            OilPatternRevealDateTime = input.OilPatternRevealDateTime
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        var response = new CreatedTournamentResponse
        {
            TournamentId = result.Value.Value.ToString()
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetTournament",
            routeValues: new { id = result.Value.Value.ToString() },
            responseBody: response,
            cancellation: ct);
    }
}