using Neba.Api.Features.News.DeleteArticle;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleRequestValidatorTests
{
    private readonly DeleteArticleRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when Id is a valid 26-character ULID")]
    public void Validate_ShouldSucceed_WhenIdIsValidUlid()
    {
        // Arrange
        var request = new DeleteArticleRequest { Id = "01ARZ3NDEKTSV4RRFFQ69G5FAV" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired when Id is null")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsNull()
    {
        // Arrange
#nullable disable
        var request = new DeleteArticleRequest { Id = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeleteArticleRequest.Id)
            && e.ErrorCode == "DeleteArticleRequest.IdRequired"
            && e.ErrorMessage == "Id is required.");
    }

    [Theory(DisplayName = "Validate should fail with IdRequired when Id is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        var request = new DeleteArticleRequest { Id = id };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeleteArticleRequest.Id)
            && e.ErrorCode == "DeleteArticleRequest.IdRequired"
            && e.ErrorMessage == "Id is required.");
    }

    [Theory(DisplayName = "Validate should fail with IdInvalidLength when Id is not 26 characters")]
    [InlineData("too-short", TestDisplayName = "Too short")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA", TestDisplayName = "Too long")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsNot26Characters(string id)
    {
        // Arrange
        var request = new DeleteArticleRequest { Id = id };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeleteArticleRequest.Id)
            && e.ErrorCode == "DeleteArticleRequest.IdInvalidLength"
            && e.ErrorMessage == "Id must be a 26-character ULID.");
    }
}