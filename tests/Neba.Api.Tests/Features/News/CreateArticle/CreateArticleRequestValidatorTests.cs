using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.CreateArticle;

[UnitTest]
[Component("News")]
public sealed class CreateArticleRequestValidatorTests
{
    private readonly CreateArticleRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when all fields are valid")]
    public void Validate_ShouldSucceed_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with TitleRequired when Title is empty")]
    public void Validate_ShouldFailWithTitleRequired_WhenTitleIsEmpty()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(title: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.TitleRequired");
    }

    [Fact(DisplayName = "Validate should fail with TitleTooLong when Title exceeds 256 characters")]
    public void Validate_ShouldFailWithTitleTooLong_WhenTitleExceeds256Characters()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(title: new string('a', 257)) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.TitleTooLong");
    }

    [Fact(DisplayName = "Validate should fail with SlugTooLong when a supplied Slug exceeds 256 characters")]
    public void Validate_ShouldFailWithSlugTooLong_WhenSlugExceeds256Characters()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(slug: new string('a', 257)) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.SlugTooLong");
    }

    [Fact(DisplayName = "Validate should succeed when Slug is null")]
    public void Validate_ShouldSucceed_WhenSlugIsNull()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(slug: null) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with ContentRequired when Content is empty")]
    public void Validate_ShouldFailWithContentRequired_WhenContentIsEmpty()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(content: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.ContentRequired");
    }

    [Fact(DisplayName = "Validate should fail with PublicationStatusRequired when PublicationStatus is empty")]
    public void Validate_ShouldFailWithPublicationStatusRequired_WhenPublicationStatusIsEmpty()
    {
        // Arrange
        var input = ArticleInputFactory.Create() with { PublicationStatus = string.Empty };
        var request = new CreateArticleRequest { Article = input };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.PublicationStatusRequired");
    }

    [Fact(DisplayName = "Validate should fail with PublicationStatusInvalid when PublicationStatus is not a known value")]
    public void Validate_ShouldFailWithPublicationStatusInvalid_WhenPublicationStatusIsUnknown()
    {
        // Arrange
        var input = ArticleInputFactory.Create() with { PublicationStatus = "Archived" };
        var request = new CreateArticleRequest { Article = input };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.PublicationStatusInvalid");
    }

    [Fact(DisplayName = "Validate should fail with PublishDateRequired when PublishDate is default")]
    public void Validate_ShouldFailWithPublishDateRequired_WhenPublishDateIsDefault()
    {
        // Arrange
        var input = ArticleInputFactory.Create() with { PublishDate = default };
        var request = new CreateArticleRequest { Article = input };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.PublishDateRequired");
    }

    [Fact(DisplayName = "Validate should succeed when TournamentId is null")]
    public void Validate_ShouldSucceed_WhenTournamentIdIsNull()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(tournamentId: null) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with TournamentIdInvalidLength when TournamentId is not 26 characters")]
    [InlineData("too-short", TestDisplayName = "Too short")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA", TestDisplayName = "Too long")]
    public void Validate_ShouldFailWithTournamentIdInvalidLength_WhenTournamentIdIsNot26Characters(string tournamentId)
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(tournamentId: tournamentId) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateArticleRequest.TournamentIdInvalidLength");
    }

    [Fact(DisplayName = "Validate should succeed when TournamentId is a valid 26-character ULID")]
    public void Validate_ShouldSucceed_WhenTournamentIdIsValidUlid()
    {
        // Arrange
        var request = new CreateArticleRequest { Article = ArticleInputFactory.Create(tournamentId: "01ARZ3NDEKTSV4RRFFQ69G5FAV") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}