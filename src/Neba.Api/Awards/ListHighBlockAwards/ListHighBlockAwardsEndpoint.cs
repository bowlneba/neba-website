using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;
using Neba.Application.Awards.ListHighBlockAwards;
using Neba.Application.Messaging;

namespace Neba.Api.Awards.ListHighBlockAwards;

internal sealed class ListHighBlockAwardsEndpoint(IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<HighBlockAwardResponse>>
{
    private readonly IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("high-block");
        Group<AwardsGroup>();

        Options(options => options
            .WithVersionSet("Awards")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListHighBlockAwards")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListHighBlockAwardsQuery(), ct);

        var response = new CollectionResponse<HighBlockAwardResponse>
        {
            Items = [.. result
                .Select(a => new HighBlockAwardResponse
                {
                    Season = a.Season,
                    BowlerName = a.BowlerName.ToFormalName(),
                    Score = a.Score,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}