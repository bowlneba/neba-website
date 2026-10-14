using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournamentLogo;
using Neba.Api.Features.Tournaments.EditTournamentLogo;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentLogo;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentLogoRequestValidatorTests
{
    private readonly EditTournamentLogoRequestValidator _validator = new();

    private static TournamentLogoInput ValidLogo()
        => new()
        {
            Container = "bowlneba-public",
            Path = "tournaments/logo/abc.png",
            ContentType = "image/png",
            SizeInBytes = 2048
        };

    private static EditTournamentLogoRequest ValidRequest(TournamentLogoInput? logo = null)
        => new() { Id = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E", Input = new EditTournamentLogoInput { Logo = logo ?? ValidLogo() } };

    [Fact(DisplayName = "Validate should succeed when the request has an id and a valid logo")]
    public void Validate_ShouldSucceed_WhenRequestHasValidLogo()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed when the logo is null (remove the logo)")]
    public void Validate_ShouldSucceed_WhenLogoIsNull()
    {
        // Arrange
        var request = new EditTournamentLogoRequest { Id = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E", Input = new EditTournamentLogoInput { Logo = null } };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired when Id is empty")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsEmpty()
    {
        // Arrange
        var request = ValidRequest() with { Id = string.Empty };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength when Id is not a 26-character ULID")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsNotUlidLength()
    {
        // Arrange
        var request = ValidRequest() with { Id = "not-a-ulid" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.IdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with LogoContainerRequired when the logo container is empty")]
    public void Validate_ShouldFailWithLogoContainerRequired_WhenContainerIsEmpty()
    {
        // Arrange
        var request = ValidRequest(ValidLogo() with { Container = string.Empty });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.LogoContainerRequired");
    }

    [Fact(DisplayName = "Validate should fail with LogoPathRequired when the logo path is empty")]
    public void Validate_ShouldFailWithLogoPathRequired_WhenPathIsEmpty()
    {
        // Arrange
        var request = ValidRequest(ValidLogo() with { Path = string.Empty });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.LogoPathRequired");
    }

    [Fact(DisplayName = "Validate should fail with LogoContentTypeRequired when the logo content type is empty")]
    public void Validate_ShouldFailWithLogoContentTypeRequired_WhenContentTypeIsEmpty()
    {
        // Arrange
        var request = ValidRequest(ValidLogo() with { ContentType = string.Empty });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.LogoContentTypeRequired");
    }

    [Theory(DisplayName = "Validate should fail with LogoSizeInvalid when the logo size is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFailWithLogoSizeInvalid_WhenSizeIsNotPositive(long size)
    {
        // Arrange
        var request = ValidRequest(ValidLogo() with { SizeInBytes = size });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.LogoSizeInvalid");
    }

#nullable disable
    [Fact(DisplayName = "Validate should fail with InputRequired when the input is missing")]
    public void Validate_ShouldFailWithInputRequired_WhenInputIsNull()
    {
        // Arrange
        var request = new EditTournamentLogoRequest { Id = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E", Input = null };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditTournamentLogoRequest.InputRequired");
    }
#nullable enable
}
