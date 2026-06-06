using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.News.Domain;

[UnitTest]
[Component("News")]
public sealed class ArticleTests
{
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