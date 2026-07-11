using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class FileUploadValidationRulesTests
{
    [Fact(DisplayName = "HasAllowedContentType should succeed when content type is allowed")]
    public void HasAllowedContentType_ShouldSucceed_WhenContentTypeIsAllowed()
    {
        // Arrange
        const string contentType = "image/png";
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAllowedContentType should return validation error when content type is not allowed")]
    public void HasAllowedContentType_ShouldReturnValidationError_WhenContentTypeIsNotAllowed()
    {
        // Arrange
        const string contentType = "application/pdf";
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("FileUpload.InvalidContentType");
    }

    [Fact(DisplayName = "HasAllowedContentType should return validation error when content type is null")]
    public void HasAllowedContentType_ShouldReturnValidationError_WhenContentTypeIsNull()
    {
        // Arrange
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(null, allowedContentTypes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("FileUpload.InvalidContentType");
    }

    [Fact(DisplayName = "HasAllowedContentType should return validation error when allowed set is empty")]
    public void HasAllowedContentType_ShouldReturnValidationError_WhenAllowedSetIsEmpty()
    {
        // Arrange
        const string contentType = "image/png";
        var allowedContentTypes = new HashSet<string>();

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("FileUpload.InvalidContentType");
    }

    [Theory(DisplayName = "IsWithinSizeLimit should succeed when length is greater than zero and within the max size")]
    [InlineData(1, 100)]
    [InlineData(100, 100)]
    public void IsWithinSizeLimit_ShouldSucceed_WhenLengthIsGreaterThanZeroAndWithinMaxSize(long lengthInBytes, long maxSizeBytes)
    {
        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return validation error when length exceeds the max size")]
    public void IsWithinSizeLimit_ShouldReturnValidationError_WhenLengthExceedsMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = 101;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("FileUpload.FileSizeExceedsLimit");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata!["MaxSizeBytes"].ShouldBe(maxSizeBytes);
        result.FirstError.Metadata!["ActualSizeBytes"].ShouldBe(lengthInBytes);
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return validation error when length is zero")]
    public void IsWithinSizeLimit_ShouldReturnValidationError_WhenLengthIsZero()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = 0;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("FileUpload.FileSizeExceedsLimit");
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return validation error when length is negative")]
    public void IsWithinSizeLimit_ShouldReturnValidationError_WhenLengthIsNegative()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = -1;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("FileUpload.FileSizeExceedsLimit");
    }
}
