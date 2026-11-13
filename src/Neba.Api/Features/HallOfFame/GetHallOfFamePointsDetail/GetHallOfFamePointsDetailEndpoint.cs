using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed class GetHallOfFamePointsDetailEndpoint(
    IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>> queryHandler)
    : Endpoint<GetHallOfFamePointsDetailRequest, HallOfFamePointsDetailResponse>
{
    private readonly IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("points/{bowlerId}");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetHallOfFamePointsDetail")
            .WithTags("Public")
            .Produces<HallOfFamePointsDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetHallOfFamePointsDetailRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new GetHallOfFamePointsDetailQuery { BowlerId = new BowlerId(req.BowlerId) }, ct);

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

        var response = new HallOfFamePointsDetailResponse
        {
            BowlerId = dto.BowlerId,
            BowlerName = dto.BowlerName,
            InductedCategories = dto.InductedCategories,
            TotalPoints = dto.TotalPoints,
            SeasonsWithPoints = dto.SeasonsWithPoints,
            Breakdown = [.. dto.Breakdown.Select(entry => new HallOfFamePointsEntryResponse
            {
                Source = entry.Source,
                Date = entry.Date,
                Points = entry.Points,
                RunningTotal = entry.RunningTotal,
                ReachedThreshold = entry.ReachedThreshold
            })],
            MembershipYears = [.. dto.MembershipYears.Select(year => new HallOfFameMembershipYearResponse
            {
                Year = year.Year,
                MembershipType = year.MembershipType
            })],
            MembershipHistoryComplete = dto.MembershipHistoryComplete
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
