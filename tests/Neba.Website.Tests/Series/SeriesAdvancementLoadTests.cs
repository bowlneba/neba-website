using System.Net;

using Bunit;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementLoadTests : IDisposable
{
    private const string SeriesId = SeriesDetailHost.SeriesId;
    private const string ReloadFailedText = "The Stage status could not refresh. Reload the page to see the latest.";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private void SetupPage(SeriesAdvancementResponse? advancement = null)
        => _host.SetupDetailFor(advancement ?? DefaultAdvancement());

    private static SeriesAdvancementResponse DefaultAdvancement()
        => SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor"),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement"),
        ]);

    // ---- Gate ----

    [Fact(DisplayName = "Should make no advancement call and show the Stage lists for an anonymous visitor")]
    public void Gate_ShouldMakeNoCall_ForAnonymous()
    {
        // Arrange
        SetupPage();

        // Act
        var cut = _host.Render();

        // Assert
        _host.VerifyAdvancementCalls(Times.Never());
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(4);
    }

    [Fact(DisplayName = "Should make no advancement call for a signed-in user without Series.EditSeries")]
    public void Gate_ShouldMakeNoCall_WithoutEditSeries()
    {
        // Arrange
        SetupPage();
        _host.SignInWithoutEditSeries();

        // Act
        var cut = _host.Render();

        // Assert
        _host.VerifyAdvancementCalls(Times.Never());
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(4);
    }

    [Fact(DisplayName = "Should make no advancement call for a user who holds only Series.ManageAdvancement")]
    public void Gate_ShouldMakeNoCall_WithManageAdvancementOnly()
    {
        // Arrange
        SetupPage();
        _host.Auth.SetAuthorized("test-user");
        _host.Auth.SetPolicies(Permissions.ManageAdvancement.PolicyName);

        // Act
        var cut = _host.Render();

        // Assert
        _host.VerifyAdvancementCalls(Times.Never());
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should call the advancement API once and show the season rail for a Manager and an Admin")]
    [InlineData(false)]
    [InlineData(true)]
    public void Gate_ShouldCallOnce_ForManagerAndAdmin(bool admin)
    {
        // Arrange
        var advancement = DefaultAdvancement();
        SetupPage(advancement);
        _host.SetupAdvancement(advancement);
        if (admin)
        {
            _host.SignInAsAdmin();
        }
        else
        {
            _host.SignInAsManager();
        }

        // Act
        var cut = _host.Render();

        // Assert
        _host.VerifyAdvancementCalls(Times.Once());
        cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(4);
        cut.FindAll(".series-stage-list li").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-progress-loading']").ShouldBeEmpty();
        cut.Find("[data-testid='series-strip']").ShouldNotBeNull();
        cut.Find("[data-testid='series-progress-line']").ShouldNotBeNull();
    }

    // ---- Loading ----

    [Fact(DisplayName = "Should show the busy skeleton while the advancement call is pending, then the rail")]
    public void Load_ShouldShowSkeletonWhilePending()
    {
        // Arrange
        var advancement = DefaultAdvancement();
        SetupPage(advancement);
        var completion = new TaskCompletionSource<IApiResponse<SeriesAdvancementResponse>>();
        _host.SetupAdvancementPending(completion);
        _host.SignInAsManager();

        // Act
        var cut = _host.Render();
        var skeleton = cut.Find("[data-testid='series-progress-loading']");
        var busy = skeleton.GetAttribute("aria-busy");
        var cardsWhileLoading = cut.FindAll("[data-testid='series-stage-card']").Count;
        completion.SetResult(_host.OkResponse(advancement));

        // Assert
        busy.ShouldBe("true");
        cardsWhileLoading.ShouldBe(0);
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(4));
        cut.FindAll("[data-testid='series-progress-loading']").ShouldBeEmpty();
    }

    // ---- Failure ----

    [Fact(DisplayName = "Should show the reload-failed text with Retry and keep the Stage lists when the advancement call fails")]
    public void Failure_ShouldShowAlertAndKeepStageLists()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancementStatus(HttpStatusCode.InternalServerError);
        _host.SignInAsManager();

        // Act
        var cut = _host.Render();

        // Assert
        var failed = cut.Find("[data-testid='series-progress-failed']");
        failed.TextContent.ShouldContain(ReloadFailedText);
        failed.QuerySelector("button")!.TextContent.Trim().ShouldBe("Retry");
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(4);
        _host.Toasts.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Should call the advancement API again on Retry and then show the rail")]
    public async Task Failure_ShouldRetryAndShowRail()
    {
        // Arrange
        var advancement = DefaultAdvancement();
        SetupPage(advancement);
        _host.SetupAdvancementFailureThenSuccess(HttpStatusCode.InternalServerError, advancement);
        _host.SignInAsManager();
        var cut = _host.Render();

        // Act
        await cut.Find("[data-testid='series-progress-failed'] button").ClickAsync();

        // Assert
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        cut.FindAll("[data-testid='series-progress-failed']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(4);
        cut.FindAll(".series-stage-list li").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should hide the section with no toast when the advancement call returns 403")]
    public void Forbidden_ShouldHideSectionWithoutToast()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancementStatus(HttpStatusCode.Forbidden);
        _host.SignInAsManager();

        // Act
        var cut = _host.Render();

        // Assert
        _host.VerifyAdvancementCalls(Times.Once());
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-progress-failed']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(4);
        _host.Toasts.Current.ShouldBeNull();
    }

    // ---- Empty ----

    [Fact(DisplayName = "Should say the Series has no Stages yet when the advancement has zero Stages")]
    public void Empty_ShouldShowNoStagesMessage()
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(stages: [], positions: []);
        _host.SetupDetail(SeriesDetailResponseFactory.Create(id: SeriesId, stages: [SeriesStageResponseFactory.Create()]));
        _host.SetupAdvancement(advancement);
        _host.SignInAsManager();

        // Act
        var cut = _host.Render();

        // Assert
        cut.Find("[data-testid='series-progress']").TextContent.ShouldContain("This Series has no Stages yet.");
        cut.FindAll("[data-testid='series-strip']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-stage-card']").ShouldBeEmpty();
    }

    // ---- Reload ----

    [Fact(DisplayName = "Should not call the advancement API again when a logo save reloads the Series")]
    public async Task LogoSave_ShouldNotReloadAdvancement()
    {
        // Arrange
        var advancement = DefaultAdvancement();
        _host.SetupDetailFor(advancement, new Uri("https://storage.example.com/series/logo.png"));
        _host.SetupAdvancement(advancement);
        _host.Auth.SetAuthorized("test-user");
        _host.Auth.SetPolicies(Permissions.CanViewSeriesAdvancementPolicyName, Permissions.EditSeries.PolicyName, Permissions.EditSeriesLogo.PolicyName);
        using var saved = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent,
        };
        _host.Api
            .Setup(x => x.EditSeriesLogoAsync(SeriesId, It.IsAny<EditSeriesLogoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);
        var cut = _host.Render();
        await cut.Find("button.logo-edit-button").ClickAsync();
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        _host.Api.Verify(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _host.VerifyAdvancementCalls(Times.Once());
        cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(4);
    }
}