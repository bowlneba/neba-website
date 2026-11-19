using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeries;

internal sealed class ListSeriesEndpoint(
    IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<SeriesSummaryResponse>>
{
    private readonly IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListSeries")
            .WithTags("Public")
            .Produces<CollectionResponse<SeriesSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListSeriesQuery(), ct);

        var response = new CollectionResponse<SeriesSummaryResponse>
        {
            Items = [.. result.Select(dto => new SeriesSummaryResponse
            {
                Id = dto.Id.Value.ToString(),
                Name = dto.Name,
                SeasonId = dto.SeasonId.Value.ToString(),
                SeasonDescription = dto.SeasonDescription,
                SeasonStartDate = dto.SeasonStartDate,
                SeasonEndDate = dto.SeasonEndDate,
                QualifyingStageCount = dto.QualifyingStageCount,
                SurvivorStageCount = dto.SurvivorStageCount,
                PlacementStageCount = dto.PlacementStageCount,
                IsLocked = dto.IsLocked,
                LogoUrl = dto.LogoUrl,
                StageTournamentIds = [.. dto.StageTournamentIds.Select(id => id.Value.ToString())],
                NextStage = dto.NextStage is null ? null : new SeriesNextStageResponse
                {
                    Kind = dto.NextStage.Kind,
                    TournamentId = dto.NextStage.TournamentId.Value.ToString(),
                    TournamentName = dto.NextStage.TournamentName,
                    TournamentStartDate = dto.NextStage.TournamentStartDate,
                    TournamentEndDate = dto.NextStage.TournamentEndDate,
                },
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}