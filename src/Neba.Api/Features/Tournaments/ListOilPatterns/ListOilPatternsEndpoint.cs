using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed class ListOilPatternsEndpoint(IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<OilPatternSummaryResponse>>
{
    private readonly IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<OilPatternsEndpointGroup>();

        Options(options => options
            .WithVersionSet("OilPatterns")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListOilPatterns")
            .WithTags("Public")
            .Produces<CollectionResponse<OilPatternSummaryResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListOilPatternsQuery(), ct);

        var response = new CollectionResponse<OilPatternSummaryResponse>
        {
            Items = [.. result.Select(p => new OilPatternSummaryResponse
            {
                OilPatternId = p.Id.Value.ToString(),
                Name = p.Name,
                Length = p.Length,
                Volume = p.Volume,
                LeftRatio = p.LeftRatio,
                RightRatio = p.RightRatio,
                KegelId = p.KegelId,
                LengthCategory = p.LengthCategory,
                RatioCategory = p.RatioCategory
            })]
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
