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

    [Fact(DisplayName = "Update returns Success when all inputs are valid")]
    public void Update_ShouldReturnSuccess_WhenInputsAreValid()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.Update(
            "Updated Title",
            "Updated Content",
            PublicationStatus.Published,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Update sets Title, Content, PublicationStatus, PublishDateUtc, and TournamentId")]
    public void Update_ShouldSetProperties()
    {
        // Arrange
        var article = ArticleFactory.Create();
        var tournamentId = TournamentId.New();
        var publishDateUtc = ArticleFactory.ValidPublishDateUtc.AddDays(1);

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            PublicationStatus.Published,
            publishDateUtc,
            tournamentId,
            headerImage: null);

        // Assert
        article.Title.ShouldBe("Updated Title");
        article.Content.ShouldBe("Updated Content");
        article.PublicationStatus.ShouldBe(PublicationStatus.Published);
        article.PublishDateUtc.ShouldBe(publishDateUtc);
        article.TournamentId.ShouldBe(tournamentId);
    }

    [Fact(DisplayName = "Update sets HeaderImage when one is provided")]
    public void Update_ShouldSetHeaderImage_WhenProvided()
    {
        // Arrange
        var article = ArticleFactory.Create();
        var headerImage = StoredFileFactory.Create();

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage);

        // Assert
        article.HeaderImage.ShouldBe(headerImage);
    }

    [Fact(DisplayName = "Update clears HeaderImage when null is provided")]
    public void Update_ShouldClearHeaderImage_WhenNullProvided()
    {
        // Arrange
        var article = ArticleFactory.Create(headerImage: StoredFileFactory.Create());

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        article.HeaderImage.ShouldBeNull();
    }

    [Fact(DisplayName = "Update clears TournamentId when null is provided")]
    public void Update_ShouldClearTournamentId_WhenNullProvided()
    {
        // Arrange
        var article = ArticleFactory.Create(tournamentId: TournamentId.New());

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        article.TournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "Update does not change the Slug")]
    public void Update_ShouldNotChangeSlug()
    {
        // Arrange
        var article = ArticleFactory.Create(slug: "original-slug");

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        article.Slug.ShouldBe("original-slug");
    }

    [Fact(DisplayName = "Update does not change the Id")]
    public void Update_ShouldNotChangeId()
    {
        // Arrange
        var article = ArticleFactory.Create();
        var originalId = article.Id;

        // Act
        article.Update(
            "Updated Title",
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        article.Id.ShouldBe(originalId);
    }

    [Theory(DisplayName = "Update returns Article.Title.Required when title is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_ShouldReturnTitleRequiredError_WhenTitleIsEmptyOrWhitespace(string title)
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.Update(
            title,
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
    [Fact(DisplayName = "Update returns Article.Title.Required when title is null")]
    public void Update_ShouldReturnTitleRequiredError_WhenTitleIsNull()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.Update(
            null,
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

    [Theory(DisplayName = "Update returns Article.Content.Required when content is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_ShouldReturnContentRequiredError_WhenContentIsEmptyOrWhitespace(string content)
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.Update(
            ArticleFactory.ValidTitle,
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
    [Fact(DisplayName = "Update returns Article.Content.Required when content is null")]
    public void Update_ShouldReturnContentRequiredError_WhenContentIsNull()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.Update(
            ArticleFactory.ValidTitle,
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

    [Fact(DisplayName = "Update does not change Title or Content when title is invalid")]
    public void Update_ShouldNotChangeProperties_WhenTitleIsInvalid()
    {
        // Arrange
        var article = ArticleFactory.Create(title: "Original Title", content: "Original Content");

        // Act
        article.Update(
            string.Empty,
            "Updated Content",
            ArticleFactory.ValidPublicationStatus,
            ArticleFactory.ValidPublishDateUtc,
            tournamentId: null,
            headerImage: null);

        // Assert
        article.Title.ShouldBe("Original Title");
        article.Content.ShouldBe("Original Content");
    }

    [Fact(DisplayName = "RemoveAttachment returns Success when the attachment exists")]
    public void RemoveAttachment_ShouldReturnSuccess_WhenAttachmentExists()
    {
        // Arrange
        var article = ArticleFactory.Create();
        article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);
        var attachmentId = article.Attachments.Single().Id;

        // Act
        var result = article.RemoveAttachment(attachmentId);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "RemoveAttachment removes the attachment from the Attachments collection")]
    public void RemoveAttachment_ShouldRemoveAttachmentFromCollection()
    {
        // Arrange
        var article = ArticleFactory.Create();
        article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);
        var attachmentId = article.Attachments.Single().Id;

        // Act
        article.RemoveAttachment(attachmentId);

        // Assert
        article.Attachments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "RemoveAttachment only removes the matching attachment")]
    public void RemoveAttachment_ShouldOnlyRemoveMatchingAttachment()
    {
        // Arrange
        var article = ArticleFactory.Create();
        article.AddAttachment("First", StoredFileFactory.Create(), isInline: true);
        article.AddAttachment("Second", StoredFileFactory.Create(), isInline: false);
        var attachmentIdToRemove = article.Attachments[0].Id;

        // Act
        article.RemoveAttachment(attachmentIdToRemove);

        // Assert
        var remaining = article.Attachments.Single();
        remaining.DisplayName.ShouldBe("Second");
    }

    [Fact(DisplayName = "RemoveAttachment returns Article.Attachment.NotFound when no attachment matches")]
    public void RemoveAttachment_ShouldReturnNotFoundError_WhenAttachmentDoesNotExist()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        var result = article.RemoveAttachment(ArticleAttachmentId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Attachment.NotFound");
    }

    [Fact(DisplayName = "RemoveAttachment does not change the Attachments collection when no attachment matches")]
    public void RemoveAttachment_ShouldNotChangeCollection_WhenAttachmentDoesNotExist()
    {
        // Arrange
        var article = ArticleFactory.Create();
        article.AddAttachment("My Attachment", StoredFileFactory.Create(), isInline: false);

        // Act
        article.RemoveAttachment(ArticleAttachmentId.New());

        // Assert
        article.Attachments.Count.ShouldBe(1);
    }
}