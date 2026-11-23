using Microsoft.AspNetCore.Http;

using Neba.Api.Features.Series.UploadSeriesLogo;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.UploadSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class UploadSeriesLogoRequestValidatorTests
{
    private readonly UploadSeriesLogoRequestValidator _validator = new();

    private static IFormFile CreateFile(string contentType = "image/png", long length = 1024)
    {
        var file = new Mock<IFormFile>(MockBehavior.Strict);
        file.SetupGet(x => x.ContentType).Returns(contentType);
        file.SetupGet(x => x.Length).Returns(length);
        return file.Object;
    }

    [Theory(DisplayName = "Validate should succeed for every allowed content type")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_ShouldSucceed_ForAllowedContentTypes(string contentType)
    {
        // Arrange & Act
        var result = _validator.Validate(new UploadSeriesLogoRequest { File = CreateFile(contentType) });

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail for a content type that is not an allowed image")]
    public void Validate_ShouldFail_ForDisallowedContentType()
    {
        // Arrange & Act
        var result = _validator.Validate(new UploadSeriesLogoRequest { File = CreateFile("application/pdf") });

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.InvalidContentType.Code);
    }

    [Theory(DisplayName = "Validate should accept exactly 5 MB and reject one byte more")]
    [InlineData(5L * 1024 * 1024, true)]
    [InlineData((5L * 1024 * 1024) + 1, false)]
    public void Validate_ShouldEnforceSizeLimit(long length, bool valid)
    {
        // Arrange & Act
        var result = _validator.Validate(new UploadSeriesLogoRequest { File = CreateFile(length: length) });

        // Assert
        result.IsValid.ShouldBe(valid);
        if (!valid)
        {
            result.Errors.ShouldContain(e => e.ErrorCode == FileUploadErrors.FileSizeExceedsLimit.Code);
        }
    }
}