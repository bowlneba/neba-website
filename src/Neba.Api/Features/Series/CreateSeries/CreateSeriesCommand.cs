using ErrorOr;

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.CreateSeries;

internal sealed record CreateSeriesCommand
    : ICommand<SeriesId>
{
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public StoredFile? Logo { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }

    /// <summary>Null means the Series has no Finale.</summary>
    public SeriesFinaleDefinition? Finale { get; init; }
}