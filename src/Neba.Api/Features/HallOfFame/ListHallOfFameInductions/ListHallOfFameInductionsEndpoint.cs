using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFameInductions;

internal sealed class ListHallOfFameInductionsEndpoint(IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<HallOfFameInductionResponse>>
{
    private readonly IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("inductions");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListHallOfFameInductions")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        var response = new CollectionResponse<HallOfFameInductionResponse>
        {
            Items = [.. result
                .Select(i => new HallOfFameInductionResponse
                {
                    Year = i.Year,
                    BowlerName = $"{i.BowlerName.FirstName} {i.BowlerName.LastName}",
                    Categories = [.. i.Categories.Select(c => c.Name)],
                    PhotoUri = i.PhotoUri,
                })],
        };

        await Send.OkAsync(response, ct);
    }
}