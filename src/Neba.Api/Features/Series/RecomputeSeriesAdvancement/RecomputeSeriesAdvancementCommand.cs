using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed record RecomputeSeriesAdvancementCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }

    public required int Version { get; init; }

    public required bool ConfirmResetOfficial { get; init; }
}