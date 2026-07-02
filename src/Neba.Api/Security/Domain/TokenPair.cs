namespace Neba.Api.Security.Domain;

internal sealed record TokenPair
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}