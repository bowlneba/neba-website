namespace Neba.Api.Security.Login;

internal sealed record LoginDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required Ulid UserId { get; init; }

    public required string Email { get; init; }
}