using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed class ListStageSquadsEndpoint(
    IQueryHandler<ListStageSquadsQuery, ErrorOr<IReadOnlyList<StageSquadSummaryDto>>> queryHandler)
    : Endpoint<ListStageSquadsRequest, CollectionResponse<StageSquadSummaryResponse>>
{
    private readonly IQueryHandler<ListStageSquadsQuery, ErrorOr<IReadOnlyList<StageSquadSummaryDto>>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/stages/{stageId}/squads");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("ListStageSquads")
            .WithTags("Admin")
            .Produces<CollectionResponse<StageSquadSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(ListStageSquadsRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new ListStageSquadsQuery
            {
                SeriesId = new SeriesId(req.SeriesId),
                StageId = new SeriesStageId(req.StageId),
            }, ct);

        if (result.IsError)
        {
            // NotFound (Series or Stage) is the only error the query returns.
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var response = new CollectionResponse<StageSquadSummaryResponse>
        {
            Items = [.. result.Value.Select(dto => new StageSquadSummaryResponse
            {
                SquadId = dto.SquadId.Value.ToString(),
                Date = dto.LocalDate,
                StartTime = dto.LocalStartTime,
                HasScores = dto.HasScores,
                IsSelected = dto.IsSelected,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}