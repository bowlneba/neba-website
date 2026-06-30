namespace Neba.Website.Server.Account;

internal sealed record AdminLoginSettings
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}