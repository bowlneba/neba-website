using Microsoft.AspNetCore.Identity;

using Neba.Api.Compliance;

using PersonalDataAttribute = Neba.Api.Compliance.PersonalDataAttribute;

namespace Neba.Api.Security.Domain;

/// <summary>
/// Application user class that extends IdentityUser with a strongly typed UserId as the primary key and an optional UsbcId property.
/// </summary>
public sealed class ApplicationUser
    : IdentityUser<Ulid>
{
    /// <inheritdoc />
    [PrivateData]
    public override string? PasswordHash { get => base.PasswordHash; set => base.PasswordHash = value; }

    /// <inheritdoc />
    [PrivateData]
    public override string? SecurityStamp { get => base.SecurityStamp; set => base.SecurityStamp = value; }

    /// <inheritdoc />
    [PrivateData]
    public override string? ConcurrencyStamp { get => base.ConcurrencyStamp; set => base.ConcurrencyStamp = value; }

    /// <inheritdoc />
    [PersonalData]
    public override string? Email { get => base.Email; set => base.Email = value; }

    /// <inheritdoc />
    [PersonalData]
    public override string? PhoneNumber { get => base.PhoneNumber; set => base.PhoneNumber = value; }

    /// <inheritdoc />
    [PersonalData]
    public override string? UserName { get => base.UserName; set => base.UserName = value; }

    /// <summary>
    /// Gets or sets the optional UsbcId for this user.
    /// </summary>
    public string? UsbcId { get; init; }
}