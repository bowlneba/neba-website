using Neba.Api.Features.News.EditArticle;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.EditArticle;

[UnitTest]
[Component("News")]
public sealed class EditArticleRequestValidatorTests
{
    private const string ValidId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    private readonly EditArticleRequestValidator _validator = new();

    private static EditArticleRequest CreateRequest(
        string? id = null,
        string? title = null,
        string? content = null,
        string? publicationStatus = null,
        DateTimeOffset? publishDate = null,
        string? tournamentId = null)
        => new()
        {
            Id = id ?? ValidId,
            Article = EditArticleInputFactory.Create(
                title: title,
                content: content,
                publicationStatus: publicationStatus is null ? null : Neba.Api.Features.News.Domain.PublicationStatus.FromName(publicationStatus),
                publishDate: publishDate,
                tournamentId: tournamentId)
        };

    [Fact(DisplayName = "Validate should succeed when all fields are valid")]
    public void Validate_ShouldSucceed_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = CreateRequest();

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
        var request = new EditArticleRequest { Id = null, Article = EditArticleInputFactory.Create() };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(EditArticleRequest.Id)
            && e.ErrorCode == "EditArticleRequest.IdRequired"
            && e.ErrorMessage == "Id is required.");
    }

    [Theory(DisplayName = "Validate should fail with IdInvalidLength when Id is not 26 characters")]
    [InlineData("too-short", TestDisplayName = "Too short")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA", TestDisplayName = "Too long")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsNot26Characters(string id)
    {
        // Arrange
        var request = CreateRequest(id: id);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(EditArticleRequest.Id)
            && e.ErrorCode == "EditArticleRequest.IdInvalidLength"
            && e.ErrorMessage == "Id must be a 26-character ULID.");
    }

    [Fact(DisplayName = "Validate should fail with TitleRequired when Title is empty")]
    public void Validate_ShouldFailWithTitleRequired_WhenTitleIsEmpty()
    {
        // Arrange
        var request = CreateRequest(title: string.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.TitleRequired"
            && e.ErrorMessage == "Title is required.");
    }

    [Fact(DisplayName = "Validate should fail with TitleTooLong when Title exceeds 256 characters")]
    public void Validate_ShouldFailWithTitleTooLong_WhenTitleExceeds256Characters()
    {
        // Arrange
        var request = CreateRequest(title: new string('a', 257));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.TitleTooLong"
            && e.ErrorMessage == "Title must be 256 characters or fewer.");
    }

    [Fact(DisplayName = "Validate should fail with ContentRequired when Content is empty")]
    public void Validate_ShouldFailWithContentRequired_WhenContentIsEmpty()
    {
        // Arrange
        var request = CreateRequest(content: string.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.ContentRequired"
            && e.ErrorMessage == "Content is required.");
    }

    [Fact(DisplayName = "Validate should fail with PublicationStatusRequired when PublicationStatus is empty")]
    public void Validate_ShouldFailWithPublicationStatusRequired_WhenPublicationStatusIsEmpty()
    {
        // Arrange
        var request = new EditArticleRequest
        {
            Id = ValidId,
            Article = EditArticleInputFactory.Create() with { PublicationStatus = string.Empty }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.PublicationStatusRequired"
            && e.ErrorMessage == "Publication status is required.");
    }

    [Fact(DisplayName = "Validate should fail with PublicationStatusInvalid when PublicationStatus is not a known value")]
    public void Validate_ShouldFailWithPublicationStatusInvalid_WhenPublicationStatusIsUnknown()
    {
        // Arrange
        var request = new EditArticleRequest
        {
            Id = ValidId,
            Article = EditArticleInputFactory.Create() with { PublicationStatus = "Archived" }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.PublicationStatusInvalid"
            && e.ErrorMessage == "Publication status must be one of: Draft, Published.");
    }

    [Fact(DisplayName = "Validate should fail with PublishDateRequired when PublishDate is default")]
    public void Validate_ShouldFailWithPublishDateRequired_WhenPublishDateIsDefault()
    {
        // Arrange
        var request = new EditArticleRequest
        {
            Id = ValidId,
            Article = EditArticleInputFactory.Create() with { PublishDate = default }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.PublishDateRequired"
            && e.ErrorMessage == "Publish date is required.");
    }

    [Theory(DisplayName = "Validate should fail with TournamentIdInvalidLength when TournamentId is supplied but not 26 characters")]
    [InlineData("too-short", TestDisplayName = "Too short")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA", TestDisplayName = "Too long")]
    public void Validate_ShouldFailWithTournamentIdInvalidLength_WhenTournamentIdIsSuppliedButNot26Characters(string tournamentId)
    {
        // Arrange
        var request = CreateRequest(tournamentId: tournamentId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "EditArticleRequest.TournamentIdInvalidLength"
            && e.ErrorMessage == "TournamentId must be a 26-character ULID.");
    }

    [Fact(DisplayName = "Validate should succeed when TournamentId is null")]
    public void Validate_ShouldSucceed_WhenTournamentIdIsNull()
    {
        // Arrange
        var request = CreateRequest(tournamentId: null);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
