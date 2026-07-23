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
    /// The claim type used to carry a permission value on a <see cref="System.Security.Claims.ClaimsPrincipal"/>
    /// (JWT claim, role claim, and cookie identity claim all agree on this single constant).
    /// </summary>
    public const string ClaimType = "permission";

    #region News

    /// <summary>
    /// Permission to create a news article.
    /// </summary>
    public static readonly Permissions CreateArticle = new("News.CreateArticle", "Create Article");

    /// <summary>
    /// Permission to edit a news article.
    /// </summary>
    public static readonly Permissions EditArticle = new("News.EditArticle", "Edit Article");

    /// <summary>
    /// Permission to delete a news article.
    /// </summary>
    public static readonly Permissions DeleteArticle = new("News.DeleteArticle", "Delete Article");

    /// <summary>
    /// A collection of permissions related to article management.
    /// </summary>
    public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
    [
        CreateArticle,
        EditArticle,
        DeleteArticle,
    ];

    /// <summary>
    /// Policy name satisfied when the caller holds any permission in <see cref="ArticleManagementPermissions"/>.
    /// </summary>
    public const string CanManageArticlesPolicyName = "CanManageArticles";

    #endregion

    #region Sponsors

    /// <summary>
    /// Permission to create a sponsor.
    /// </summary>
    public static readonly Permissions CreateSponsor = new("Sponsors.CreateSponsor", "Create Sponsor");

    /// <summary>
    /// Permission to edit a sponsor.
    /// </summary>
    public static readonly Permissions EditSponsor = new("Sponsors.EditSponsor", "Edit Sponsor");

    /// <summary>
    /// A collection of permissions related to sponsor management.
    /// </summary>
    public static readonly IReadOnlyCollection<Permissions> SponsorManagementPermissions =
    [
        CreateSponsor,
        EditSponsor,
    ];

    /// <summary>
    /// Policy name satisfied when the caller holds any permission in <see cref="SponsorManagementPermissions"/>.
    /// </summary>
    public const string CanManageSponsorsPolicyName = "CanManageSponsors";

    #endregion

    #region Tournaments

    /// <summary>
    /// Permission to create a tournament.
    /// </summary>
    public static readonly Permissions CreateTournament = new("Tournaments.CreateTournament", "Create Tournament");

    /// <summary>
    /// Permission to add or remove sponsors on a tournament.
    /// </summary>
    public static readonly Permissions ManageTournamentSponsors = new("Tournaments.ManageSponsors", "Manage Tournament Sponsors");

    #endregion

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