using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Application.Messaging;
using Neba.Application.Seasons;
using Neba.Application.Seasons.ListSeasons;

namespace Neba.Api.Seasons.ListSeasons;

internal sealed class ListSeasonsEndpoint(IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<SeasonResponse>>
{
    private readonly IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<SeasonsGroup>();

        Options(options => options
            .WithVersionSet("Seasons")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListSeasons")
            .WithTags("Public")
            .Produces<CollectionResponse<SeasonResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListSeasonsQuery(), ct);

        var response = new CollectionResponse<SeasonResponse>
        {
            Items = [.. result
                .Select(s => new SeasonResponse
                {
                    Id = s.Id.Value.ToString(),
                    Description = s.Description,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}