using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class FileUploadValidationRulesTests
{
    [Fact(DisplayName = "HasAllowedContentType should return true when content type is allowed")]
    public void HasAllowedContentType_ShouldReturnTrue_WhenContentTypeIsAllowed()
    {
        // Arrange
        const string contentType = "image/png";
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAllowedContentType should return false when content type is not allowed")]
    public void HasAllowedContentType_ShouldReturnFalse_WhenContentTypeIsNotAllowed()
    {
        // Arrange
        const string contentType = "application/pdf";
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAllowedContentType should return false when content type is null")]
    public void HasAllowedContentType_ShouldReturnFalse_WhenContentTypeIsNull()
    {
        // Arrange
        var allowedContentTypes = new HashSet<string> { "image/png", "image/jpeg" };

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(null, allowedContentTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAllowedContentType should return false when allowed set is empty")]
    public void HasAllowedContentType_ShouldReturnFalse_WhenAllowedSetIsEmpty()
    {
        // Arrange
        const string contentType = "image/png";
        var allowedContentTypes = new HashSet<string>();

        // Act
        var result = FileUploadValidationRules.HasAllowedContentType(contentType, allowedContentTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory(DisplayName = "IsWithinSizeLimit should return true when length is greater than zero and within the max size")]
    [InlineData(1, 100)]
    [InlineData(100, 100)]
    public void IsWithinSizeLimit_ShouldReturnTrue_WhenLengthIsGreaterThanZeroAndWithinMaxSize(long lengthInBytes, long maxSizeBytes)
    {
        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return false when length exceeds the max size")]
    public void IsWithinSizeLimit_ShouldReturnFalse_WhenLengthExceedsMaxSize()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = 101;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return false when length is zero")]
    public void IsWithinSizeLimit_ShouldReturnFalse_WhenLengthIsZero()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = 0;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsWithinSizeLimit should return false when length is negative")]
    public void IsWithinSizeLimit_ShouldReturnFalse_WhenLengthIsNegative()
    {
        // Arrange
        const long maxSizeBytes = 100;
        const long lengthInBytes = -1;

        // Act
        var result = FileUploadValidationRules.IsWithinSizeLimit(lengthInBytes, maxSizeBytes);

        // Assert
        result.ShouldBeFalse();
    }
}
