using Neba.Api.Features.Series.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Features.Series;

/// <summary>
/// Composes <see cref="AdvancementFacts"/> for a Series. An interface so the run can be tested with fixed facts.
/// </summary>
internal interface ISeriesAdvancementFactsReader
{
    Task<AdvancementFacts> ReadAsync(SeriesAggregate series, CancellationToken cancellationToken);
}