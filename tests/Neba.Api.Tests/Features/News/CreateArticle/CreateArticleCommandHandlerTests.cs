using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.Domain;
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

namespace Neba.Api.Tests.Features.News.CreateArticle;

[IntegrationTest]
[Component("News")]
[Collection<AppDbContextFixture>]
public sealed class CreateArticleCommandHandlerTests(AppDbContextFixture fixture)
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

    private CreateArticleCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return new CreateArticleCommandHandler(_dbContext, cache);
    }

    private static CreateArticleCommand ValidCommand(
        string? title = null,
        string? slug = null,
        string? content = null,
        PublicationStatus? publicationStatus = null,
        DateTimeOffset? publishDate = null,
        TournamentId? tournamentId = null,
        StoredFile? headerImage = null,
        IReadOnlyCollection<NewArticleAttachment>? attachments = null)
        => new()
        {
            Title = title ?? ArticleFactory.ValidTitle,
            Slug = slug ?? ArticleFactory.ValidSlug,
            Content = content ?? ArticleFactory.ValidContent,
            PublicationStatus = publicationStatus ?? ArticleFactory.ValidPublicationStatus,
            PublishDate = publishDate ?? ArticleFactory.ValidPublishDateUtc,
            TournamentId = tournamentId,
            HeaderImage = headerImage,
            Attachments = attachments ?? []
        };

    [Fact(DisplayName = "HandleAsync returns Validation error when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(tournamentId: TournamentId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Article.Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync does not persist an article when tournament does not exist")]
    public async Task HandleAsync_ShouldNotPersistArticle_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(slug: "no-tournament-article", tournamentId: TournamentId.New());

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var exists = await _dbContext.Articles.AsNoTracking()
            .AnyAsync(a => a.Slug == "no-tournament-article", ct);
        exists.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync succeeds when tournament exists")]
    public async Task HandleAsync_ShouldSucceed_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "tournament-linked-article", tournamentId: tournament.Id);

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
        var handler = CreateHandler();
        var command = ValidCommand(
            slug: "sanitized-content-article",
            content: "<p>Hello</p><script>alert('xss')</script>");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var article = await _dbContext.Articles.SingleAsync(a => a.Slug == "sanitized-content-article", ct);
        article.Content.ShouldNotContain("<script", Case.Insensitive);
        article.Content.ShouldContain("<p>Hello</p>");
    }

    [Fact(DisplayName = "HandleAsync returns Article.Content.Required when content sanitizes to an empty string")]
    public async Task HandleAsync_ShouldReturnContentRequiredError_WhenContentSanitizesToEmpty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(content: "<script>alert('xss')</script>");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Content.Required");
    }

    [Fact(DisplayName = "HandleAsync returns validation error when article creation fails")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenArticleCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(title: string.Empty);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Article.Title.Required");
    }

    [Fact(DisplayName = "HandleAsync does not persist an article when article creation fails")]
    public async Task HandleAsync_ShouldNotPersistArticle_WhenArticleCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(title: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Articles.CountAsync(ct);
        count.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync returns Conflict error when slug already exists")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenSlugAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingArticle = ArticleFactory.Create(slug: "existing-slug");
        await _dbContext.Articles.AddAsync(existingArticle, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(title: "Existing Slug", slug: "existing-slug");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Article.Slug.AlreadyExists");
    }

    [Fact(DisplayName = "HandleAsync does not persist a duplicate article when slug already exists")]
    public async Task HandleAsync_ShouldNotPersistDuplicateArticle_WhenSlugAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingArticle = ArticleFactory.Create(slug: "duplicate-slug");
        await _dbContext.Articles.AddAsync(existingArticle, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(title: "Duplicate Slug", slug: "duplicate-slug");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Articles.CountAsync(a => a.Slug == "duplicate-slug", ct);
        count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync persists the article when command is valid")]
    public async Task HandleAsync_ShouldPersistArticle_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            title: "My New Article",
            slug: "my-new-article",
            content: "Some content.",
            publicationStatus: PublicationStatus.Published,
            publishDate: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .SingleAsync(a => a.Slug == "my-new-article", ct);
        persisted.Title.ShouldBe("My New Article");
        persisted.Content.ShouldBe("Some content.");
        persisted.PublicationStatus.ShouldBe(PublicationStatus.Published);
        persisted.PublishDateUtc.ShouldBe(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "HandleAsync converts the local publish date to UTC using its embedded offset")]
    public async Task HandleAsync_ShouldConvertLocalPublishDateToUtc_UsingItsEmbeddedOffset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(
            slug: "eastern-offset-article",
            publishDate: new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.FromHours(-4)));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .SingleAsync(a => a.Slug == "eastern-offset-article", ct);
        persisted.PublishDateUtc.ShouldBe(new DateTimeOffset(2025, 6, 1, 13, 0, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "HandleAsync returns the created article's id and normalized slug when command is valid")]
    public async Task HandleAsync_ShouldReturnCreatedArticleIdAndSlug_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(title: "Some Title", slug: "Some Slug!!");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("some-slug");
        var persisted = await _dbContext.Articles.AsNoTracking()
            .SingleAsync(a => a.Slug == "some-slug", ct);
        result.Value.Id.ShouldBe(persisted.Id);
    }

    [Fact(DisplayName = "HandleAsync generates the slug from the title when slug is not provided")]
    public async Task HandleAsync_ShouldGenerateSlugFromTitle_WhenSlugIsNotProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = new CreateArticleCommand
        {
            Title = "A Brand New Headline",
            Content = ArticleFactory.ValidContent,
            PublicationStatus = ArticleFactory.ValidPublicationStatus,
            PublishDate = ArticleFactory.ValidPublishDateUtc
        };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("a-brand-new-headline");
    }

    [Fact(DisplayName = "HandleAsync invalidates the article list cache tag when command is valid")]
    public async Task HandleAsync_ShouldInvalidateArticleListCacheTag_WhenCommandIsValid()
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
        var command = ValidCommand(slug: "cache-invalidation-create-article");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the article list cache tag when tournament does not exist")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentDoesNotExist()
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
        var command = ValidCommand(tournamentId: TournamentId.New());

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was created
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("cached-list");
    }

    [Fact(DisplayName = "HandleAsync persists the header image when one is provided")]
    public async Task HandleAsync_ShouldPersistHeaderImage_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var headerImage = StoredFileFactory.Create(container: "header-container", path: "header.jpg");
        var command = ValidCommand(slug: "header-image-article", headerImage: headerImage);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .SingleAsync(a => a.Slug == "header-image-article", ct);
        persisted.HeaderImage.ShouldBe(headerImage);
    }

    [Fact(DisplayName = "HandleAsync persists attachments when provided")]
    public async Task HandleAsync_ShouldPersistAttachments_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var attachment = NewArticleAttachmentFactory.Create(displayName: "My Attachment", isInline: true);
        var command = ValidCommand(slug: "attachment-article", attachments: [attachment]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Articles.AsNoTracking()
            .Include(a => a.Attachments)
            .SingleAsync(a => a.Slug == "attachment-article", ct);
        var persistedAttachment = persisted.Attachments.ShouldHaveSingleItem();
        persistedAttachment.DisplayName.ShouldBe("My Attachment");
        persistedAttachment.IsInline.ShouldBeTrue();
        persistedAttachment.File.ShouldBe(attachment.File);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when an attachment is invalid")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenAttachmentIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var invalidAttachment = NewArticleAttachmentFactory.Create(displayName: string.Empty);
        var command = ValidCommand(attachments: [invalidAttachment]);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("ArticleAttachment.DisplayName");
    }

    [Fact(DisplayName = "HandleAsync does not persist an article when an attachment is invalid")]
    public async Task HandleAsync_ShouldNotPersistArticle_WhenAttachmentIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var invalidAttachment = NewArticleAttachmentFactory.Create(displayName: string.Empty);
        var command = ValidCommand(slug: "invalid-attachment-article", attachments: [invalidAttachment]);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var exists = await _dbContext.Articles.AsNoTracking()
            .AnyAsync(a => a.Slug == "invalid-attachment-article", ct);
        exists.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for the header image when command is valid")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForHeaderImage_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var headerImage = StoredFileFactory.Create(container: "header-pending-container", path: "header-pending.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: headerImage.Container, path: headerImage.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "header-pending-upload-article", headerImage: headerImage);

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
        var attachment = NewArticleAttachmentFactory.Create(
            file: StoredFileFactory.Create(container: "attachment-pending-container", path: "attachment-pending.pdf"));
        var pendingUpload = PendingUploadFactory.Create(container: attachment.File.Container, path: attachment.File.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "attachment-pending-upload-article", attachments: [attachment]);

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
        var unrelatedPendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(unrelatedPendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(slug: "no-matching-pending-upload-article");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == "unrelated-container" && p.Path == "unrelated-file.jpg", ct);
        stillPending.ShouldBeTrue();
    }
}