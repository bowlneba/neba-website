namespace Neba.Website.Server.Account.CreateUser;

/// <summary>
/// The staff role options shown in the Create User form's role checkboxes. Mirrors
/// <see cref="Neba.Website.Server.Sponsors.SponsorCategoryOptions"/>'s pattern — a small, static,
/// client-side list. <c>Neba.Api.Security.Domain.Roles</c> is internal to <c>Neba.Api</c> and
/// unreachable here, so this list is intentionally duplicated rather than shared.
/// </summary>
internal static class StaffRoleOptions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Webmaster",
        "Manager",
        "Tournament Director",
        "Journalist",
        "Member"
    ];
}
