using Microsoft.AspNetCore.Http;

using Moq;
using Neba.Api.Contracts.Sponsors.UploadSponsorLogo;
using Neba.Api.Features.Sponsors.UploadSponsorLogo;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.UploadSponsorLogo;

[UnitTest]
[Component("Sponsors")]
public sealed class UploadSponsorLogoRequestValidatorTests
{
    private readonly UploadSponsorLogoRequestValidator _validator = new();

    private static IFormFile CreateFile(string contentType = "image/png", long length = 1024)
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.SetupGet(x => x.ContentType).Returns(contentType);
        fileMock.SetupGet(x => x.Length).Returns(length);
        return fileMock.Object;
    }

    [Fact(DisplayName = "Validate should succeed when the file is a valid content type and size")]
    public void Validate_ShouldSucceed_WhenFileIsValid()
    {
        // Arrange
        var request = new UploadSponsorLogoRequest { File = CreateFile() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Validate should succeed for every allowed content type")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_ShouldSucceed_ForEveryAllowedContentType(string contentType)
    {
        // Arrange
        var request = new UploadSponsorLogoRequest { File = CreateFile(contentType: contentType) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with FileRequired when File is null")]
    public void Validate_ShouldFailWithFileRequired_WhenFileIsNull()
    {
        // Arrange
#nullable disable
        var request = new UploadSponsorLogoRequest { File = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UploadSponsorLogoRequest.FileRequired");
    }

    [Fact(DisplayName = "Validate should not evaluate content type or size rules when File is null")]
    public void Validate_ShouldNotEvaluateContentTypeOrSizeRules_WhenFileIsNull()
    {
        // Arrange
#nullable disable
        var request = new UploadSponsorLogoRequest { File = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldNotContain(e => e.ErrorCode == FileUploadErrors.InvalidContentType.Code);
        result.Errors.ShouldNotContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
    }

    [Fact(DisplayName = "Validate should fail with InvalidContentType when the content type is not allowed")]
    public void Validate_ShouldFailWithInvalidContentType_WhenContentTypeIsNotAllowed()
    {
        // Arrange
        var request = new UploadSponsorLogoRequest { File = CreateFile(contentType: "application/pdf") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.InvalidContentType.Code);
    }

    [Fact(DisplayName = "Validate should fail with FileSizeExceedsLimit when the file exceeds 5 MB")]
    public void Validate_ShouldFailWithFileSizeExceedsLimit_WhenFileExceedsMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 5 * 1024 * 1024;
        var request = new UploadSponsorLogoRequest { File = CreateFile(length: maxSizeBytes + 1) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
    }

    [Fact(DisplayName = "Validate should succeed when the file is exactly at the 5 MB limit")]
    public void Validate_ShouldSucceed_WhenFileIsExactlyAtMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 5 * 1024 * 1024;
        var request = new UploadSponsorLogoRequest { File = CreateFile(length: maxSizeBytes) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with FileSizeExceedsLimit when the file length is zero")]
    public void Validate_ShouldFailWithFileSizeExceedsLimit_WhenFileLengthIsZero()
    {
        // Arrange
        var request = new UploadSponsorLogoRequest { File = CreateFile(length: 0) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
    }
}