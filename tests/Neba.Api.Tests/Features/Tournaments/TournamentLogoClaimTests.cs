using Neba.Api.Features.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentLogoClaimTests
{
    [Fact(DisplayName = "Resolve returns null when the logo is null")]
    public void Resolve_ShouldReturnNull_WhenLogoIsNull()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create();

        // Act
        var result = TournamentLogoClaim.Resolve(null, currentLogo);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Resolve returns the stored logo when the logo names the same file with different metadata")]
    public void Resolve_ShouldReturnCurrentLogo_WhenLogoNamesSameFile()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 100);
        var logo = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 999_999);

        // Act
        var result = TournamentLogoClaim.Resolve(logo, currentLogo);

        // Assert
        result.ShouldBeSameAs(currentLogo);
    }

    [Fact(DisplayName = "Resolve returns the supplied logo when the tournament has no logo")]
    public void Resolve_ShouldReturnLogo_WhenCurrentLogoIsNull()
    {
        // Arrange
        var logo = StoredFileFactory.Create();

        // Act
        var result = TournamentLogoClaim.Resolve(logo, null);

        // Assert
        result.ShouldBeSameAs(logo);
    }

    [Fact(DisplayName = "Resolve returns the supplied logo when the path differs")]
    public void Resolve_ShouldReturnLogo_WhenPathDiffers()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create(path: "old.png");
        var logo = StoredFileFactory.Create(path: "new.png");

        // Act
        var result = TournamentLogoClaim.Resolve(logo, currentLogo);

        // Assert
        result.ShouldBeSameAs(logo);
    }

    [Fact(DisplayName = "Resolve returns the supplied logo when the container differs")]
    public void Resolve_ShouldReturnLogo_WhenContainerDiffers()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create(container: "container-a");
        var logo = StoredFileFactory.Create(container: "container-b");

        // Act
        var result = TournamentLogoClaim.Resolve(logo, currentLogo);

        // Assert
        result.ShouldBeSameAs(logo);
    }

    [Fact(DisplayName = "IsSameFile returns true when both logos are null")]
    public void IsSameFile_ShouldReturnTrue_WhenBothNull()
    {
        // Arrange & Act
        var result = TournamentLogoClaim.IsSameFile(null, null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsSameFile returns false when the logo is null and a current logo exists")]
    public void IsSameFile_ShouldReturnFalse_WhenLogoNullAndCurrentLogoExists()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create();

        // Act
        var result = TournamentLogoClaim.IsSameFile(null, currentLogo);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns false when a logo is set and no current logo exists")]
    public void IsSameFile_ShouldReturnFalse_WhenCurrentLogoNull()
    {
        // Arrange
        var logo = StoredFileFactory.Create();

        // Act
        var result = TournamentLogoClaim.IsSameFile(logo, null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns true when container and path match and metadata differs")]
    public void IsSameFile_ShouldReturnTrue_WhenContainerAndPathMatch()
    {
        // Arrange
        var logo = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 1);
        var currentLogo = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 2);

        // Act
        var result = TournamentLogoClaim.IsSameFile(logo, currentLogo);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsSameFile returns false when the path differs")]
    public void IsSameFile_ShouldReturnFalse_WhenPathDiffers()
    {
        // Arrange
        var logo = StoredFileFactory.Create(path: "a.png");
        var currentLogo = StoredFileFactory.Create(path: "b.png");

        // Act
        var result = TournamentLogoClaim.IsSameFile(logo, currentLogo);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns false when the container differs")]
    public void IsSameFile_ShouldReturnFalse_WhenContainerDiffers()
    {
        // Arrange
        var logo = StoredFileFactory.Create(container: "container-a");
        var currentLogo = StoredFileFactory.Create(container: "container-b");

        // Act
        var result = TournamentLogoClaim.IsSameFile(logo, currentLogo);

        // Assert
        result.ShouldBeFalse();
    }
}
