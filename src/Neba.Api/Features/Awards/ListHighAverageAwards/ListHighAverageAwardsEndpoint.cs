using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;
using Neba.Application.Awards.ListHighAverageAwards;
using Neba.Application.Messaging;

namespace Neba.Api.Features.Awards.ListHighAverageAwards;

internal sealed class ListHighAverageAwardsEndpoint(IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<HighAverageAwardResponse>>
{
    private readonly IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("high-average");
        Group<AwardsGroup>();

        Options(options => options
            .WithVersionSet("Awards")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListHighAverageAwards")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListHighAverageAwardsQuery(), ct);

        var response = new CollectionResponse<HighAverageAwardResponse>
        {
            Items = [.. result
                .Select(a => new HighAverageAwardResponse
                {
                    Season = a.Season,
                    BowlerName = a.BowlerName.ToFormalName(),
                    Average = a.Average,
                    TotalGames = a.TotalGames,
                    TournamentsParticipated = a.TournamentsParticipated,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}