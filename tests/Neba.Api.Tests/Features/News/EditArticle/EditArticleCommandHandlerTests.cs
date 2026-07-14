using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.EditArticle;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.News.EditArticle;

[IntegrationTest]
[Component("News")]
[Collection<AppDbContextFixture>]
public sealed class EditArticleCommandHandlerTests(AppDbContextFixture fixture)
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

    private EditArticleCommandHandler CreateHandler(IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new EditArticleCommandHandler(_dbContext, scheduler, cache);
    }

#pragma warning disable S107
    private static EditArticleCommand ValidCommand(
        ArticleId articleId,
        string? title = null,
        string? content = null,
        PublicationStatus? publicationStatus = null,
        DateTimeOffset? publishDate = null,
        TournamentId? tournamentId = null,
        StoredFile? headerImage = null,
        IReadOnlyCollection<EditArticleAttachment>? attachments = null)
        => new()
        {
            ArticleId = articleId,
            Title = title ?? ArticleFactory.ValidTitle,
            Content = content ?? ArticleFactory.ValidContent,
            PublicationStatus = publicationStatus ?? ArticleFactory.ValidPublicationStatus,
            PublishDate = publishDate ?? ArticleFactory.ValidPublishDateUtc,
            TournamentId = tournamentId,
            HeaderImage = headerImage,
            Attachments = attachments ?? []
        };
