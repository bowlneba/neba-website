using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Bowlers.SearchBowlers;
using Neba.Api.Messaging;
using Neba.Api.RateLimiting;

namespace Neba.Api.Features.Bowlers.SearchBowlers;

internal sealed class SearchBowlersEndpoint(
    IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>> queryHandler)
    : Endpoint<SearchBowlersRequest, CollectionResponse<BowlerSearchResponse>>
{
    public override void Configure()
    {
        Get(string.Empty);
        Group<BowlersEndpointGroup>();

        Options(options => options
            .WithVersionSet("Bowlers")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(RateLimitingConfiguration.BowlerSearchPolicy));

        AllowAnonymous();

        Description(description => description
            .WithName("SearchBowlers")
            .WithTags("Public")
            .Produces<CollectionResponse<BowlerSearchResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status429TooManyRequests));
    }

    public override async Task HandleAsync(SearchBowlersRequest req, CancellationToken ct)
    {
        var results = await queryHandler.HandleAsync(new SearchBowlersQuery { Name = req.Name }, ct);

        await Send.OkAsync(
            new CollectionResponse<BowlerSearchResponse>
            {
                Items = [.. results.Select(result => new BowlerSearchResponse
                {
                    BowlerId = result.Id.Value.ToString(),
                    Name = result.Name.ToDisplayName(),
                    UsbcId = result.UsbcId
                })]
            },
            ct);
    }
}