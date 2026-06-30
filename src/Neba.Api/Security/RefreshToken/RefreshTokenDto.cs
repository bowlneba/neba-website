namespace Neba.Api.Security.RefreshToken;

internal sealed record RefreshTokenDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required Ulid UserId { get; init; }

    public required string Email { get; init; }
}