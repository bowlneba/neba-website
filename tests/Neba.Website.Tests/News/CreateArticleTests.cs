using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.News;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.News;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments;

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
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public CreateArticleTests()
    {
        _mockNewsApi = new Mock<INewsApi>(MockBehavior.Strict);
        _mockTournamentApiService = new Mock<ITournamentApiService>(MockBehavior.Strict);
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);

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
        _ctx.JSInterop.SetupModule("./js/browser-time.js");
        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CreateArticle.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockNewsApi.Object);
        _ctx.Services.AddSingleton(_mockTournamentApiService.Object);
        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
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

    private IRenderedComponent<CreateArticle> RenderCreateArticle()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/news/new?tournamentId=tournament-1");

        return _ctx.Render<CreateArticle>();
    }
}
