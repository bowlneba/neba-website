using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.GetArticle;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.News.GetArticle;

[IntegrationTest]
[Component("News")]
[Collection<PostgreSqlFixture>]
public sealed class GetArticleQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private GetArticleQueryHandler CreateHandler(IFileStorageService? fileStorageService = null)
    {
        var storage = fileStorageService ?? new Mock<IFileStorageService>(MockBehavior.Strict).Object;
        return new GetArticleQueryHandler(_dbContext, storage);
    }

    private static GetArticleQuery QueryFor(string slug) => new() { Slug = slug };

    [Fact(DisplayName = "HandleAsync returns NotFound error when article does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenArticleDoesNotExist()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(QueryFor("does-not-exist"), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Article.NotFound");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata["Slug"].ShouldBe("does-not-exist");
    }

    [Fact(DisplayName = "HandleAsync returns article with correct fields when found")]
    public async Task HandleAsync_ShouldReturnArticleWithCorrectFields_WhenFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var publishDate = new DateTimeOffset(2025, 5, 10, 9, 0, 0, TimeSpan.Zero);
        var article = ArticleFactory.Create(
            title: "My Article",
            slug: "my-article",
            content: "Full article content here.",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(QueryFor("my-article"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var dto = result.Value;
        dto.Slug.ShouldBe("my-article");
        dto.Title.ShouldBe("My Article");
        dto.Content.ShouldBe("Full article content here.");
        dto.PublishDateUtc.ShouldBe(publishDate);
        dto.HeaderImageUrl.ShouldBeNull();
        dto.Attachments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync sets HeaderImageUrl when article has a header image")]
    public async Task HandleAsync_ShouldSetHeaderImageUrl_WhenArticleHasHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "news-images", path: "articles/hero.jpg");
        var article = ArticleFactory.Create(
            slug: "image-article",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero),
            headerImage: headerImage);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/news-images/articles/hero.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("news-images", "articles/hero.jpg"))
            .Returns(expectedUri);

        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("image-article"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.HeaderImageUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync leaves HeaderImageUrl null when article has no header image")]
    public async Task HandleAsync_ShouldLeaveHeaderImageUrlNull_WhenArticleHasNoHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(
            slug: "no-image-article",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero),
            headerImage: null);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("no-image-article"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.HeaderImageUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync sets Url on each attachment")]
    public async Task HandleAsync_ShouldSetUrl_OnEachAttachment()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file1 = StoredFileFactory.Create(container: "news-files", path: "docs/schedule.pdf");
        var file2 = StoredFileFactory.Create(container: "news-files", path: "docs/results.pdf");
        var attachments = new[]
        {
            ArticleAttachmentFactory.Create(displayName: "Schedule", file: file1),
            ArticleAttachmentFactory.Create(displayName: "Results", file: file2)
        };
        var article = ArticleFactory.Create(
            slug: "attachment-article",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero),
            attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var uri1 = new Uri("https://storage.example.com/news-files/docs/schedule.pdf");
        var uri2 = new Uri("https://storage.example.com/news-files/docs/results.pdf");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock.Setup(s => s.GetBlobUri("news-files", "docs/schedule.pdf")).Returns(uri1);
        fileStorageMock.Setup(s => s.GetBlobUri("news-files", "docs/results.pdf")).Returns(uri2);

        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("attachment-article"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Attachments.Count.ShouldBe(2);
        result.Value.Attachments.ShouldContain(a => a.DisplayName == "Schedule" && a.Url == uri1);
        result.Value.Attachments.ShouldContain(a => a.DisplayName == "Results" && a.Url == uri2);
    }

    [Fact(DisplayName = "HandleAsync excludes inline attachments from the attachment list")]
    public async Task HandleAsync_ShouldExcludeInlineAttachments_FromAttachmentList()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var inlineFile = StoredFileFactory.Create(container: "news-files", path: "docs/inline-image.jpg");
        var downloadFile = StoredFileFactory.Create(container: "news-files", path: "docs/schedule.pdf");
        var attachments = new[]
        {
            ArticleAttachmentFactory.Create(displayName: "Inline Image", file: inlineFile, isInline: true),
            ArticleAttachmentFactory.Create(displayName: "Schedule", file: downloadFile, isInline: false)
        };
        var article = ArticleFactory.Create(
            slug: "mixed-attachments",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero),
            attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var scheduleUri = new Uri("https://storage.example.com/news-files/docs/schedule.pdf");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock.Setup(s => s.GetBlobUri("news-files", "docs/schedule.pdf")).Returns(scheduleUri);

        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("mixed-attachments"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Attachments.Count.ShouldBe(1);
        result.Value.Attachments.Single().DisplayName.ShouldBe("Schedule");
        result.Value.Attachments.Single().Url.ShouldBe(scheduleUri);
    }

    [Fact(DisplayName = "HandleAsync returns article regardless of publication status")]
    public async Task HandleAsync_ShouldReturnArticle_RegardlessOfPublicationStatus()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var draft = ArticleFactory.Create(
            slug: "draft-article",
            publicationStatus: PublicationStatus.Draft,
            publishDateUtc: new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero));
        await _dbContext.Articles.AddAsync(draft, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(QueryFor("draft-article"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("draft-article");
    }

    [Fact(DisplayName = "HandleAsync returns snapshot of article with header image and attachments")]
    public async Task HandleAsync_ShouldReturnSnapshot_OfArticleWithHeaderImageAndAttachments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "news", path: "articles/recap.jpg");
        var file1 = StoredFileFactory.Create(container: "news-files", path: "docs/bracket.pdf");
        var file2 = StoredFileFactory.Create(container: "news-files", path: "docs/scores.pdf");
        var attachments = new[]
        {
            ArticleAttachmentFactory.Create(displayName: "Bracket", file: file1),
            ArticleAttachmentFactory.Create(displayName: "Scores", file: file2)
        };
        var article = ArticleFactory.Create(
            id: new ArticleId("01000000000000000000000001"),
            title: "Tournament Recap",
            slug: "tournament-recap",
            content: "The annual tournament concluded last weekend with exciting results across all divisions.",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 20, 9, 0, 0, TimeSpan.Zero),
            headerImage: headerImage,
            attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var headerUri = new Uri("https://cdn.example.com/news/articles/recap.jpg");
        var bracketUri = new Uri("https://cdn.example.com/news-files/docs/bracket.pdf");
        var scoresUri = new Uri("https://cdn.example.com/news-files/docs/scores.pdf");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock.Setup(s => s.GetBlobUri("news", "articles/recap.jpg")).Returns(headerUri);
        fileStorageMock.Setup(s => s.GetBlobUri("news-files", "docs/bracket.pdf")).Returns(bracketUri);
        fileStorageMock.Setup(s => s.GetBlobUri("news-files", "docs/scores.pdf")).Returns(scoresUri);

        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(QueryFor("tournament-recap"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        await Verify(result.Value);
    }
}