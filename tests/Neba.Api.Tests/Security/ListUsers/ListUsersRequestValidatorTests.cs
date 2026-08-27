using Neba.Api.Security.ListUsers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.ListUsers;

[UnitTest]
[Component("Security")]
public sealed class ListUsersRequestValidatorTests
{
    private readonly ListUsersRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when page and page size are within range")]
    public void Validate_ShouldSucceed_WhenPageAndPageSizeAreWithinRange()
    {
        // Arrange
        var request = new ListUsersRequest { Page = 1, PageSize = 20 };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Validate should fail with PageInvalid when page is less than 1")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    public void Validate_ShouldFailWithPageInvalid_WhenPageIsLessThanOne(int page)
    {
        // Arrange
        var request = new ListUsersRequest { Page = page, PageSize = 20 };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ListUsersRequest.Page)
            && e.ErrorCode == "ListUsersRequest.PageInvalid"
            && e.ErrorMessage == "Page must be greater than or equal to 1.");
    }

    [Theory(DisplayName = "Validate should fail with PageSizeInvalid when page size is outside 1-100")]
    [InlineData(0, TestDisplayName = "Zero")]
    [InlineData(-1, TestDisplayName = "Negative")]
    [InlineData(101, TestDisplayName = "Above maximum")]
    public void Validate_ShouldFailWithPageSizeInvalid_WhenPageSizeIsOutOfRange(int pageSize)
    {
        // Arrange
        var request = new ListUsersRequest { Page = 1, PageSize = pageSize };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ListUsersRequest.PageSize)
            && e.ErrorCode == "ListUsersRequest.PageSizeInvalid"
            && e.ErrorMessage == "Page size must be between 1 and 100.");
    }

    [Theory(DisplayName = "Validate should succeed at the page size boundaries")]
    [InlineData(1, TestDisplayName = "Minimum")]
    [InlineData(100, TestDisplayName = "Maximum")]
    public void Validate_ShouldSucceed_AtPageSizeBoundaries(int pageSize)
    {
        // Arrange
        var request = new ListUsersRequest { Page = 1, PageSize = pageSize };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
