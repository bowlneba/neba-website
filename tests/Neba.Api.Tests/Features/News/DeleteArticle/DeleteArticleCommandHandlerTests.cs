using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.DeleteArticle;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Storage;

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
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private DeleteArticleCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<HybridCache>();
        return new DeleteArticleCommandHandler(_dbContext, cache);
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

        var handler = CreateHandler();
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

    [Fact(DisplayName = "HandleAsync invalidates the article list and detail cache tags when article exists")]
    public async Task HandleAsync_ShouldInvalidateListAndDetailCacheTags_WhenArticleExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = ArticleFactory.Create(slug: "cache-invalidation-article");
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);

        var cache = _serviceProvider.GetRequiredService<HybridCache>();

        const string listCacheKey = "neba:news:articles:list:page:1:size:10";
        var detailCacheKey = $"neba:news:{article.Slug}:article";

        await cache.GetOrCreateAsync(
            listCacheKey,
            _ => ValueTask.FromResult("cached-list"),
            tags: ["neba:news:articles"],
            cancellationToken: ct);
        await cache.GetOrCreateAsync(
            detailCacheKey,
            _ => ValueTask.FromResult("cached-detail"),
            tags: [$"neba:news:{article.Slug}"],
            cancellationToken: ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = article.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrCreateAsync instead of invoking the factory
        var listAfterDelete = await cache.GetOrCreateAsync(
            listCacheKey,
            _ => ValueTask.FromResult("fresh-list"),
            cancellationToken: ct);
        listAfterDelete.ShouldBe("fresh-list");

        var detailAfterDelete = await cache.GetOrCreateAsync(
            detailCacheKey,
            _ => ValueTask.FromResult("fresh-detail"),
            cancellationToken: ct);
        detailAfterDelete.ShouldBe("fresh-detail");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the article cache when article does not exist")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<HybridCache>();
        const string listCacheKey = "neba:news:articles:list:page:1:size:10";

        await cache.GetOrCreateAsync(
            listCacheKey,
            _ => ValueTask.FromResult("cached-list"),
            tags: ["neba:news:articles"],
            cancellationToken: ct);

        var handler = CreateHandler();
        var command = new DeleteArticleCommand { ArticleId = ArticleId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was deleted
        var listAfterDelete = await cache.GetOrCreateAsync(
            listCacheKey,
            _ => ValueTask.FromResult("fresh-list"),
            cancellationToken: ct);
        listAfterDelete.ShouldBe("cached-list");
    }
}
