using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.GetSeriesAdvancers;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed class GetSeriesAdvancersEndpoint(
    IQueryHandler<GetSeriesAdvancersQuery, ErrorOr<SeriesAdvancersDto>> queryHandler)
    : Endpoint<GetSeriesAdvancersRequest, SeriesAdvancersResponse>
{
    private readonly IQueryHandler<GetSeriesAdvancersQuery, ErrorOr<SeriesAdvancersDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/advancers");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetSeriesAdvancers")
            .WithTags("Public")
            .Produces<SeriesAdvancersResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSeriesAdvancersRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new GetSeriesAdvancersQuery { SeriesId = new SeriesId(req.SeriesId) }, ct);

        if (result.IsError)
        {
            // NotFound is the only error the query returns.
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        await Send.OkAsync(new SeriesAdvancersResponse
        {
            Stages = [.. result.Value.Stages.Select(s => new StageAdvancersResponse
            {
                StageId = s.StageId.Value.ToString(),
                Order = s.Order,
                Kind = s.Kind,
                Advancers = [.. s.Advancers.Select(a => new AdvancerResponse
                {
                    BowlerId = a.BowlerId.Value.ToString(),
                    BowlerName = a.BowlerName,
                    Place = a.Place,
                    BlockTotal = a.BlockTotal,
                    IsBackfill = a.IsBackfill,
                })],
            })],
        }, ct);
    }
}
