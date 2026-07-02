namespace Neba.Api.Contracts.Security.GetCurrentUser;

/// <summary>
/// Represents the response for the "Me" endpoint, which provides information about the currently authenticated user.
/// </summary>
public sealed record GetCurrentUserResponse
{
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the roles assigned to the user.
    /// </summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>
    /// Gets the permissions assigned to the user. This property is a collection of permission names that define what actions the user is allowed to perform within the application.
    /// </summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }

    /// <summary>
    /// Gets the USBC ID of the user, if available. This property is optional and may be null if the user does not have a USBC ID associated with their account.
    /// </summary>
    public string? UsbcId { get; init; }
}