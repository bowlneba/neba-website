using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersEndpoint(
    IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>> queryHandler)
    : Endpoint<ListEligibleBowlersRequest, EligibleBowlersResponse>
{
    private readonly IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/eligible-bowlers");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListEligibleBowlers")
            .WithTags("Public")
            .Produces<EligibleBowlersResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(ListEligibleBowlersRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new ListEligibleBowlersQuery { Id = new TournamentId(req.TournamentId) },
            ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description, error.Code);
            }

            await Send.ErrorsAsync(
                result.FirstError.Type == ErrorType.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status500InternalServerError,
                ct);

            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.OkAsync(
            new EligibleBowlersResponse
            {
                Requirements = result.Value.Requirements is null
                    ? null
                    : new EligibilityRequirementsResponse
                    {
                        MinTournamentsCount = result.Value.Requirements.MinTournamentsCount
                    },
                Bowlers = [.. result.Value.Bowlers.Select(bowler => new EligibleBowlerSummaryResponse
                {
                    Name = bowler.Name,
                    UsbcId = bowler.UsbcId
                })]
            },
            ct);
    }
}