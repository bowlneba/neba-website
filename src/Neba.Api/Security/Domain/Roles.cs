namespace Neba.Api.Security.Domain;

/// <summary>
/// Defines the roles available in the system.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Administrator role with full access to all resources.
    /// </summary>
    public const string Admin = nameof(Admin);

    /// <summary>
    /// Webmaster role with access to website management features.
    /// </summary>
    public const string Webmaster =  nameof(Webmaster);

    /// <summary>
    /// Member role with access to standard user features.
    /// </summary>
    public const string Member = nameof(Member);
}