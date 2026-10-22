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

        foreach (var (titleSponsor, logoContainer, logoPath) in sponsors)
        {
            if (titleSponsor && logoContainer is not null && logoPath is not null)
            {
                return (logoContainer, logoPath);
            }
        }

        return (null, null);
    }
}