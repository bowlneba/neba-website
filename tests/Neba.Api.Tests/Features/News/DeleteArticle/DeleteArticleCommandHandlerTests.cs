using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[IntegrationTest]
[Component("News")]
[Collection<AppDbContextFixture>]
public sealed class DeleteArticleCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
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

    private DeleteArticleCommandHandler CreateHandler(IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new DeleteArticleCommandHandler(_dbContext, scheduler, cache);
    }

    [Fact(DisplayName = "HandleAsync returns Deleted when article does not exist")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenArticleDoesNotExist()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = ArticleId.New() };

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns Deleted when article exists")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenArticleExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create();
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the article from the database when it exists")]
    public async Task HandleAsync_ShouldRemoveArticleFromDatabase_WhenArticleExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create();
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillExists = await _dbContext.Articles.AsNoTracking()
            .AnyAsync(a => a.Id == article.Id, ct);
        stillExists.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not modify the database when article does not exist")]
    public async Task HandleAsync_ShouldNotModifyDatabase_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingArticle = ArticleFactory.Create();
        await _dbContext.Articles.AddAsync(existingArticle, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = ArticleId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillExists = await _dbContext.Articles.AsNoTracking()
            .AnyAsync(a => a.Id == existingArticle.Id, ct);
        stillExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync cascades deletion to the article's attachments when it exists")]
    public async Task HandleAsync_ShouldCascadeDeleteAttachments_WhenArticleExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var attachments = new[]
        {
            ArticleAttachmentFactory.Create(displayName: "Schedule", file: StoredFileFactory.Create()),
        };
        var article = ArticleFactory.Create(attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var articleDbId = _dbContext.Entry(article)
            .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>())).Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        const string sql = "SELECT COUNT(*)::int AS \"Value\" FROM " + AppDbContext.DefaultSchema
            + ".article_attachments WHERE " + ArticleConfiguration.ForeignKey + " = {0}";
        var remainingAttachments = await _dbContext.Database
            .SqlQueryRaw<int>(sql, articleDbId)
            .SingleAsync(ct);
        remainingAttachments.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync enqueues a file deletion job for the article's attachments and header image when article exists")]
    public async Task HandleAsync_ShouldEnqueueFileDeletionJob_WhenArticleHasAttachmentsAndHeaderImage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "header-container", path: "header.jpg");
        var attachmentFile = StoredFileFactory.Create(container: "attachment-container", path: "attachment.pdf");
        var attachments = new[]
        {
            ArticleAttachmentFactory.Create(displayName: "Schedule", file: attachmentFile),
        };
        var article = ArticleFactory.Create(headerImage: headerImage, attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        DeleteArticleFilesJob? enqueuedJob = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>()))
            .Callback<DeleteArticleFilesJob>(job => enqueuedJob = job)
            .Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        enqueuedJob.ShouldNotBeNull();
        enqueuedJob.Files.ShouldContain(f => f.Container == headerImage.Container && f.Path == headerImage.Path);
        enqueuedJob.Files.ShouldContain(f => f.Container == attachmentFile.Container && f.Path == attachmentFile.Path);
        enqueuedJob.Files.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when article has no header image or attachments")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenArticleHasNoFiles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create();
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a strict mock with no Enqueue setup would throw if called
        scheduler.Verify(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when article does not exist")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteArticleCommand { ArticleId = ArticleId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        scheduler.Verify(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync invalidates the article list and detail cache tags when article exists")]
    public async Task HandleAsync_ShouldInvalidateListAndDetailCacheTags_WhenArticleExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(slug: "cache-invalidation-article");
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        const string listCacheKey = "neba:news:articles:list:page:1:size:10";
        var detailCacheKey = $"neba:news:{article.Slug}:article";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: ["neba:news:articles"],
            token: ct);
        await cache.GetOrSetAsync(
            detailCacheKey,
            _ => Task.FromResult("cached-detail"),
            tags: [$"neba:news:{article.Slug}"],
            token: ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterDelete = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterDelete.ShouldBe("fresh-list");

        var detailAfterDelete = await cache.GetOrSetAsync(
            detailCacheKey,
            _ => Task.FromResult("fresh-detail"),
            token: ct);
        detailAfterDelete.ShouldBe("fresh-detail");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the article cache when article does not exist")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:news:articles:list:page:1:size:10";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: ["neba:news:articles"],
            token: ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = ArticleId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was deleted
        var listAfterDelete = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterDelete.ShouldBe("cached-list");
    }
}