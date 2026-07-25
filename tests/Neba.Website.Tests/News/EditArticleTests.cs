using System.Net;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.News;
using Neba.Api.Contracts.News.EditArticle;
using Neba.Api.Contracts.News.GetArticle;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.News;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.EditArticle")]
public sealed class EditArticleTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<INewsApi> _mockNewsApi;
    private readonly Mock<ITournamentApiService> _mockTournamentApiService;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public EditArticleTests()
    {
        _mockNewsApi = new Mock<INewsApi>(MockBehavior.Strict);
        _mockTournamentApiService = new Mock<ITournamentApiService>(MockBehavior.Strict);
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockClientTimeZoneService = new Mock<IClientTimeZoneService>(MockBehavior.Strict);
        _mockClientTimeZoneService
            .Setup(s => s.ToLocalAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync((DateTimeOffset utc) => utc);
        _mockClientTimeZoneService
            .Setup(s => s.ToUtcAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime local) => new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Utc)));

        // Default: an article with no tournament assigned triggers the season/tournament picker load
        // on init (see EditArticle.razor's OnInitializedAsync) — tests that care about that picker's
        // behavior override this with their own Setup.
        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonViewModel>());

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditArticle.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockNewsApi.Object);
        _ctx.Services.AddSingleton(_mockTournamentApiService.Object);
        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(_mockClientTimeZoneService.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Loading / error / not-found states ──────────────────────────────────

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        // Arrange
        _mockNewsApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<ArticleDetailResponse>>().Task);

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    [Fact(DisplayName = "Should show not-found message when API returns 404")]
    public void Render_ShouldShowNotFoundMessage_WhenApiReturnsNotFound()
    {
        // Arrange
        SetupGetArticleFailure(HttpStatusCode.NotFound);

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("This article could not be found.");
    }

    [Fact(DisplayName = "Should show error alert when API returns a server error")]
    public void Render_ShouldShowErrorAlert_WhenApiReturnsServerError()
    {
        // Arrange
        SetupGetArticleFailure(HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("Unable to Load Article");
    }

    // ── Pre-population ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should pre-populate title, content, status, and read-only slug from the loaded article")]
    public void OnInit_ShouldPrepopulateFields_WhenArticleLoads()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(
            slug: "season-recap-2026",
            publicationStatus: PublicationStatus.Published,
            title: "Season Recap 2026",
            content: "<p>Great season!</p>");
        SetupGetArticleSuccess(article);

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Find("#title").GetAttribute("value").ShouldBe("Season Recap 2026");
        cut.Markup.ShouldContain("season-recap-2026");
        cut.FindAll("#slug").ShouldBeEmpty();
        cut.FindComponent<RichTextEditor>().Instance.Value.ShouldBe("<p>Great season!</p>");
    }

    [Fact(DisplayName = "Should show the locked tournament name when the article is linked to a tournament")]
    public void OnInit_ShouldShowLockedTournamentName_WhenArticleHasTournament()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(tournamentId: "tournament-1");
        SetupGetArticleSuccess(article);

        var tournament = TournamentDetailResponseFactory.Create(id: "tournament-1", name: "Granite State Open");
        using var tournamentResponse = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = tournament
        };
        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync("tournament-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tournamentResponse);

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));

        // Assert
        cut.Markup.ShouldContain("Granite State Open");
    }

    // ── Season/tournament picker (no tournament already assigned) ───────────

    [Fact(DisplayName = "Should show and populate the season/tournament picker immediately when the article has no tournament")]
    public void OnInit_ShouldShowAndPopulateTournamentPicker_WhenArticleHasNoTournament()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create();
        SetupGetArticleSuccess(article);

        var currentSeason = SeasonViewModelFactory.Create(
            id: "season-current",
            description: "Current Season",
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
        var tournament = SeasonTournamentViewModelFactory.Create(id: "tournament-42", name: "Granite State Open");

        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonViewModel> { currentSeason });
        _mockTournamentApiService
            .Setup(x => x.GetTournamentsForSeasonAsync(currentSeason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonTournamentViewModel> { tournament });

        // Act
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));

        // Assert — regression guard: the picker must load without requiring a "Change tournament"
        // click, which only exists once a tournament is already assigned.
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Change tournament"));
        cut.Find("select.neba-select").GetAttribute("value").ShouldBe("season-current");
        cut.Markup.ShouldContain("Granite State Open");
    }

    // ── Cancel / dirty guard ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate straight to the article detail page when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToArticleDetail_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(slug: "season-recap-2026");
        SetupGetArticleSuccess(article);
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        cut.Find("button.neba-btn-secondary").Click();

        // Assert
        nav.Uri.ShouldEndWith("/news/season-recap-2026");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the title")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingTitle()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create();
        SetupGetArticleSuccess(article);
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#title").Change("An updated title");

        // Act
        cut.Find("button.neba-btn-secondary").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    [Fact(DisplayName = "Should navigate to the article detail page when the discard-changes prompt is confirmed")]
    public void Click_ShouldNavigateToArticleDetail_WhenDiscardChangesPromptIsConfirmed()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(slug: "season-recap-2026");
        SetupGetArticleSuccess(article);
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        cut.Find("#title").Change("An updated title");
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        nav.Uri.ShouldEndWith("/news/season-recap-2026");
    }

    [Fact(DisplayName = "Should remain on the edit page with edits intact when the discard-changes prompt is cancelled")]
    public void Click_ShouldRemainOnPageWithEditsIntact_WhenDiscardChangesPromptIsCancelled()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create();
        SetupGetArticleSuccess(article);
        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#title").Change("An updated title");
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        nav.Uri.ShouldBe(originalUri);
        cut.Find("#title").GetAttribute("value").ShouldBe("An updated title");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should submit the mapped request, toast, and navigate to the article detail page when saving succeeds")]
    public async Task Submit_ShouldSendMappedRequestAndNavigateToDetail_WhenSaveSucceeds()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(
            articleId: "01JSARTICLE0000000000000A",
            slug: "season-recap-2026",
            title: "Season Recap 2026",
            tournamentId: "tournament-1");
        SetupGetArticleSuccess(article);

        var tournament = TournamentDetailResponseFactory.Create(id: "tournament-1", name: "Granite State Open");
        using var tournamentResponse = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = tournament
        };
        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync("tournament-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tournamentResponse);

        EditArticleRequest? capturedRequest = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };
        _mockNewsApi
            .Setup(x => x.EditArticleAsync("01JSARTICLE0000000000000A", It.IsAny<EditArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditArticleRequest, CancellationToken>((_, request, _) => capturedRequest = request)
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        await cut.InvokeAsync(() => cut.Find("#title").Change("Season Recap 2026 (Updated)"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Id.ShouldBe("01JSARTICLE0000000000000A");
        capturedRequest.Article.Title.ShouldBe("Season Recap 2026 (Updated)");
        capturedRequest.Article.TournamentId.ShouldBe("tournament-1");

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/news/season-recap-2026");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when saving fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenSaveFails()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(articleId: "01JSARTICLE0000000000000A", slug: "season-recap-2026");
        SetupGetArticleSuccess(article);

        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict
        };
        _mockNewsApi
            .Setup(x => x.EditArticleAsync(It.IsAny<string>(), It.IsAny<EditArticleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        await cut.InvokeAsync(() => cut.Find("#title").Change("Season Recap 2026 (Updated)"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Save Article");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldNotContain("/news/season-recap-2026");
    }

    // ── Header image ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should display the existing header image and remove it from submission when Remove is clicked")]
    public async Task HeaderImage_ShouldRemoveAndExcludeFromSubmit_WhenRemoveClicked()
    {
        // Arrange
        var article = ArticleDetailResponseFactory.Create(
            articleId: "01JSARTICLE0000000000000A",
            slug: "season-recap-2026",
            headerImageUrl: new Uri("https://storage.example.com/news/header.png"),
            headerImageContainer: "news",
            headerImagePath: "header.png",
            headerImageContentType: "image/png",
            headerImageSizeInBytes: 2048);
        SetupGetArticleSuccess(article);

        EditArticleRequest? capturedRequest = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };
        _mockNewsApi
            .Setup(x => x.EditArticleAsync(It.IsAny<string>(), It.IsAny<EditArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditArticleRequest, CancellationToken>((_, request, _) => capturedRequest = request)
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        cut.Markup.ShouldContain("header.png");

        // Act
        await cut.InvokeAsync(() => cut.Find(".edit-article-current-file button").Click());

        // Assert
        await cut.Find("form").SubmitAsync();
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Article.HeaderImage.ShouldBeNull();
    }

    // ── Attachments ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should include existing attachments unchanged in the submitted request")]
    public async Task Submit_ShouldIncludeExistingAttachments_WhenUnchanged()
    {
        // Arrange
        var attachment = ArticleAttachmentResponseFactory.Create(
            displayName: "Schedule",
            url: new Uri("https://storage.example.com/news-files/schedule.pdf"),
            contentType: "application/pdf",
            isInline: false,
            container: "news-files",
            path: "schedule.pdf",
            sizeInBytes: 4096);
        var article = ArticleDetailResponseFactory.Create(
            articleId: "01JSARTICLE0000000000000A",
            slug: "season-recap-2026",
            attachments: [attachment]);
        SetupGetArticleSuccess(article);

        EditArticleRequest? capturedRequest = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };
        _mockNewsApi
            .Setup(x => x.EditArticleAsync(It.IsAny<string>(), It.IsAny<EditArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditArticleRequest, CancellationToken>((_, request, _) => capturedRequest = request)
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        cut.Markup.ShouldContain("Schedule");

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Article.Attachments.Count.ShouldBe(1);
        var submitted = capturedRequest.Article.Attachments.Single();
        submitted.DisplayName.ShouldBe("Schedule");
        submitted.Container.ShouldBe("news-files");
        submitted.Path.ShouldBe("schedule.pdf");
        submitted.IsInline.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should remove a non-inline attachment immediately without a confirmation prompt")]
    public void RemoveAttachment_ShouldRemoveImmediately_WhenAttachmentIsNotInline()
    {
        // Arrange
        var attachment = ArticleAttachmentResponseFactory.Create(
            displayName: "Schedule",
            url: new Uri("https://storage.example.com/news-files/schedule.pdf"),
            isInline: false,
            container: "news-files",
            path: "schedule.pdf",
            sizeInBytes: 4096);
        var article = ArticleDetailResponseFactory.Create(attachments: [attachment]);
        SetupGetArticleSuccess(article);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        cut.FindAll("ul.edit-article-attachment-list li").Count.ShouldBe(1);

        // Act
        cut.Find("button.edit-article-attachment-action.neba-btn-danger").Click();

        // Assert
        cut.FindAll("ul.edit-article-attachment-list").ShouldBeEmpty();
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should prompt for confirmation before removing an inline attachment")]
    public void RemoveAttachment_ShouldPromptForConfirmation_WhenAttachmentIsInline()
    {
        // Arrange
        var attachment = ArticleAttachmentResponseFactory.Create(
            displayName: "inline-image.png",
            url: new Uri("https://storage.example.com/news-files/inline-image.png"),
            isInline: true,
            container: "news-files",
            path: "inline-image.png",
            sizeInBytes: 1024);
        var article = ArticleDetailResponseFactory.Create(attachments: [attachment]);
        SetupGetArticleSuccess(article);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));

        // Act
        cut.Find("button.edit-article-attachment-action.neba-btn-danger").Click();

        // Assert
        cut.Markup.ShouldContain("Remove embedded image?");
        cut.FindAll("ul.edit-article-attachment-list li").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should upload a new attachment and include it in the submitted request alongside existing attachments")]
    public async Task Submit_ShouldIncludeNewlyUploadedAttachment_AlongsideExisting()
    {
        // Arrange
        var existing = ArticleAttachmentResponseFactory.Create(
            displayName: "Schedule",
            url: new Uri("https://storage.example.com/news-files/schedule.pdf"),
            isInline: false,
            container: "news-files",
            path: "schedule.pdf",
            sizeInBytes: 4096);
        var article = ArticleDetailResponseFactory.Create(
            articleId: "01JSARTICLE0000000000000A",
            attachments: [existing]);
        SetupGetArticleSuccess(article);

        var upload = UploadedFileResponseFactory.Create(container: "news", path: "results.pdf", fileName: "results.pdf");
        using var uploadResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };
        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        EditArticleRequest? capturedRequest = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };
        _mockNewsApi
            .Setup(x => x.EditArticleAsync(It.IsAny<string>(), It.IsAny<EditArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditArticleRequest, CancellationToken>((_, request, _) => capturedRequest = request)
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditArticle>(p => p.Add(x => x.Slug, article.Slug));
        var attachmentUploader = cut.FindComponents<FileUpload>()[1].FindComponent<InputFile>();

        await cut.InvokeAsync(() => attachmentUploader.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "results.pdf", contentType: "application/pdf")));
        await cut.WaitForAssertionAsync(() => cut.FindAll("ul.edit-article-attachment-list li").Count.ShouldBe(2));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Article.Attachments.Count.ShouldBe(2);
        capturedRequest.Article.Attachments.ShouldContain(a => a.Path == "schedule.pdf");
        capturedRequest.Article.Attachments.ShouldContain(a => a.Path == "results.pdf");
    }

    private void SetupGetArticleSuccess(ArticleDetailResponse article)
    {
        using var response = new StubApiResponse<ArticleDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = article
        };

        _mockNewsApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupGetArticleFailure(HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<ArticleDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (ArticleDetailResponse?)null
        };

        _mockNewsApi
            .Setup(x => x.GetArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}