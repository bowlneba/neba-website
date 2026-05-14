using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;

internal sealed class ListBowlerOfTheYearAwardsEndpoint(IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<BowlerOfTheYearAwardResponse>>
{
    private readonly IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("bowler-of-the-year");
        Group<AwardsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Awards")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListBowlerOfTheYearAwards")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListBowlerOfTheYearAwardsQuery(), ct);

        var response = new CollectionResponse<BowlerOfTheYearAwardResponse>
        {
            Items = [.. result
                .Select(a => new BowlerOfTheYearAwardResponse
                {
                    Season = a.Season,
                    BowlerName = a.BowlerName.ToFormalName(),
                    Category = a.Category,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}