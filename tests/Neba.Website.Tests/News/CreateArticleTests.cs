using System.Net;

using Bunit;
using Bunit.TestDoubles;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.Api.Contracts.News;
using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Help;
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
[Component("Website.News.CreateArticle")]
public sealed class CreateArticleTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<INewsApi> _mockNewsApi;
    private readonly Mock<ITournamentApiService> _mockTournamentApiService;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public CreateArticleTests()
    {
        _mockNewsApi = new Mock<INewsApi>(MockBehavior.Strict);
        _mockTournamentApiService = new Mock<ITournamentApiService>(MockBehavior.Strict);
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockClientTimeZoneService = new Mock<IClientTimeZoneService>(MockBehavior.Strict);
        _mockClientTimeZoneService
            .Setup(s => s.ToUtcAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime local) => new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Utc)));

        using var tournamentResponse = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = false,
            Content = null
        };

        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tournamentResponse);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CreateArticle.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockNewsApi.Object);
        _ctx.Services.AddSingleton(_mockTournamentApiService.Object);
        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(_mockClientTimeZoneService.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    [Fact(DisplayName = "Should navigate straight to the news list when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToNewsList_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var cut = RenderCreateArticle();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        cut.Find("button.neba-btn-secondary").Click();

        // Assert
        nav.Uri.ShouldEndWith("/news");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the title")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingTitle()
    {
        // Arrange
        var cut = RenderCreateArticle();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#title").Change("A brand new article title");

        // Act
        cut.Find("button.neba-btn-secondary").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    [Fact(DisplayName = "Should navigate to the news list when the discard-changes prompt is confirmed")]
    public void Click_ShouldNavigateToNewsList_WhenDiscardChangesPromptIsConfirmed()
    {
        // Arrange
        var cut = RenderCreateArticle();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        cut.Find("#title").Change("A brand new article title");
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        nav.Uri.ShouldEndWith("/news");
    }

    [Fact(DisplayName = "Should remain on the create page with edits intact when the discard-changes prompt is cancelled")]
    public void Click_ShouldRemainOnPageWithEditsIntact_WhenDiscardChangesPromptIsCancelled()
    {
        // Arrange
        var cut = RenderCreateArticle();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#title").Change("A brand new article title");
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        nav.Uri.ShouldBe(originalUri);
        cut.Find("#title").GetAttribute("value").ShouldBe("A brand new article title");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    // ── Season/tournament loading (no deep link) ────────────────────────────

    [Fact(DisplayName = "Should pre-select the season containing today's date and load its tournaments")]
    public void OnInit_ShouldPreSelectCurrentSeasonAndLoadItsTournaments_WhenNoTournamentIdSupplied()
    {
        // Arrange
        var pastSeason = SeasonViewModelFactory.Create(
            id: "season-past",
            description: "2020 Season",
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2020, 12, 31));
        var currentSeason = SeasonViewModelFactory.Create(
            id: "season-current",
            description: "Current Season",
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
        var tournament = SeasonTournamentViewModelFactory.Create(id: "tournament-42", name: "Granite State Open");

        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonViewModel> { pastSeason, currentSeason });
        _mockTournamentApiService
            .Setup(x => x.GetTournamentsForSeasonAsync(currentSeason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonTournamentViewModel> { tournament });

        // Act
        var cut = RenderWithoutDeepLink();

        // Assert
        cut.Find("select.neba-select").GetAttribute("value").ShouldBe("season-current");
        cut.Markup.ShouldContain("Granite State Open");
    }

    [Fact(DisplayName = "Should leave the season and tournament dropdowns empty when loading seasons fails")]
    public void OnInit_ShouldLeaveDropdownsEmpty_WhenGetSeasonsFails()
    {
        // Arrange
        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure());

        // Act
        var cut = RenderWithoutDeepLink();

        // Assert
        cut.FindAll("select.neba-select")[0].Children.ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Loading seasons and tournaments");
    }

    [Fact(DisplayName = "Should reload tournaments for the newly selected season and clear the tournament selection")]
    public async Task SeasonChanged_ShouldReloadTournamentsAndClearSelection_WhenDifferentSeasonSelected()
    {
        // Arrange
        var seasonOne = SeasonViewModelFactory.Create(id: "season-1", description: "2025 Season");
        var seasonTwo = SeasonViewModelFactory.Create(id: "season-2", description: "2026 Season");
        var tournamentInSeasonOne = SeasonTournamentViewModelFactory.Create(id: "t1", name: "Season One Open");
        var tournamentInSeasonTwo = SeasonTournamentViewModelFactory.Create(id: "t2", name: "Season Two Open");

        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonViewModel> { seasonOne, seasonTwo });
        _mockTournamentApiService
            .Setup(x => x.GetTournamentsForSeasonAsync(seasonOne, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonTournamentViewModel> { tournamentInSeasonOne });
        _mockTournamentApiService
            .Setup(x => x.GetTournamentsForSeasonAsync(seasonTwo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonTournamentViewModel> { tournamentInSeasonTwo });

        var cut = RenderWithoutDeepLink();
        cut.Markup.ShouldContain("Season One Open");

        // Act
        await cut.InvokeAsync(() => cut.Find("select.neba-select").Change("season-2"));

        // Assert
        cut.Markup.ShouldContain("Season Two Open");
        cut.Markup.ShouldNotContain("Season One Open");
        cut.FindAll("select.neba-select")[1].GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Fact(DisplayName = "Should mark the form dirty when a tournament is selected from the dropdown")]
    public async Task TournamentChanged_ShouldMarkFormDirty_WhenTournamentSelected()
    {
        // Arrange
        var season = SeasonViewModelFactory.Create();
        var tournament = SeasonTournamentViewModelFactory.Create(id: "tournament-99", name: "Dirty Test Open");

        _mockTournamentApiService
            .Setup(x => x.GetSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonViewModel> { season });
        _mockTournamentApiService
            .Setup(x => x.GetTournamentsForSeasonAsync(season, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SeasonTournamentViewModel> { tournament });

        var cut = RenderWithoutDeepLink();

        // Act
        await cut.InvokeAsync(() => cut.FindAll("select.neba-select")[1].Change("tournament-99"));
        await cut.InvokeAsync(() => cut.Find("button.neba-btn-secondary").Click());

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
    }

    // ── Slug placeholder ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show a normalized slug preview as the slug field placeholder while typing the title")]
    public async Task SlugPlaceholder_ShouldNormalizeTitle_WhileTyping()
    {
        // Arrange
        var cut = RenderCreateArticle();

        // Act
        await cut.InvokeAsync(() => cut.Find("#title").Change("  Season Champions & Records!! 2026  "));

        // Assert
        cut.Find("#slug").GetAttribute("placeholder").ShouldBe("season-champions-records-2026");
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should submit the mapped request, toast, and navigate to the new article's slug when creation succeeds")]
    public async Task Submit_ShouldSendMappedRequestAndNavigateToSlug_WhenCreationSucceeds()
    {
        // Arrange
        CreateArticleRequest? capturedRequest = null;
        using var response = new StubApiResponse<ArticleResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = ArticleResponseFactory.Create(slug: "season-recap-2026")
        };

        _mockNewsApi
            .Setup(x => x.CreateArticleAsync(It.IsAny<CreateArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateArticleRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(response);

        var cut = RenderCreateArticle();
        await cut.InvokeAsync(() => cut.Find("#title").Change("Season Recap 2026"));
        await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.NotifyContentChanged("<p>Great season!</p>"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Article.Title.ShouldBe("Season Recap 2026");
        capturedRequest.Article.Content.ShouldBe("<p>Great season!</p>");
        capturedRequest.Article.TournamentId.ShouldBe("tournament-1");

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/news/season-recap-2026");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when creation fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenCreationFails()
    {
        // Arrange
        using var response = new StubApiResponse<ArticleResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict,
            Content = null
        };

        _mockNewsApi
            .Setup(x => x.CreateArticleAsync(It.IsAny<CreateArticleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderCreateArticle();
        await cut.InvokeAsync(() => cut.Find("#title").Change("Season Recap 2026"));
        await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.NotifyContentChanged("<p>Great season!</p>"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Create Article");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldNotContain("/news/season-recap-2026");
    }

    [Fact(DisplayName = "Should include the uploaded header image and attachments in the submitted request")]
    public async Task Submit_ShouldIncludeHeaderImageAndAttachments_WhenFilesWereUploaded()
    {
        // Arrange
        var headerUpload = UploadedFileResponseFactory.Create(container: "news", path: "header.png", fileName: "header.png");
        var attachmentUpload = UploadedFileResponseFactory.Create(container: "news", path: "attachment.pdf", fileName: "attachment.pdf");

        using var headerImageResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = headerUpload
        };
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = attachmentUpload
        };
        using var createResponse = new StubApiResponse<ArticleResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = ArticleResponseFactory.Create()
        };

        CreateArticleRequest? capturedRequest = null;

        _mockNewsApi
            .Setup(x => x.UploadArticleHeaderImageAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(headerImageResponse);
        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);
        _mockNewsApi
            .Setup(x => x.CreateArticleAsync(It.IsAny<CreateArticleRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateArticleRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(createResponse);

        var cut = RenderCreateArticle();
        await cut.InvokeAsync(() => cut.Find("#title").Change("Season Recap 2026"));
        await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.NotifyContentChanged("<p>Great season!</p>"));

        var fileUploaders = cut.FindComponents<FileUpload>();
        var headerImageInput = fileUploaders[0].FindComponent<InputFile>();
        var attachmentInput = fileUploaders[1].FindComponent<InputFile>();

        await cut.InvokeAsync(() => headerImageInput.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        await cut.InvokeAsync(() => attachmentInput.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "attachment.pdf", contentType: "application/pdf")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(2));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Article.HeaderImage.ShouldNotBeNull();
        capturedRequest.Article.HeaderImage.Path.ShouldBe("header.png");
        capturedRequest.Article.Attachments.Count.ShouldBe(1);
        capturedRequest.Article.Attachments.Single().Path.ShouldBe("attachment.pdf");
        capturedRequest.Article.Attachments.Single().IsInline.ShouldBeFalse();
    }

    // ── Attachment removal ───────────────────────────────────────────────────

    [Fact(DisplayName = "Should remove a non-inline attachment immediately without a confirmation prompt")]
    public async Task RemoveAttachment_ShouldRemoveImmediately_WhenAttachmentIsNotInline()
    {
        // Arrange
        var upload = UploadedFileResponseFactory.Create(fileName: "attachment.pdf");
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();
        var attachmentUploader = cut.FindComponents<FileUpload>()[1].FindComponent<InputFile>();

        await cut.InvokeAsync(() => attachmentUploader.UploadFiles(
            InputFileContent.CreateFromBinary([1], "attachment.pdf", contentType: "application/pdf")));
        await cut.WaitForAssertionAsync(() => cut.FindAll("ul.create-article-attachment-list li").Count.ShouldBe(1));

        // Act
        await cut.InvokeAsync(() => cut.Find("button.create-article-attachment-action.neba-btn-danger").Click());

        // Assert
        cut.FindAll("ul.create-article-attachment-list").ShouldBeEmpty();
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should give each attachment row's Download, Open, and Remove actions an accessible name naming the file")]
    public async Task Render_ShouldGiveAttachmentActionsAccessibleNamesNamingFile_WhenAttachmentUploaded()
    {
        // Arrange
        var upload = UploadedFileResponseFactory.Create(fileName: "roster.pdf");
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();
        var attachmentUploader = cut.FindComponents<FileUpload>()[1].FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => attachmentUploader.UploadFiles(
            InputFileContent.CreateFromBinary([1], "roster.pdf", contentType: "application/pdf")));
        await cut.WaitForAssertionAsync(() => cut.FindAll("ul.create-article-attachment-list li").Count.ShouldBe(1));

        // Assert
        var row = cut.Find("li.create-article-attachment-row");
        row.QuerySelector("a.neba-btn-secondary[download]")!.GetAttribute("aria-label").ShouldBe("Download roster.pdf");
        row.QuerySelector("a[target='_blank']")!.GetAttribute("aria-label").ShouldBe("Open roster.pdf");
        row.QuerySelector("button.neba-btn-danger")!.GetAttribute("aria-label").ShouldBe("Remove roster.pdf");
    }

    [Fact(DisplayName = "Should prompt for confirmation and remove the attachment when confirmed for an inline image")]
    public async Task RemoveAttachment_ShouldPromptAndRemoveOnConfirm_WhenAttachmentIsInline()
    {
        // Arrange
        var upload = UploadedFileResponseFactory.Create(fileName: "inline.png");
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();

        await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.RequestImageEmbedAsync(
            new FakeJSStreamReference(), "inline.png", "image/png"));
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("inline.png"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button.create-article-attachment-action.neba-btn-danger").Click());

        // Assert
        cut.Markup.ShouldContain("Remove embedded image?");

        await cut.InvokeAsync(() => cut.Find("button.confirm-action-modal-confirm").Click());
        cut.Markup.ShouldNotContain("inline.png");
    }

    [Fact(DisplayName = "Should keep the inline attachment when the removal confirmation is cancelled")]
    public async Task RemoveAttachment_ShouldKeepAttachment_WhenRemovalConfirmationCancelled()
    {
        // Arrange
        var upload = UploadedFileResponseFactory.Create(fileName: "inline.png");
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();

        await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.RequestImageEmbedAsync(
            new FakeJSStreamReference(), "inline.png", "image/png"));
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("inline.png"));

        await cut.InvokeAsync(() => cut.Find("button.create-article-attachment-action.neba-btn-danger").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("button.confirm-action-modal-cancel").Click());

        // Assert
        cut.Markup.ShouldContain("inline.png");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    // ── Inline image embed via the rich text editor ─────────────────────────

    [Fact(DisplayName = "Should upload the image, track it as an inline attachment, and return its URL when the editor requests an image embed")]
    public async Task RequestImageEmbed_ShouldUploadAndReturnUrl_WhenUploadSucceeds()
    {
        // Arrange
        var upload = UploadedFileResponseFactory.Create(fileName: "diagram.png", url: new Uri("https://storage.example.com/news/diagram.png"));
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = upload
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();

        // Act
        var result = await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.RequestImageEmbedAsync(
            new FakeJSStreamReference(), "diagram.png", "image/png"));

        // Assert
        result.ShouldBe("https://storage.example.com/news/diagram.png");
        cut.Markup.ShouldContain("diagram.png");
        cut.Markup.ShouldContain("Used in article body");
    }

    [Fact(DisplayName = "Should show an error toast and return null when the editor's image upload fails")]
    public async Task RequestImageEmbed_ShouldShowErrorToastAndReturnNull_WhenUploadFails()
    {
        // Arrange
        using var attachmentResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.UnsupportedMediaType,
            Content = null
        };

        _mockNewsApi
            .Setup(x => x.UploadArticleAttachmentAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachmentResponse);

        var cut = RenderCreateArticle();

        // Act
        var result = await cut.InvokeAsync(() => cut.FindComponent<RichTextEditor>().Instance.RequestImageEmbedAsync(
            new FakeJSStreamReference(), "diagram.png", "image/png"));

        // Assert
        result.ShouldBeNull();
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
        cut.Markup.ShouldNotContain("diagram.png");
    }

    private IRenderedComponent<CreateArticle> RenderCreateArticle()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/news/new?tournamentId=tournament-1");

        return _ctx.Render<CreateArticle>();
    }

    private IRenderedComponent<CreateArticle> RenderWithoutDeepLink()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/news/new");

        return _ctx.Render<CreateArticle>();
    }

    /// <summary>
    /// Minimal <see cref="IJSStreamReference"/> stand-in — this test project has no Moq dependency for
    /// this interface's small surface, matching the pattern already used by RichTextEditorTests.
    /// </summary>
    private sealed class FakeJSStreamReference : IJSStreamReference
    {
        public long Length => 3;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}