using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.EditSeriesLogo;

internal sealed record EditSeriesLogoCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }

    /// <summary>Null removes the logo.</summary>
    public StoredFile? Logo { get; init; }
}