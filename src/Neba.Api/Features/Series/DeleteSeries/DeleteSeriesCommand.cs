using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed record DeleteSeriesCommand
    : ICommand<Deleted>
{
    public required SeriesId SeriesId { get; init; }
}
