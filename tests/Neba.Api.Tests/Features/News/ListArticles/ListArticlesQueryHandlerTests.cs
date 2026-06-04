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
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
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
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
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
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
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
        result.Items.ShouldHaveSingleItem();
        result.TotalItems.ShouldBe(1);
        var dto = result.Items.Single();
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
        result.Items.ShouldHaveSingleItem();
        result.Items.Single().HeaderImageUrl.ShouldBe(expectedUri);
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
        result.Items.ShouldHaveSingleItem();
        result.Items.Single().HeaderImageUrl.ShouldBeNull();
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
        result.Items.Count.ShouldBe(2);
        var ordered = result.Items.ToList();
        ordered[0].Slug.ShouldBe("newer-article");
        ordered[1].Slug.ShouldBe("older-article");
    }

    [Fact(DisplayName = "HandleAsync TotalItems is consistent regardless of page size")]
    public async Task HandleAsync_TotalItems_ShouldBeConsistent_RegardlessOfPageSize()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var articles = Enumerable.Range(1, 20)
            .Select(i => ArticleFactory.Create(
                slug: $"article-{i:D3}",
                content: new string('x', 250),
                publicationStatus: PublicationStatus.Published,
                publishDateUtc: Now.AddDays(-i)));
        await _dbContext.Articles.AddRangeAsync(articles, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var paged = await handler.HandleAsync(new ListArticlesQuery { Page = 1, PageSize = 5 }, ct);
        var all = await handler.HandleAsync(new ListArticlesQuery { Page = 1, PageSize = 500 }, ct);

        // Assert — TotalItems must be identical regardless of page size; fetching all items at once must equal TotalItems
        paged.TotalItems.ShouldBe(all.TotalItems);
        paged.Items.Count.ShouldBeLessThanOrEqualTo(5);
        all.Items.Count.ShouldBe(all.TotalItems);
    }

    [Fact(DisplayName = "HandleAsync returns non-overlapping pages with consistent TotalItems")]
    public async Task HandleAsync_ShouldReturnNonOverlappingPages_WithConsistentTotalItems()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var articles = Enumerable.Range(1, 20)
            .Select(i => ArticleFactory.Create(
                slug: $"article-{i:D3}",
                content: new string('x', 250),
                publicationStatus: PublicationStatus.Published,
                publishDateUtc: Now.AddDays(-i)));
        await _dbContext.Articles.AddRangeAsync(articles, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var page1 = await handler.HandleAsync(new ListArticlesQuery { Page = 1, PageSize = 5 }, ct);
        var page2 = await handler.HandleAsync(new ListArticlesQuery { Page = 2, PageSize = 5 }, ct);

        // Assert — TotalItems is consistent across pages; items on different pages do not overlap
        page1.TotalItems.ShouldBe(page2.TotalItems);
        page1.Items.Count.ShouldBeLessThanOrEqualTo(5);
        page2.Items.Count.ShouldBeLessThanOrEqualTo(5);
        page1.Items.Select(x => x.Slug).ShouldAllBe(slug => !page2.Items.Select(x => x.Slug).Contains(slug));
    }

    [Fact(DisplayName = "HandleAsync TotalItems excludes drafts and future-dated articles")]
    public async Task HandleAsync_TotalItems_ShouldExcludeDraftsAndFutureDatedArticles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var published = ArticleFactory.Create(
            slug: "published",
            content: new string('x', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1));
        var draft = ArticleFactory.Create(
            slug: "draft",
            content: new string('x', 250),
            publicationStatus: PublicationStatus.Draft,
            publishDateUtc: Now.AddDays(-1));
        var scheduled = ArticleFactory.Create(
            slug: "scheduled",
            content: new string('x', 250),
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(1));
        await _dbContext.Articles.AddRangeAsync([published, draft, scheduled], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.TotalItems.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync strips HTML tags from excerpt")]
    public async Task HandleAsync_ShouldStripHtmlTags_FromExcerpt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(
            slug: "html-article",
            content: "<p>Hello from <strong>NEBA</strong>. Welcome.</p>",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1));
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.Items.ShouldHaveSingleItem();
        var excerpt = result.Items.Single().Excerpt;
        excerpt.ShouldNotContain("<");
        excerpt.ShouldNotContain(">");
        excerpt.ShouldContain("Hello from");
        excerpt.ShouldContain("NEBA");
    }

    [Fact(DisplayName = "HandleAsync decodes HTML entities in excerpt")]
    public async Task HandleAsync_ShouldDecodeHtmlEntities_InExcerpt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(
            slug: "entity-article",
            content: "<p>Scores &amp; standings for the 2025 season.</p>",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1));
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.Items.ShouldHaveSingleItem();
        var excerpt = result.Items.Single().Excerpt;
        excerpt.ShouldContain("Scores & standings");
        excerpt.ShouldNotContain("&amp;");
    }

    [Fact(DisplayName = "HandleAsync does not append ellipsis when plain-text excerpt is under limit")]
    public async Task HandleAsync_ShouldNotAppendEllipsis_WhenExcerptUnderLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(
            slug: "short-article",
            content: "<p>Short content.</p>",
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1));
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(DefaultQuery, ct);

        // Assert
        result.Items.ShouldHaveSingleItem();
        result.Items.Single().Excerpt.ShouldNotEndWith("...");
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