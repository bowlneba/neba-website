using Microsoft.AspNetCore.Identity;

namespace Neba.Api.Security.Domain;

/// <summary>
/// Application user class that extends IdentityUser with a strongly typed UserId as the primary key and an optional UsbcId property.
/// </summary>
public class ApplicationUser
    : IdentityUser<Ulid>
{
    /// <summary>
    /// Gets or sets the optional UsbcId for this user.
    /// </summary>
    public string? UsbcId { get; init; }
}