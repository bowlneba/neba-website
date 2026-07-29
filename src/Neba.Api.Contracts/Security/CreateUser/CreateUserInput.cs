namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>The fields required to create a new staff user account.</summary>
public sealed record CreateUserInput
{
    /// <summary>The new user's email address. Used as both username and login identifier.</summary>
    public required string Email { get; init; }

    /// <summary>The role(s) to assign the new user. Must not include "Admin".</summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>Optional USBC member ID.</summary>
    public string? UsbcId { get; init; }

    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Additional claims to grant the new user. Empty when none are requested.</summary>
    public IReadOnlyCollection<ClaimInput> Claims { get; init; } = [];
}