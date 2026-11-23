using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed class ListStageResultsEndpoint(
    IQueryHandler<ListStageResultsQuery, ErrorOr<IReadOnlyList<StageResultSummaryDto>>> queryHandler)
    : Endpoint<ListStageResultsRequest, CollectionResponse<StageResultSummaryResponse>>
{
    private readonly IQueryHandler<ListStageResultsQuery, ErrorOr<IReadOnlyList<StageResultSummaryDto>>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/stages/{stageId}/results");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageAdvancement.PolicyName);

        Description(description => description
            .WithName("ListStageResults")
            .WithTags("Admin")
            .Produces<CollectionResponse<StageResultSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(ListStageResultsRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new ListStageResultsQuery
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

        var response = new CollectionResponse<StageResultSummaryResponse>
        {
            Items = [.. result.Value.Select(dto => new StageResultSummaryResponse
            {
                BowlerId = dto.BowlerId.Value.ToString(),
                BowlerName = dto.BowlerName,
                Place = dto.Place,
            })],
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}