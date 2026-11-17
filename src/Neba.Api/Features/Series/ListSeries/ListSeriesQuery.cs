using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeries;

/// <summary>Lists every Series, newest season first. Not cached: Series reads are rare and small.</summary>
internal sealed record ListSeriesQuery
    : IQuery<IReadOnlyCollection<SeriesSummaryDto>>;