#pragma warning restore S107

    private async Task<Article> SeedArticleAsync(CancellationToken ct, string? slug = null, StoredFile? headerImage = null, IReadOnlyCollection<ArticleAttachment>? attachments = null)
    {
        var article = ArticleFactory.Create(slug: slug, headerImage: headerImage, attachments: attachments);
        await _dbContext.Articles.AddAsync(article, ct);
        await _dbContext.SaveChangesAsync(ct);
        return article;
    }

    [Fact(DisplayName = "HandleAsync returns NotFound error when article does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenArticleDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(ArticleId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Article.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns Validation error when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "tournament-missing-article");
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, tournamentId: TournamentId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Article.Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync does not modify the article when tournament does not exist")]
    public async Task HandleAsync_ShouldNotModifyArticle_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "tournament-missing-no-change-article");
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, title: "Should Not Apply", tournamentId: TournamentId.New());

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var persisted = await _dbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id, ct);
        persisted.Title.ShouldBe(ArticleFactory.ValidTitle);
    }

    [Fact(DisplayName = "HandleAsync succeeds when tournament exists")]
    public async Task HandleAsync_ShouldSucceed_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "tournament-linked-edit-article");
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(article.Id, tournamentId: tournament.Id);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync sanitizes script tags out of content before persisting")]
    public async Task HandleAsync_ShouldSanitizeScriptTagsOutOfContent_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "sanitized-content-edit-article");
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, content: "<p>Hello</p><script>alert('xss')</script>");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var persisted = await _dbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id, ct);
        persisted.Content.ShouldNotContain("<script", Case.Insensitive);
        persisted.Content.ShouldContain("<p>Hello</p>");
    }

    [Fact(DisplayName = "HandleAsync returns Article.Content.Required when content sanitizes to an empty string")]
    public async Task HandleAsync_ShouldReturnContentRequiredError_WhenContentSanitizesToEmpty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "empty-sanitized-content-edit-article");
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, content: "<script>alert('xss')</script>");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Content.Required");
    }

    [Fact(DisplayName = "HandleAsync returns validation error when title is empty")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenTitleIsEmpty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "empty-title-edit-article");
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, title: string.Empty);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Title.Required");
    }

    [Fact(DisplayName = "HandleAsync persists updated fields when command is valid")]
    public async Task HandleAsync_ShouldPersistUpdatedFields_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "updated-fields-edit-article");
        var handler = CreateHandler();
        var command = ValidCommand(
            article.Id,
            title: "Updated Title",
            content: "Updated content.",
            publicationStatus: PublicationStatus.Published,
            publishDate: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id, ct);
        persisted.Title.ShouldBe("Updated Title");
        persisted.Content.ShouldBe("Updated content.");
        persisted.PublicationStatus.ShouldBe(PublicationStatus.Published);
        persisted.PublishDateUtc.ShouldBe(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "HandleAsync converts the local publish date to UTC using its embedded offset")]
    public async Task HandleAsync_ShouldConvertLocalPublishDateToUtc_UsingItsEmbeddedOffset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "eastern-offset-edit-article");
        var handler = CreateHandler();
        var command = ValidCommand(
            article.Id,
            publishDate: new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.FromHours(-4)));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id, ct);
        persisted.PublishDateUtc.ShouldBe(new DateTimeOffset(2025, 6, 1, 13, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "HandleAsync replaces the header image when a different one is provided")]
    public async Task HandleAsync_ShouldReplaceHeaderImage_WhenDifferentImageProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var oldHeaderImage = StoredFileFactory.Create(container: "old-header-container", path: "old-header.jpg");
        var article = await SeedArticleAsync(ct, slug: "replace-header-edit-article", headerImage: oldHeaderImage);
        var newHeaderImage = StoredFileFactory.Create(container: "new-header-container", path: "new-header.jpg");

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>())).Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id, headerImage: newHeaderImage);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id, ct);
        persisted.HeaderImage.ShouldBe(newHeaderImage);
    }

    [Fact(DisplayName = "HandleAsync enqueues the old header image for deletion when replaced")]
    public async Task HandleAsync_ShouldEnqueueOldHeaderImageForDeletion_WhenReplaced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var oldHeaderImage = StoredFileFactory.Create(container: "orphaned-header-container", path: "orphaned-header.jpg");
        var article = await SeedArticleAsync(ct, slug: "orphaned-header-edit-article", headerImage: oldHeaderImage);
        var newHeaderImage = StoredFileFactory.Create(container: "kept-header-container", path: "kept-header.jpg");

        DeleteArticleFilesJob? enqueuedJob = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>()))
            .Callback<DeleteArticleFilesJob>(job => enqueuedJob = job)
            .Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id, headerImage: newHeaderImage);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        enqueuedJob.ShouldNotBeNull();
        enqueuedJob.Files.ShouldContain(f => f.Container == oldHeaderImage.Container && f.Path == oldHeaderImage.Path);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue the header image for deletion when unchanged")]
    public async Task HandleAsync_ShouldNotEnqueueHeaderImageForDeletion_WhenUnchanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "unchanged-header-container", path: "unchanged-header.jpg");
        var article = await SeedArticleAsync(ct, slug: "unchanged-header-edit-article", headerImage: headerImage);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id, headerImage: headerImage);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert — a strict mock with no Enqueue setup would throw if Enqueue was called
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes attachments no longer in the desired list")]
    public async Task HandleAsync_ShouldRemoveAttachments_WhenNoLongerDesired()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingAttachment = ArticleAttachmentFactory.Create(
            displayName: "Removed Attachment",
            file: StoredFileFactory.Create(container: "removed-attachment-container", path: "removed.pdf"));
        var article = await SeedArticleAsync(ct, slug: "remove-attachment-edit-article", attachments: [existingAttachment]);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>())).Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id, attachments: []);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .Include(a => a.Attachments)
            .SingleAsync(a => a.Id == article.Id, ct);
        persisted.Attachments.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync enqueues removed attachments for deletion")]
    public async Task HandleAsync_ShouldEnqueueRemovedAttachmentsForDeletion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var removedFile = StoredFileFactory.Create(container: "orphaned-attachment-container", path: "orphaned.pdf");
        var existingAttachment = ArticleAttachmentFactory.Create(displayName: "Removed Attachment", file: removedFile);
        var article = await SeedArticleAsync(ct, slug: "orphaned-attachment-edit-article", attachments: [existingAttachment]);

        DeleteArticleFilesJob? enqueuedJob = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteArticleFilesJob>()))
            .Callback<DeleteArticleFilesJob>(job => enqueuedJob = job)
            .Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id, attachments: []);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        enqueuedJob.ShouldNotBeNull();
        enqueuedJob.Files.ShouldContain(f => f.Container == removedFile.Container && f.Path == removedFile.Path);
    }

    [Fact(DisplayName = "HandleAsync adds new attachments present in the desired list")]
    public async Task HandleAsync_ShouldAddNewAttachments_WhenPresentInDesiredList()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "add-attachment-edit-article");
        var newAttachment = EditArticleAttachmentFactory.Create(
            displayName: "New Attachment",
            file: StoredFileFactory.Create(container: "new-attachment-container", path: "new.pdf"));

        var handler = CreateHandler();
        var command = ValidCommand(article.Id, attachments: [newAttachment]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .Include(a => a.Attachments)
            .SingleAsync(a => a.Id == article.Id, ct);
        var persistedAttachment = persisted.Attachments.ShouldHaveSingleItem();
        persistedAttachment.DisplayName.ShouldBe("New Attachment");
        persistedAttachment.File.ShouldBe(newAttachment.File);
    }

    [Fact(DisplayName = "HandleAsync keeps attachments present in both existing and desired lists unchanged")]
    public async Task HandleAsync_ShouldKeepUnchangedAttachments_WhenPresentInBothLists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var keptFile = StoredFileFactory.Create(container: "kept-attachment-container", path: "kept.pdf");
        var existingAttachment = ArticleAttachmentFactory.Create(displayName: "Kept Attachment", file: keptFile);
        var article = await SeedArticleAsync(ct, slug: "kept-attachment-edit-article", attachments: [existingAttachment]);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var keptDesiredAttachment = EditArticleAttachmentFactory.Create(displayName: "Kept Attachment", file: keptFile);
        var command = ValidCommand(article.Id, attachments: [keptDesiredAttachment]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .Include(a => a.Attachments)
            .SingleAsync(a => a.Id == article.Id, ct);
        var persistedAttachment = persisted.Attachments.ShouldHaveSingleItem();
        persistedAttachment.File.ShouldBe(keptFile);
    }

    [Fact(DisplayName = "HandleAsync returns validation error when a new attachment is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenNewAttachmentIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "invalid-new-attachment-edit-article");
        var invalidAttachment = EditArticleAttachmentFactory.Create(displayName: string.Empty);
        var handler = CreateHandler();
        var command = ValidCommand(article.Id, attachments: [invalidAttachment]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("ArticleAttachment.DisplayName");
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when there are no orphaned files")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenNoOrphanedFiles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "no-orphans-edit-article");
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = ValidCommand(article.Id);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert — a strict mock with no Enqueue setup would throw if Enqueue was called
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync invalidates the article list and detail cache tags when command is valid")]
    public async Task HandleAsync_ShouldInvalidateListAndDetailCacheTags_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "cache-invalidation-edit-article");
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
        var command = ValidCommand(article.Id);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterEdit = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterEdit.ShouldBe("fresh-list");

        var detailAfterEdit = await cache.GetOrSetAsync(
            detailCacheKey,
            _ => Task.FromResult("fresh-detail"),
            token: ct);
        detailAfterEdit.ShouldBe("fresh-detail");
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
        var command = ValidCommand(ArticleId.New());

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was updated
        var listAfterEdit = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterEdit.ShouldBe("cached-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the article cache when tournament does not exist")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "cache-tournament-missing-edit-article");
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string listCacheKey = "neba:news:articles:list:page:1:size:10";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: ["neba:news:articles"],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(article.Id, tournamentId: TournamentId.New());

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was updated
        var listAfterEdit = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterEdit.ShouldBe("cached-list");
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for the header image when command is valid")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForHeaderImage_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "header-pending-upload-edit-article");
        var headerImage = StoredFileFactory.Create(container: "header-pending-edit-container", path: "header-pending-edit.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: headerImage.Container, path: headerImage.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(article.Id, headerImage: headerImage);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == headerImage.Container && p.Path == headerImage.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for an attachment when command is valid")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForAttachment_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "attachment-pending-upload-edit-article");
        var attachment = EditArticleAttachmentFactory.Create(
            file: StoredFileFactory.Create(container: "attachment-pending-edit-container", path: "attachment-pending-edit.pdf"));
        var pendingUpload = PendingUploadFactory.Create(container: attachment.File.Container, path: attachment.File.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(article.Id, attachments: [attachment]);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == attachment.File.Container && p.Path == attachment.File.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not remove unrelated pending upload records")]
    public async Task HandleAsync_ShouldNotRemoveUnrelatedPendingUploads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var article = await SeedArticleAsync(ct, slug: "no-matching-pending-upload-edit-article");
        var unrelatedPendingUpload = PendingUploadFactory.Create(container: "unrelated-edit-container", path: "unrelated-edit-file.jpg");
        await _dbContext.PendingUploads.AddAsync(unrelatedPendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(article.Id);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == "unrelated-edit-container" && p.Path == "unrelated-edit-file.jpg", ct);
        stillPending.ShouldBeTrue();
    }
}