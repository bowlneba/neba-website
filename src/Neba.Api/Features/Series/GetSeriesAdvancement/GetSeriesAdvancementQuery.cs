using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed record GetSeriesAdvancementQuery
    : IQuery<ErrorOr<SeriesAdvancementDto>>
{
    public required SeriesId SeriesId { get; init; }
}