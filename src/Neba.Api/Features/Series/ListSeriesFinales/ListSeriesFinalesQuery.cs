using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListSeriesFinales;

/// <summary>Lists the Finale of every Series that has one. Not cached: a handful of rows.</summary>
internal sealed record ListSeriesFinalesQuery
    : IQuery<IReadOnlyCollection<SeriesFinaleSummaryDto>>;