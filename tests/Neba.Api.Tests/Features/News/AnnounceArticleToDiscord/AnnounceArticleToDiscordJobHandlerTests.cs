using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.News.AnnounceArticleToDiscord;
using Neba.Api.Features.News.Domain;
using Neba.Api.Security;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.News.AnnounceArticleToDiscord;

[IntegrationTest]
[Component("News")]
[Collection<AppDbContextFixture>]
public sealed class AnnounceArticleToDiscordJobHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishDate = new(2026, 10, 27, 11, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly Mock<INewsAnnouncementNotifier> _notifier = new(MockBehavior.Strict);
    private readonly Mock<IFileStorageService> _fileStorage = new(MockBehavior.Strict);
    private readonly FakeLogger<AnnounceArticleToDiscordJobHandler> _logger = new();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private AnnounceArticleToDiscordJobHandler CreateHandler(AppDbContext? dbContext = null, string baseUrl = "https://bowlneba.com")
        => new(
            dbContext ?? _dbContext,
            new FakeTimeProvider(Now),
            new WebsiteSettings { BaseUrl = baseUrl },
            _fileStorage.Object,
            _notifier.Object,
            _serviceProvider.GetRequiredService<IFusionCache>(),
            _logger);

    private async Task<Article> SeedArticleAsync(
        CancellationToken ct,
        string slug = "season-champions",
        PublicationStatus? publicationStatus = null,
        DateTimeOffset? announcedAtUtc = null,
        Api.Features.Storage.Domain.StoredFile? headerImage = null,
        DateTimeOffset? publishDateUtc = null)
    {
        var article = ArticleFactory.Create(
            slug: slug,
            title: "Season Champions Crowned",
            publicationStatus: publicationStatus ?? PublicationStatus.Published,
            publishDateUtc: publishDateUtc ?? PublishDate,
            announcedAtUtc: announcedAtUtc,
            headerImage: headerImage);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        return article;
    }

    private async Task<DateTimeOffset?> ReadAnnouncedAtAsync(ArticleId id, CancellationToken ct)
    {
        await using var context = fixture.CreateDbContext();

        return (await context.Articles.AsNoTracking().SingleAsync(a => a.Id == id, ct)).AnnouncedAtUtc;
    }

    private async Task SeedCacheEntryAsync(string slug, CancellationToken ct)
        => await _serviceProvider.GetRequiredService<IFusionCache>()
            .SetAsync($"entry-{slug}", 1, tags: [$"neba:news:{slug}"], token: ct);

    private async Task<bool> CacheEntryExistsAsync(string slug, CancellationToken ct)
        => (await _serviceProvider.GetRequiredService<IFusionCache>()
            .TryGetAsync<int>($"entry-{slug}", token: ct)).HasValue;

    private void AssertSkipLogged(string expectedText)
    {
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain(expectedText);
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the article no longer exists")]
    public async Task ExecuteAsync_ShouldSkip_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(expectedPublishDateUtc: PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        AssertSkipLogged("no longer exists");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the article is a draft")]
    public async Task ExecuteAsync_ShouldSkip_WhenArticleIsDraft()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, publicationStatus: PublicationStatus.Draft);
        await SeedCacheEntryAsync(article.Slug, ct);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(article.Id, ct)).ShouldBeNull();
        (await CacheEntryExistsAsync(article.Slug, ct)).ShouldBeTrue();
        AssertSkipLogged("is a draft");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the publish date no longer matches the job")]
    public async Task ExecuteAsync_ShouldSkip_WhenPublishDateChanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate.AddDays(-1));

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(article.Id, ct)).ShouldBeNull();
        AssertSkipLogged("publish date is now");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the article was already announced")]
    public async Task ExecuteAsync_ShouldSkip_WhenArticleAlreadyAnnounced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var announcedAt = Now.AddHours(-1);
        var article = await SeedArticleAsync(ct, announcedAtUtc: announcedAt);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(article.Id, ct)).ShouldBe(announcedAt);
        AssertSkipLogged("already announced");
    }

    [Fact(DisplayName = "ExecuteAsync posts the title, article link and header image, and stamps the article")]
    public async Task ExecuteAsync_ShouldPostAnnouncementAndStampArticle_WhenArticleIsDue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create();
        var imageUrl = new Uri("https://files.bowlneba.com/news/header.jpg");
        var article = await SeedArticleAsync(ct, headerImage: headerImage);
        await SeedCacheEntryAsync(article.Slug, ct);
        _fileStorage.Setup(f => f.GetBlobUri(headerImage.Container, headerImage.Path)).Returns(imageUrl);

        NewsAnnouncement? posted = null;
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Callback<NewsAnnouncement, CancellationToken>((announcement, _) => posted = announcement)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.Title.ShouldBe("Season Champions Crowned");
        posted.ArticleUrl.ShouldBe(new Uri("https://bowlneba.com/news/season-champions"));
        posted.ImageUrl.ShouldBe(imageUrl);
        (await ReadAnnouncedAtAsync(article.Id, ct)).ShouldBe(Now);
        (await CacheEntryExistsAsync(article.Slug, ct)).ShouldBeFalse();
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem().Message.ShouldContain("Announced article");
    }

    [Fact(DisplayName = "ExecuteAsync posts without an image when the article has no header image")]
    public async Task ExecuteAsync_ShouldPostWithoutImage_WhenArticleHasNoHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);

        NewsAnnouncement? posted = null;
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Callback<NewsAnnouncement, CancellationToken>((announcement, _) => posted = announcement)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.ImageUrl.ShouldBeNull();
        _fileStorage.VerifyNoOtherCalls();
    }

    [Theory(DisplayName = "ExecuteAsync builds the article link from the configured website base URL")]
    [InlineData("https://bowlneba.com", "https://bowlneba.com/news/season-champions")]
    [InlineData("https://preview.bowlneba.com", "https://preview.bowlneba.com/news/season-champions")]
    [InlineData("http://localhost:5200/", "http://localhost:5200/news/season-champions")]
    public async Task ExecuteAsync_ShouldBuildLinkFromBaseUrl(string website, string expectedLink)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);

        NewsAnnouncement? posted = null;
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Callback<NewsAnnouncement, CancellationToken>((announcement, _) => posted = announcement)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(baseUrl: website);
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.ArticleUrl.ShouldBe(new Uri(expectedLink));
    }

    [Fact(DisplayName = "ExecuteAsync stamps the article before it posts")]
    public async Task ExecuteAsync_ShouldClaimBeforePosting()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);
        DateTimeOffset? announcedAtWhilePosting = null;
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Returns<NewsAnnouncement, CancellationToken>(async (_, token) =>
                announcedAtWhilePosting = await ReadAnnouncedAtAsync(article.Id, token));
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        announcedAtWhilePosting.ShouldBe(Now);
    }

    [Fact(DisplayName = "ExecuteAsync leaves the article stamped, skips cache eviction, and rethrows when the post fails")]
    public async Task ExecuteAsync_ShouldKeepClaimAndRethrow_WhenPostFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);
        await SeedCacheEntryAsync(article.Slug, ct);
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Discord is down"));
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(() => handler.ExecuteAsync(job, ct));
        (await ReadAnnouncedAtAsync(article.Id, ct)).ShouldBe(Now);
        (await CacheEntryExistsAsync(article.Slug, ct)).ShouldBeTrue();
    }

    [Fact(DisplayName = "ExecuteAsync does not post a second time when a duplicate job runs afterwards")]
    public async Task ExecuteAsync_ShouldPostOnce_WhenTheSameJobRunsTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync posts once when two jobs for the same article run at the same time")]
    public async Task ExecuteAsync_ShouldPostOnce_WhenTwoJobsRunConcurrently()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct);
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = CreateHandler(firstContext);
        var second = CreateHandler(secondContext);
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await Task.WhenAll(first.ExecuteAsync(job, ct), second.ExecuteAsync(job, ct));

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync posts when the date the job was scheduled for carries sub-microsecond ticks the database drops")]
    public async Task ExecuteAsync_ShouldPost_WhenScheduledDateWasTruncatedToMicroseconds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, publishDateUtc: PublishDate.AddTicks(7));
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();
        var job = AnnounceArticleToDiscordJobFactory.Create(article.Id, PublishDate);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}