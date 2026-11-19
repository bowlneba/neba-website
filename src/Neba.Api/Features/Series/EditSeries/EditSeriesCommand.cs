using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.EditSeries;

internal sealed record EditSeriesCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }
    public required Ulid CallerUserId { get; init; }
    public required string Name { get; init; }
    public StoredFile? Logo { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }

    /// <summary>Null removes the Finale (refused while locked).</summary>
    public SeriesFinaleDefinition? Finale { get; init; }
}