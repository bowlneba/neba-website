using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed record GetSeriesAdvancersQuery
    : IQuery<ErrorOr<SeriesAdvancersDto>>
{
    public required SeriesId SeriesId { get; init; }
}