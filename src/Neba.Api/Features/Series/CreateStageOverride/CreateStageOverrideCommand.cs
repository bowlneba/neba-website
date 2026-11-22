using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.CreateStageOverride;

internal sealed record CreateStageOverrideCommand
    : ICommand<CreatedStageOverride>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }

    public required OverrideKind Kind { get; init; }

    public required BowlerId BowlerId { get; init; }

    public required string Reason { get; init; }

    public required Ulid CallerUserId { get; init; }

    public required int Version { get; init; }
}

internal sealed record CreatedStageOverride(StageOverrideId OverrideId, int Version);