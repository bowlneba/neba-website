using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.ListArticles;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.News.ListArticles;

[IntegrationTest]
[Component("News")]
[Collection<PostgreSqlFixture>]
public sealed class ListArticlesQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private static readonly DateTimeOffset Now = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private ListArticlesQueryHandler CreateHandler(IFileStorageService? fileStorageService = null)
    {
        var timeProvider = new FakeTimeProvider(Now);
        var storage = fileStorageService ?? new Mock<IFileStorageService>(MockBehavior.Loose).Object;
        return new ListArticlesQueryHandler(_dbContext, timeProvider, storage);
    }

    private static ListArticlesQuery DefaultQuery => new() { Page = 1, PageSize = 10 };

    [Fact(DisplayName = "HandleAsync returns empty collection when no articles exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoArticlesExist()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync excludes draft articles")]
    public async Task HandleAsync_ShouldExcludeDraftArticles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var draft = ArticleFactory.Create(
            slug: "draft-article",
            content: new string('x', 250),
            publicationStatus: PublicationStatus.Draft,
            publishDateUtc: Now.AddDays(-1));
        await _dbContext.Articles.AddAsync(draft, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync excludes published articles with a future publish date")]
    public async Task HandleAsync_ShouldExcludePublishedArticles_WithFuturePublishDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scheduled = ArticleFactory.Create(
            slug: "future-article",
            content: new string('x', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(1));
        await _dbContext.Articles.AddAsync(scheduled, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns published article with correct fields")]
    public async Task HandleAsync_ShouldReturnPublishedArticle_WithCorrectFields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var publishDate = Now.AddDays(-3);
        var content = new string('a', 220);
        var article = ArticleFactory.Create(
            title: "My Article",
            slug: "my-article",
            content: content,
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Slug.ShouldBe("my-article");
        dto.Title.ShouldBe("My Article");
        dto.Excerpt.ShouldBe($"{new string('a', 200)}...");
        dto.PublishDateUtc.ShouldBe(publishDate);
        dto.HeaderImageUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync sets HeaderImageUrl when article has a header image")]
    public async Task HandleAsync_ShouldSetHeaderImageUrl_WhenArticleHasHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "news-images", path: "articles/hero.jpg");
        var article = ArticleFactory.Create(
            slug: "image-article",
            content: new string('b', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1),
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
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().HeaderImageUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync leaves HeaderImageUrl null when article has no header image")]
    public async Task HandleAsync_ShouldLeaveHeaderImageUrlNull_WhenArticleHasNoHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(
            slug: "no-image-article",
            content: new string('c', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1),
            headerImage: null);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().HeaderImageUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync orders articles by publish date descending")]
    public async Task HandleAsync_ShouldOrderArticles_ByPublishDateDescending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var older = ArticleFactory.Create(
            slug: "older-article",
            content: new string('d', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-5));
        var newer = ArticleFactory.Create(
            slug: "newer-article",
            content: new string('e', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1));
        await _dbContext.Articles.AddRangeAsync([older, newer], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.Count.ShouldBe(2);
        var ordered = result.ToList();
        ordered[0].Slug.ShouldBe("newer-article");
        ordered[1].Slug.ShouldBe("older-article");
    }

    [Fact(DisplayName = "HandleAsync returns snapshot of published articles with header images")]
    public async Task HandleAsync_ShouldReturnSnapshot_OfPublishedArticlesWithHeaderImages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var article1 = ArticleFactory.Create(
            id: new ArticleId("01000000000000000000000001"),
            title: "Tournament Recap",
            slug: "tournament-recap",
            content: "The annual tournament concluded last weekend with exciting results across all divisions. " +
                     "Bowlers from across the region competed in a thrilling championship round that kept " +
                     "spectators on the edge of their seats throughout the entire competition.",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 5, 20, 9, 0, 0, TimeSpan.Zero),
            headerImage: StoredFileFactory.Create(container: "news", path: "articles/recap.jpg"));
        var article2 = ArticleFactory.Create(
            id: new ArticleId("01000000000000000000000002"),
            title: "Season Kickoff",
            slug: "season-kickoff",
            content: "The new bowling season is officially underway. Registration is now open for all members. " +
                     "Check the schedule and sign up before spots fill up for the upcoming league events " +
                     "and practice sessions planned throughout the season.",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: new DateTimeOffset(2025, 4, 10, 8, 0, 0, TimeSpan.Zero),
            headerImage: null);

        await _dbContext.Articles.AddRangeAsync([article1, article2], ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://cdn.example.com/news/articles/recap.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("news", "articles/recap.jpg"))
            .Returns(expectedUri);

        var handler = CreateHandler(fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        await Verify(result);
    }
}
