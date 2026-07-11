using Microsoft.AspNetCore.Http;

using Neba.Api.Contracts.News.UploadArticleAttachment;
using Neba.Api.Features.News.UploadArticleAttachment;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.UploadArticleAttachment;

[UnitTest]
[Component("News")]
public sealed class UploadArticleAttachmentRequestValidatorTests
{
    private readonly UploadArticleAttachmentRequestValidator _validator = new();

    private static IFormFile CreateFile(string contentType = "application/pdf", long length = 1024)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "File", "attachment.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact(DisplayName = "Validate should succeed when the file is a valid content type and size")]
    public void Validate_ShouldSucceed_WhenFileIsValid()
    {
        // Arrange
        var request = new UploadArticleAttachmentRequest { File = CreateFile() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Validate should succeed for every allowed content type")]
    [InlineData("application/pdf")]
    [InlineData("application/msword")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_ShouldSucceed_ForEveryAllowedContentType(string contentType)
    {
        // Arrange
        var request = new UploadArticleAttachmentRequest { File = CreateFile(contentType: contentType) };

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
        var request = new UploadArticleAttachmentRequest { File = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UploadArticleAttachment.FileRequired");
    }

    [Fact(DisplayName = "Validate should not evaluate content type or size rules when File is null")]
    public void Validate_ShouldNotEvaluateContentTypeOrSizeRules_WhenFileIsNull()
    {
        // Arrange
#nullable disable
        var request = new UploadArticleAttachmentRequest { File = null };
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
        var request = new UploadArticleAttachmentRequest { File = CreateFile(contentType: "text/plain") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.InvalidContentType.Code);
    }

    [Fact(DisplayName = "Validate should fail with FileSizeExceedsLimit when the file exceeds 25 MB")]
    public void Validate_ShouldFailWithFileSizeExceedsLimit_WhenFileExceedsMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 25 * 1024 * 1024;
        var request = new UploadArticleAttachmentRequest { File = CreateFile(length: maxSizeBytes + 1) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
    }

    [Fact(DisplayName = "Validate should succeed when the file is exactly at the 25 MB limit")]
    public void Validate_ShouldSucceed_WhenFileIsExactlyAtMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 25 * 1024 * 1024;
        var request = new UploadArticleAttachmentRequest { File = CreateFile(length: maxSizeBytes) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with FileSizeExceedsLimit when the file length is zero")]
    public void Validate_ShouldFailWithFileSizeExceedsLimit_WhenFileLengthIsZero()
    {
        // Arrange
        var request = new UploadArticleAttachmentRequest { File = CreateFile(length: 0) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
    }
}