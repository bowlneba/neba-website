using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed record RemoveStageOverrideCommand
    : ICommand<Deleted>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }

    public required StageOverrideId OverrideId { get; init; }

    public required int Version { get; init; }
}