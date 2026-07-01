using System.Diagnostics.CodeAnalysis;

using Ardalis.SmartEnum;

namespace Neba.Api.Contracts.Security;

/// <summary>
/// Represents a permission in the system.
/// </summary>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces",
    Justification = "Permissions is the established domain term for this concept; System.Security.Permissions is not referenced anywhere in this codebase.")]
public sealed class Permissions
    : SmartEnum<Permissions, string>
{
    /// <summary>
    /// This is a temporary permission to set us up until real permissions come into the picture
    /// </summary>
    public static readonly Permissions Read = new("Read", "Read");
    private Permissions(string key, string name)
        : base(name, key)
    { }

    /// <summary>
    /// Gets the name of the policy associated with this permission.
    /// This is used to create authorization policies dynamically based on permissions.
    /// The policy name is in the format "Permission:{PermissionValue}".
    /// For example, if the permission value is "Read", the policy name will be "Permission:Read".
    /// This allows for a consistent way to reference permissions in authorization checks.
    /// </summary>
    public string PolicyName
        => $"Permission:{Value}";
}