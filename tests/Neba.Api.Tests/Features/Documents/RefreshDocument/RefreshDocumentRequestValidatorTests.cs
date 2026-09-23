using Neba.Api.Contracts.Documents.RefreshDocument;
using Neba.Api.Features.Documents.RefreshDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Documents.RefreshDocument;

[UnitTest]
[Component("Documents")]
public sealed class RefreshDocumentRequestValidatorTests
{
    private readonly RefreshDocumentRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new RefreshDocumentRequest { DocumentName = "bylaws" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should succeed when DocumentName is exactly 100 characters")]
    public void Validate_ShouldSucceed_WhenDocumentNameIsAtMaxLength()
    {
        // Arrange
        var request = new RefreshDocumentRequest { DocumentName = new string('a', 100) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with DocumentNameRequired error when DocumentName is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Validate_ShouldFail_WhenDocumentNameIsEmptyOrWhitespace(string documentName)
    {
        // Arrange
        var request = new RefreshDocumentRequest { DocumentName = documentName };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshDocumentRequest.DocumentName)
            && e.ErrorCode == "RefreshDocumentRequest.DocumentNameRequired");
    }

    [Fact(DisplayName = "Validate should fail with DocumentNameRequired error when DocumentName is null")]
    public void Validate_ShouldFail_WhenDocumentNameIsNull()
    {
        // Arrange
#nullable disable
        var request = new RefreshDocumentRequest { DocumentName = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshDocumentRequest.DocumentName)
            && e.ErrorCode == "RefreshDocumentRequest.DocumentNameRequired");
    }

    [Fact(DisplayName = "Validate should fail with DocumentNameTooLong error when DocumentName exceeds 100 characters")]
    public void Validate_ShouldFail_WhenDocumentNameExceedsMaxLength()
    {
        // Arrange
        var request = new RefreshDocumentRequest { DocumentName = new string('a', 101) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshDocumentRequest.DocumentName)
            && e.ErrorCode == "RefreshDocumentRequest.DocumentNameTooLong");
    }
}