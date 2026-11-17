using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed record GetSeriesQuery
    : IQuery<ErrorOr<SeriesDetailDto>>
{
    public required SeriesId Id { get; init; }
}
