using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.MakeStageOfficial;

internal sealed record MakeStageOfficialCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }

    public required int Version { get; init; }
}