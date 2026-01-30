namespace Neba.Website.Server.Services;

internal sealed record NebaApiConfiguration
{
    public required string BaseUrl { get; init; }
}