using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed class GetSeriesEndpoint(
    IQueryHandler<GetSeriesQuery, ErrorOr<SeriesDetailDto>> queryHandler)
    : Endpoint<GetSeriesRequest, SeriesDetailResponse>
{
    private readonly IQueryHandler<GetSeriesQuery, ErrorOr<SeriesDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetSeries")
            .WithTags("Public")
            .Produces<SeriesDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(GetSeriesRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetSeriesQuery { Id = new SeriesId(req.SeriesId) }, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        var response = new SeriesDetailResponse
        {
            Id = dto.Id.Value.ToString(),
            Name = dto.Name,
            SeasonId = dto.SeasonId.Value.ToString(),
            SeasonDescription = dto.SeasonDescription,
            SeasonStartDate = dto.SeasonStartDate,
            SeasonEndDate = dto.SeasonEndDate,
            IsLocked = dto.IsLocked,
            Finale = dto.Finale?.ToResponse(),
            LogoUrl = dto.LogoUrl,
            LogoContainer = dto.LogoContainer,
            LogoPath = dto.LogoPath,
            LogoContentType = dto.LogoContentType,
            LogoSizeInBytes = dto.LogoSizeInBytes,
            Stages = [.. dto.Stages.Select(stage => new SeriesStageResponse
            {
                Id = stage.Id.Value.ToString(),
                Order = stage.Order,
                Kind = stage.Kind,
                Status = stage.Status,
                AdvancingCount = stage.AdvancingCount,
                SquadDate = stage.SquadDate,
                SquadStartTime = stage.SquadStartTime,
                TournamentId = stage.TournamentId.Value.ToString(),
                TournamentName = stage.TournamentName,
                TournamentStartDate = stage.TournamentStartDate,
                TournamentEndDate = stage.TournamentEndDate,
                TournamentType = stage.TournamentType,
                TeamSize = stage.TeamSize,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}