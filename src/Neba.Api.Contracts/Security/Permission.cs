using Ardalis.SmartEnum;

namespace Neba.Api.Contracts.Security;

/// <summary>
/// Represents a permission in the system.
/// </summary>
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
}