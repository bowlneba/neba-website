using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeriesFinales;

internal sealed class ListSeriesFinalesEndpoint(
    IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<SeriesFinaleSummaryResponse>>
{
    private readonly IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("finales");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListSeriesFinales")
            .WithTags("Public")
            .Produces<CollectionResponse<SeriesFinaleSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListSeriesFinalesQuery(), ct);

        var response = new CollectionResponse<SeriesFinaleSummaryResponse>
        {
            Items = [.. result.Select(dto => new SeriesFinaleSummaryResponse
            {
                SeriesId = dto.SeriesId.Value.ToString(),
                SeriesName = dto.SeriesName,
                SeasonId = dto.SeasonId.Value.ToString(),
                LogoUrl = dto.LogoUrl,
                Finale = new SeriesFinaleResponse
                {
                    FinalistCount = dto.Finale.FinalistCount,
                    Date = dto.Finale.Date,
                    StartTime = dto.Finale.StartTime,
                    BowlingCenterId = dto.Finale.BowlingCenterId,
                    BowlingCenterName = dto.Finale.BowlingCenterName,
                    BowlingCenterCity = dto.Finale.BowlingCenterCity,
                    BowlingCenterState = dto.Finale.BowlingCenterState,
                },
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}