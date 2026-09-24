using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentLogoResolverTests
{
    [Fact(DisplayName = "Resolve returns the tournament's own logo when it is set, regardless of sponsors")]
    public void Resolve_ShouldReturnTournamentLogo_WhenTournamentHasLogo()
    {
        // Arrange
        List<(bool TitleSponsor, string? LogoContainer, string? LogoPath)> sponsors =
            [(true, "sponsor-logos", "title-sponsor.jpg")];

        // Act
        var result = TournamentLogoResolver.Resolve("tournament-logos", "tournament.jpg", sponsors);

        // Assert
        result.Container.ShouldBe("tournament-logos");
        result.Path.ShouldBe("tournament.jpg");
    }

    [Fact(DisplayName = "Resolve returns the title sponsor's logo when the tournament has no logo of its own")]
    public void Resolve_ShouldReturnTitleSponsorLogo_WhenTournamentHasNoLogo()
    {
        // Arrange
        List<(bool TitleSponsor, string? LogoContainer, string? LogoPath)> sponsors =
            [(true, "sponsor-logos", "title-sponsor.jpg")];

        // Act
        var result = TournamentLogoResolver.Resolve(null, null, sponsors);

        // Assert
        result.Container.ShouldBe("sponsor-logos");
        result.Path.ShouldBe("title-sponsor.jpg");
    }

    [Fact(DisplayName = "Resolve ignores a non-title sponsor's logo when the tournament has no logo")]
    public void Resolve_ShouldIgnoreNonTitleSponsorLogo_WhenTournamentHasNoLogo()
    {
        // Arrange
        List<(bool TitleSponsor, string? LogoContainer, string? LogoPath)> sponsors =
            [(false, "sponsor-logos", "regular-sponsor.jpg")];

        // Act
        var result = TournamentLogoResolver.Resolve(null, null, sponsors);

        // Assert
        result.Container.ShouldBeNull();
        result.Path.ShouldBeNull();
    }

    [Fact(DisplayName = "Resolve returns null when the tournament has no logo and the title sponsor has no logo")]
    public void Resolve_ShouldReturnNull_WhenTournamentHasNoLogoAndTitleSponsorHasNoLogo()
    {
        // Arrange
        List<(bool TitleSponsor, string? LogoContainer, string? LogoPath)> sponsors =
            [(true, null, null)];

        // Act
        var result = TournamentLogoResolver.Resolve(null, null, sponsors);

        // Assert
        result.Container.ShouldBeNull();
        result.Path.ShouldBeNull();
    }

    [Fact(DisplayName = "Resolve returns null when the tournament has no logo and there are no sponsors")]
    public void Resolve_ShouldReturnNull_WhenTournamentHasNoLogoAndNoSponsors()
    {
        // Act
        var result = TournamentLogoResolver.Resolve(null, null, []);

        // Assert
        result.Container.ShouldBeNull();
        result.Path.ShouldBeNull();
    }
}