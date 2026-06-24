namespace Neba.Api.Security.Domain;

internal sealed record StoredRefreshToken
{
    public required string Hash { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }
}