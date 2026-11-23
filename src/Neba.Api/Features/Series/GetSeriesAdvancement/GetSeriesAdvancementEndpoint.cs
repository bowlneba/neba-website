using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementEndpoint(
    IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>> queryHandler)
    : Endpoint<GetSeriesAdvancementRequest, SeriesAdvancementResponse>
{
    private readonly IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/advancement");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CanViewSeriesAdvancementPolicyName);

        Description(description => description
            .WithName("GetSeriesAdvancement")
            .WithTags("Admin")
            .Produces<SeriesAdvancementResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSeriesAdvancementRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new GetSeriesAdvancementQuery { SeriesId = new SeriesId(req.SeriesId) }, ct);

        if (result.IsError)
        {
            // NotFound is the only error the query returns.
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        await Send.OkAsync(new SeriesAdvancementResponse
        {
            Version = dto.Version,
            BackfillAsOf = dto.BackfillAsOf,
            TargetField = dto.TargetField,
            Stages = [.. dto.Stages.Select(s => new StageAdvancementResponse
            {
                StageId = s.StageId.Value.ToString(),
                Order = s.Order,
                Kind = s.Kind,
                Status = s.Status,
                IsStale = s.IsStale,
                NeedsSquad = s.NeedsSquad,
                ComputedAt = s.ComputedAt,
                CanMakeOfficial = s.CanMakeOfficial,
                OfficialAt = s.OfficialAt,
                SlotCount = s.SlotCount,
                VacantSlotCount = s.VacantSlotCount,
                TournamentId = s.TournamentId.Value.ToString(),
                TournamentStatus = s.TournamentStatus,
                ResolvedSquadId = s.ResolvedSquadId?.Value.ToString(),
                Entries = [.. s.Entries.Select(e => new FieldEntryResponse
                {
                    BowlerId = e.BowlerId.Value.ToString(),
                    BowlerName = e.BowlerName,
                    SourceStageId = e.SourceStageId?.Value.ToString(),
                    Place = e.Place,
                    BackfillPoints = e.BackfillPoints,
                    ViaOverride = e.ViaOverride,
                })],
                AlreadyAdvanced = [.. s.AlreadyAdvanced.Select(a => new AlreadyAdvancedResponse
                {
                    BowlerId = a.BowlerId.Value.ToString(),
                    BowlerName = a.BowlerName,
                    Place = a.Place,
                    EarlierStageId = a.EarlierStageId.Value.ToString(),
                    EarlierStageOrder = a.EarlierStageOrder,
                })],
                Overrides = [.. s.Overrides.Select(o => new StageOverrideResponse
                {
                    OverrideId = o.OverrideId.Value.ToString(),
                    BowlerId = o.BowlerId.Value.ToString(),
                    BowlerName = o.BowlerName,
                    Kind = o.Kind,
                    Reason = o.Reason,
                    CreatedBy = o.CreatedBy,
                    CreatedAt = o.CreatedAt,
                    Applied = o.Applied,
                })],
                SurvivorOutcomes = [.. s.SurvivorOutcomes.Select(o => new SurvivorOutcomeResponse
                {
                    BowlerId = o.BowlerId.Value.ToString(),
                    BowlerName = o.BowlerName,
                    BlockTotal = o.BlockTotal,
                    Advanced = o.Advanced,
                })],
            })],
            Positions = [.. dto.Positions.Select(p => new SeriesPositionResponse
            {
                BowlerId = p.BowlerId.Value.ToString(),
                BowlerName = p.BowlerName,
                Position = p.Position,
                Bowled = p.Bowled,
                IsFinalist = p.IsFinalist,
                TournamentPlace = p.TournamentPlace,
            })],
        }, ct);
    }
}