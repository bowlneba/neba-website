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

    #region System

    /// <summary>
    /// Permission to create a new user in the system.
    /// </summary>
    public static readonly Permissions CreateUser = new("System.CreateUser", "Create User");

    /// <summary>
    /// Permission to reset a user's password in the system.
    /// </summary>
    public static readonly Permissions ResetUserPassword = new("System.ResetUserPassword", "Reset User Password");

    /// <summary>
    /// Permission to list user accounts in the system.
    /// </summary>
    public static readonly Permissions GetUsers = new("System.GetUsers", "Get Users");

    #endregion

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

    /// <summary>
    /// Permission to edit an existing tournament.
    /// </summary>
    public static readonly Permissions EditTournament = new("Tournaments.EditTournament", "Edit Tournament");

    /// <summary>
    /// Permission to delete an existing tournament.
    /// </summary>
    public static readonly Permissions DeleteTournament = new("Tournaments.DeleteTournament", "Delete Tournament");

    /// <summary>
    /// A collection of permissions related to tournament management.
    /// </summary>
    public static readonly IReadOnlyCollection<Permissions> TournamentManagementPermissions =
    [
        CreateTournament,
        ManageTournamentSponsors,
        EditTournament,
        DeleteTournament
    ];

    /// <summary>
    /// Policy name satisfied when the caller holds any permission in <see cref="TournamentManagementPermissions"/>.
    /// </summary>
    public const string CanManageTournamentsPolicyName = "CanManageTournaments";

    #endregion

    #region Background Jobs

    /// <summary>
    /// Permission to view and manage the background jobs dashboard.
    /// </summary>
    public static readonly Permissions ViewBackgroundJobsDashboard = new("BackgroundJobs.ViewDashboard", "View Background Jobs Dashboard");

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