namespace Neba.Website.Server.Services;

internal sealed record NebaApiConfiguration
{
    public required Uri BaseUrl { get; init; }
}