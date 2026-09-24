namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Determines which blob (container, path) pair should be used as a tournament's display logo:
/// the tournament's own logo when set, otherwise its title sponsor's logo, otherwise none.
/// </summary>
internal static class TournamentLogoResolver
{
    public static (string? Container, string? Path) Resolve(
        string? tournamentLogoContainer,
        string? tournamentLogoPath,
        IEnumerable<(bool TitleSponsor, string? LogoContainer, string? LogoPath)> sponsors)
    {
        if (tournamentLogoContainer is not null && tournamentLogoPath is not null)
        {
            return (tournamentLogoContainer, tournamentLogoPath);
        }

        foreach (var sponsor in sponsors)
        {
            if (sponsor.TitleSponsor && sponsor.LogoContainer is not null && sponsor.LogoPath is not null)
            {
                return (sponsor.LogoContainer, sponsor.LogoPath);
            }
        }

        return (null, null);
    }
}