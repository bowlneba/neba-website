using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.News.Domain;

[UnitTest]
[Component("News")]
public sealed class ArticleTests
{
    [Fact(DisplayName = "Create returns Success when all inputs are valid")]
    public void Create_ShouldReturnSuccess_WhenInputsAreValid()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create assigns a non-default Id")]
    public void Create_ShouldAssignNonDefaultId()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create sets Title, Content, PublicationStatus, PublishDateUtc, and TournamentId")]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var tournamentId = TournamentId.New();

        // Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            PublicationStatus.Published,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId,
            headerImage: null);

        // Assert
        var article = result.Value;
        article.Title.ShouldBe(ArticleFactory.ValidTitle);
        article.Content.ShouldBe(ArticleFactory.ValidContent);
        article.PublicationStatus.ShouldBe(PublicationStatus.Published);
        article.PublishDateUtc.ShouldBe(ArticleFactory.ValidPublishDateUtc);
        article.TournamentId.ShouldBe(tournamentId);
    }

    [Fact(DisplayName = "Create sets HeaderImage when one is provided")]
    public void Create_ShouldSetHeaderImage_WhenProvided()
    {
        // Arrange
        var headerImage = StoredFileFactory.Create();

        // Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: headerImage);

        // Assert
        result.Value.HeaderImage.ShouldBe(headerImage);
    }

    [Fact(DisplayName = "Create sets HeaderImage to null when no header image is provided")]
    public void Create_ShouldSetHeaderImageToNull_WhenNotProvided()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.HeaderImage.ShouldBeNull();
    }

    [Fact(DisplayName = "Create sets TournamentId to null when no tournament is provided")]
    public void Create_ShouldSetTournamentIdToNull_WhenNotProvided()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.TournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns an article with no attachments")]
    public void Create_ShouldReturnArticleWithNoAttachments()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.Attachments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Create uses the provided slug when one is supplied")]
    public void Create_ShouldUseProvidedSlug_WhenSupplied()
    {
        // Arrange & Act
        var result = Article.Create(
            "Some Title",
            "custom-slug",
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.Slug.ShouldBe("custom-slug");
    }

    [Theory(DisplayName = "Create generates the slug from the title when slug is null or empty")]
    [InlineData(null)]
    [InlineData("")]
    public void Create_ShouldGenerateSlugFromTitle_WhenSlugIsNullOrEmpty(string? slug)
    {
        // Arrange & Act
        var result = Article.Create(
            "My Great Title",
            slug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.Slug.ShouldBe("my-great-title");
    }

    [Fact(DisplayName = "Create returns Article.Slug.Invalid when slug is whitespace (does not fall back to title)")]
    public void Create_ShouldReturnSlugInvalidError_WhenSlugIsWhitespace()
    {
        // Arrange & Act
        var result = Article.Create(
            "My Great Title",
            "   ",
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Slug.Invalid");
    }

    [Theory(DisplayName = "Create normalizes the slug to lowercase, hyphen-separated, alphanumeric text")]
    [InlineData("Hello World!", "hello-world")]
    [InlineData("  Leading And Trailing  ", "leading-and-trailing")]
    [InlineData("Multiple---Hyphens", "multiple-hyphens")]
    [InlineData("Special_Ch@racters#123", "special-ch-racters-123")]
    [InlineData("UPPERCASE", "uppercase")]
    public void Create_ShouldNormalizeSlug(string rawSlug, string expectedSlug)
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            rawSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.Value.Slug.ShouldBe(expectedSlug);
    }

    [Fact(DisplayName = "Create returns Article.Slug.Invalid when the normalized slug has no alphanumeric characters")]
    public void Create_ShouldReturnSlugInvalidError_WhenNormalizedSlugIsEmpty()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            "!!!---!!!",
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Slug.Invalid");
    }

    [Theory(DisplayName = "Create returns Article.Slug.Reserved when the normalized slug is 'new'")]
    [InlineData("new")]
    [InlineData("New")]
    [InlineData("NEW")]
    [InlineData("  new  ")]
    public void Create_ShouldReturnSlugReservedError_WhenSlugIsNew(string slug)
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            slug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Slug.Reserved");
    }

    [Fact(DisplayName = "Create returns Article.Slug.Reserved when the title normalizes to 'new' and slug is not supplied")]
    public void Create_ShouldReturnSlugReservedError_WhenTitleNormalizesToNew()
    {
        // Arrange & Act
        var result = Article.Create(
            "New",
            slug: null,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Slug.Reserved");
    }

    [Theory(DisplayName = "Create returns Article.Title.Required when title is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnTitleRequiredError_WhenTitleIsEmptyOrWhitespace(string title)
    {
        // Arrange & Act
        var result = Article.Create(
            title,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Title.Required");
    }

#nullable disable
    [Fact(DisplayName = "Create returns Article.Title.Required when title is null")]
    public void Create_ShouldReturnTitleRequiredError_WhenTitleIsNull()
    {
        // Arrange & Act
        var result = Article.Create(
            null,
            ArticleFactory.ValidSlug,
            ArticleFactory.ValidContent,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Title.Required");
    }
#nullable enable

    [Theory(DisplayName = "Create returns Article.Content.Required when content is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnContentRequiredError_WhenContentIsEmptyOrWhitespace(string content)
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            content,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Content.Required");
    }

#nullable disable
    [Fact(DisplayName = "Create returns Article.Content.Required when content is null")]
    public void Create_ShouldReturnContentRequiredError_WhenContentIsNull()
    {
        // Arrange & Act
        var result = Article.Create(
            ArticleFactory.ValidTitle,
            ArticleFactory.ValidSlug,
            null,
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Content.Required");
    }
#nullable enable

    [Fact(DisplayName = "AddAttachment returns Success when display name and file are valid")]
    public void AddAttachment_ShouldReturnSuccess_WhenInputsAreValid()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddAttachment adds the attachment to the Attachments collection")]
    public void AddAttachment_ShouldAddAttachmentToCollection()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);

        // Assert
        article.Attachments.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddAttachment sets correct DisplayName, File, and IsInline on the attachment")]
    public void AddAttachment_ShouldSetCorrectProperties()
    {
        // Arrange
        var article = ArticleFactory.Create();
        var file = StoredFileFactory.Create();

        // Act
        article.AddAttachment("My Attachment", file, isInline: true);

        // Assert
        var attachment = article.Attachments.Single();
        attachment.DisplayName.ShouldBe("My Attachment");
        attachment.File.ShouldBe(file);
        attachment.IsInline.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddAttachment assigns a non-default Id to the attachment")]
    public void AddAttachment_ShouldAssignNonDefaultId()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);

        // Assert
        article.Attachments.Single().Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "AddAttachment returns ArticleAttachment.DisplayName validation error when display name is empty")]
    public void AddAttachment_ShouldReturnValidationError_WhenDisplayNameIsEmpty()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.AddAttachment(string.Empty, StoredFileFactory.Create(), isInline: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("ArticleAttachment.DisplayName");
    }

    [Fact(DisplayName = "AddAttachment returns ArticleAttachment.DisplayName validation error when display name is whitespace")]
    public void AddAttachment_ShouldReturnValidationError_WhenDisplayNameIsWhitespace()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.AddAttachment("   ", StoredFileFactory.Create(), isInline: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("ArticleAttachment.DisplayName");
    }

#nullable disable
    [Fact(DisplayName = "AddAttachment returns ArticleAttachment.DisplayName validation error when display name is null")]
    public void AddAttachment_ShouldReturnValidationError_WhenDisplayNameIsNull()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.AddAttachment(null, StoredFileFactory.Create(), isInline: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("ArticleAttachment.DisplayName");
    }
#nullable enable

    [Fact(DisplayName = "AddAttachment does not add to Attachments when display name is invalid")]
    public void AddAttachment_ShouldNotAddToCollection_WhenDisplayNameIsInvalid()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        article.AddAttachment(string.Empty, StoredFileFactory.Create(), isInline: false);

        // Assert
        article.Attachments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddAttachment supports adding multiple attachments")]
    public void AddAttachment_ShouldSupportMultipleAttachments()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        article.AddAttachment("First", StoredFileFactory.Create(), isInline: true);
        article.AddAttachment("Second", StoredFileFactory.Create(), isInline: false);

        // Assert
        article.Attachments.Count.ShouldBe(2);
    }
}