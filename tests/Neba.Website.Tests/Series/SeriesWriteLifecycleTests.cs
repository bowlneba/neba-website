using System.Net;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;

using Refit;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesWriteLifecycleTests : IDisposable
{
    private const string ConfirmButton = ".confirm-action-modal-confirm";
    private const string ApiText = "Stage 2 became official while you were looking.";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>Stage 1 and 2 Qualifying, 3 Survivor, 4 Placement; all Computed and none Official.</summary>
    private static SeriesAdvancementResponse Advancement(int version = 7)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Computed"),
            ]);

    /// <summary>The same Stages as <see cref="Advancement"/>, with the Stages at <paramref name="officialOrders"/> Official.</summary>
    private static SeriesAdvancementResponse AdvancementWithOfficial(int version, params int[] officialOrders)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: officialOrders.Contains(1) ? "Official" : "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: officialOrders.Contains(2) ? "Official" : "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: officialOrders.Contains(3) ? "Official" : "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Computed"),
            ]);

    private static StageSquadSummaryResponse[] Squads()
        =>
        [
            StageSquadSummaryResponseFactory.Create(squadId: "squad-current", isSelected: true),
            StageSquadSummaryResponseFactory.Create(squadId: "squad-other", isSelected: false),
        ];

    /// <summary>Renders the page as a Manager; the advancement calls return <paramref name="sequence"/> in order.</summary>
    private IRenderedComponent<SeriesDetail> RenderPage(params SeriesAdvancementResponse[] sequence)
    {
        _host.SignInAsManager();
        _host.SetupDetailFor(sequence[0]);
        _host.SetupAdvancementSequence(sequence);
        return _host.Render();
    }

    private static Task RecomputeAsync(IRenderedComponent<SeriesDetail> cut, string stageId)
        => SeriesDetailHost.Card(cut, stageId).QuerySelector("[data-testid='stage-recompute']")!.ClickAsync();

    private static async Task SaveSquadAsync(IRenderedComponent<SeriesDetail> cut, string squadId)
    {
        await SeriesDetailHost.Card(cut, "s1").QuerySelector("[data-testid='stage-pick-squad']")!.ClickAsync();
        await cut.Find($"[data-testid='stage-squad-option'][data-squad-id='{squadId}'] input")
            .ChangeAsync(new ChangeEventArgs { Value = squadId });
        await cut.Find("[data-testid='stage-squad-save']").ClickAsync();
    }

    private static IReadOnlyList<IElement> ActionButtons(IRenderedComponent<SeriesDetail> cut)
        => [.. cut.FindAll("[data-testid='stage-recompute'], [data-testid='stage-pick-squad']")];

    private void VerifyReloadedOnce()
    {
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        _host.VerifySeriesCalls(Times.Once());
    }

    // ---- Group 13: 2xx ----

    [Fact(DisplayName = "Should toast Recomputed and reload only the advancement after a recompute succeeds")]
    public async Task Recompute_ShouldToastAndReloadAdvancement_OnSuccess()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupRecompute("q2", _host.NoContent());

        // Act
        await RecomputeAsync(cut, "q2");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Recomputed");
        toast.Message.ShouldBe("Recomputed from Stage 2.");
        toast.Severity.ShouldBe(NotifySeverity.Success);
        VerifyReloadedOnce();
    }

    [Fact(DisplayName = "Should toast Squad saved and reload only the advancement after Pick squad succeeds")]
    public async Task PickSquad_ShouldToastAndReloadAdvancement_OnSuccess()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupSquads("s1", Squads());
        _host.SetupPickSquad("s1", _host.NoContent());

        // Act
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Squad Saved");
        toast.Message.ShouldBe("Squad saved.");
        toast.Severity.ShouldBe(NotifySeverity.Success);
        VerifyReloadedOnce();
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
    }

    // ---- Group 13: 409 ----

    [Fact(DisplayName = "Should toast the API text and reload when a recompute gets a 409 and no Stage is Official afterwards")]
    public async Task Recompute_ShouldToastApiTextAndReload_On409()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupRecompute("q1", await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText));

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Series Changed");
        toast.Message.ShouldBe(ApiText);
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        VerifyReloadedOnce();
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should reopen the confirmation with the fresh list when a 409 reload shows Official Stages from that order on")]
    public async Task Recompute_ShouldReopenConfirmation_WhenReloadShowsOfficialStages()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), AdvancementWithOfficial(8, 2, 3), Advancement(9));
        var attempts = 0;
        var conflict = await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText);
        var requests = _host.SetupRecompute("q1", () => Task.FromResult(attempts++ == 0 ? conflict : _host.NoContent()));

        // Act
        await RecomputeAsync(cut, "q1");
        var message = cut.Find(".neba-modal-content p").TextContent;
        var title = cut.Find(".neba-modal-title").TextContent;
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        title.ShouldBe("Recompute from Stage 1?");
        message.ShouldBe("Stages 2 and 3 are official. Recomputing resets them to Computed.");
        requests.Count.ShouldBe(2);
        requests[0].Recomputation.ConfirmResetOfficial.ShouldBeFalse();
        requests[0].Recomputation.Version.ShouldBe(7);
        requests[1].Recomputation.ConfirmResetOfficial.ShouldBeTrue();
        requests[1].Recomputation.Version.ShouldBe(8);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Exactly(3));
    }

    [Fact(DisplayName = "Should toast the API text, reload and close the dialog when Pick squad gets a 409")]
    public async Task PickSquad_ShouldToastReloadAndClose_On409()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupSquads("s1", Squads());
        _host.SetupPickSquad("s1", await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText));

        // Act
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Series Changed");
        toast.Message.ShouldBe(ApiText);
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        VerifyReloadedOnce();
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
    }

    // ---- Group 13: 422 and other failures ----

    [Fact(DisplayName = "Should keep the squad dialog open with the error text and make no reload when Pick squad gets a 422")]
    public async Task PickSquad_ShouldStayOpenWithError_On422()
    {
        // Arrange
        const string reason = "That squad does not belong to this tournament.";
        var cut = RenderPage(Advancement(7));
        _host.SetupSquads("s1", Squads());
        var requests = _host.SetupPickSquad("s1", await _host.RejectedWriteAsync(HttpStatusCode.UnprocessableEntity, reason));

        // Act
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        cut.Find("[data-testid='stage-squad-modal']").ShouldNotBeNull();
        cut.Find("[data-testid='stage-squad-error']").TextContent.ShouldContain(reason);
        cut.Find("[data-testid='stage-squad-save']").TextContent.Trim().ShouldBe("Use this squad");
        requests.ShouldHaveSingleItem();
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }

    [Theory(DisplayName = "Should toast Pick Squad Failed and close the dialog when Pick squad gets a 404 or another failure")]
    [InlineData(HttpStatusCode.NotFound, "The requested resource was not found.")]
    [InlineData(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")]
    [InlineData(HttpStatusCode.Forbidden, "An unexpected error occurred. Please try again.")]
    public async Task PickSquad_ShouldToastFailureAndClose_OnOtherFailure(HttpStatusCode status, string message)
    {
        // Arrange
        var cut = RenderPage(Advancement(7));
        _host.SetupSquads("s1", Squads());
        _host.SetupPickSquad("s1", _host.FailedWrite(status));

        // Act
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Pick Squad Failed");
        toast.Message.ShouldBe(message);
        toast.Severity.ShouldBe(NotifySeverity.Error);
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Theory(DisplayName = "Should toast Recompute Failed and make no reload when a recompute fails without a 409")]
    [InlineData(HttpStatusCode.NotFound, "The requested resource was not found.")]
    [InlineData(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")]
    public async Task Recompute_ShouldToastFailure_OnOtherFailure(HttpStatusCode status, string message)
    {
        // Arrange
        var cut = RenderPage(Advancement(7));
        _host.SetupRecompute("q1", _host.FailedWrite(status));

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Recompute Failed");
        toast.Message.ShouldBe(message);
        toast.Severity.ShouldBe(NotifySeverity.Error);
        _host.VerifyAdvancementCalls(Times.Once());
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should toast Recompute Failed with the API reason when a recompute gets a 422")]
    public async Task Recompute_ShouldToastApiReason_On422()
    {
        // Arrange
        const string reason = "The Series has no Stages to recompute.";
        var cut = RenderPage(Advancement(7));
        _host.SetupRecompute("q1", await _host.RejectedWriteAsync(HttpStatusCode.UnprocessableEntity, reason));

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Recompute Failed");
        toast.Message.ShouldBe(reason);
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Fact(DisplayName = "Should close the confirmation and toast Recompute Failed when a confirmed recompute fails")]
    public async Task Recompute_ShouldCloseConfirmation_WhenConfirmedRecomputeFails()
    {
        // Arrange
        var cut = RenderPage(AdvancementWithOfficial(7, 2));
        _host.SetupRecompute("q1", _host.FailedWrite(HttpStatusCode.InternalServerError));
        await RecomputeAsync(cut, "q1");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        _host.Toasts.Current.ShouldNotBeNull().Title.ShouldBe("Recompute Failed");
        _host.VerifyAdvancementCalls(Times.Once());
    }

    // ---- Group 13: a write in flight ----

    [Fact(DisplayName = "Should disable every action button while a recompute is pending and ignore a second click")]
    public async Task Recompute_ShouldDisableButtonsAndIgnoreSecondClick_WhilePending()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        var completion = new TaskCompletionSource<IApiResponse>();
        var requests = _host.SetupRecompute("q1", completion);

        // Act
        var click = RecomputeAsync(cut, "q1");
        await cut.WaitForAssertionAsync(() => ActionButtons(cut).ShouldAllBe(b => b.HasAttribute("disabled")));
        var buttonCount = ActionButtons(cut).Count;
        await SeriesDetailHost.Card(cut, "q2").QuerySelector("[data-testid='stage-recompute']")!.ClickAsync();
        await SeriesDetailHost.Card(cut, "q1").QuerySelector("[data-testid='stage-recompute']")!.ClickAsync();
        var callsWhilePending = requests.Count;
        completion.SetResult(_host.NoContent());
        await click;

        // Assert
        buttonCount.ShouldBe(5);
        callsWhilePending.ShouldBe(1);
        requests.Count.ShouldBe(1);
        await cut.WaitForAssertionAsync(() => ActionButtons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
    }

    [Fact(DisplayName = "Should show the busy label and disable the confirmation while a confirmed recompute is pending")]
    public async Task Recompute_ShouldShowBusyConfirmation_WhilePending()
    {
        // Arrange
        var cut = RenderPage(AdvancementWithOfficial(7, 2), Advancement(8));
        var completion = new TaskCompletionSource<IApiResponse>();
        var requests = _host.SetupRecompute("q1", completion);
        await RecomputeAsync(cut, "q1");

        // Act
        var click = cut.Find(ConfirmButton).ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Find(ConfirmButton).HasAttribute("disabled").ShouldBeTrue());
        var label = cut.Find(ConfirmButton).TextContent.Trim();
        var cancelDisabled = cut.Find(".confirm-action-modal-cancel").HasAttribute("disabled");
        await cut.Find(ConfirmButton).ClickAsync();
        completion.SetResult(_host.NoContent());
        await click;

        // Assert
        label.ShouldBe("Recomputing...");
        cancelDisabled.ShouldBeTrue();
        requests.ShouldHaveSingleItem();
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should disable the dialog controls and ignore a Recompute click while a squad save is pending")]
    public async Task PickSquad_ShouldDisableDialogAndIgnoreRecompute_WhilePending()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupSquads("s1", Squads());
        var completion = new TaskCompletionSource<IApiResponse>();
        var requests = _host.SetupPickSquad("s1", completion);
        var recomputes = _host.SetupRecompute("q1", _host.NoContent());
        await SeriesDetailHost.Card(cut, "s1").QuerySelector("[data-testid='stage-pick-squad']")!.ClickAsync();
        await cut.Find("[data-testid='stage-squad-option'][data-squad-id='squad-other'] input")
            .ChangeAsync(new ChangeEventArgs { Value = "squad-other" });

        // Act
        var click = cut.Find("[data-testid='stage-squad-save']").ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Find("[data-testid='stage-squad-save']").TextContent.Trim().ShouldBe("Saving..."));
        var saveDisabled = cut.Find("[data-testid='stage-squad-save']").HasAttribute("disabled");
        var cancelDisabled = cut.Find("[data-testid='stage-squad-cancel']").HasAttribute("disabled");
        await RecomputeAsync(cut, "q1");
        await cut.Find("[data-testid='stage-squad-save']").ClickAsync();
        completion.SetResult(_host.NoContent());
        await click;

        // Assert
        saveDisabled.ShouldBeTrue();
        cancelDisabled.ShouldBeTrue();
        requests.ShouldHaveSingleItem();
        recomputes.ShouldBeEmpty();
        await cut.WaitForAssertionAsync(() => ActionButtons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
    }

    [Theory(DisplayName = "Should render the Recompute and Pick squad buttons disabled while a squad save is pending, and enabled after it succeeds or fails")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PickSquad_ShouldDisableCardButtons_WhilePending_ThenEnable(bool fails)
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        _host.SetupSquads("s1", Squads());
        var completion = new TaskCompletionSource<IApiResponse>();
        _host.SetupPickSquad("s1", completion);
        await SeriesDetailHost.Card(cut, "s1").QuerySelector("[data-testid='stage-pick-squad']")!.ClickAsync();
        await cut.Find("[data-testid='stage-squad-option'][data-squad-id='squad-other'] input")
            .ChangeAsync(new ChangeEventArgs { Value = "squad-other" });

        // Act
        var click = cut.Find("[data-testid='stage-squad-save']").ClickAsync();
        await cut.WaitForAssertionAsync(() => ActionButtons(cut).ShouldAllBe(b => b.HasAttribute("disabled")));
        var disabledWhilePending = ActionButtons(cut).Count;
        completion.SetResult(fails ? _host.FailedWrite(HttpStatusCode.InternalServerError) : _host.NoContent());
        await click;

        // Assert
        disabledWhilePending.ShouldBe(5);
        await cut.WaitForAssertionAsync(() => ActionButtons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
    }

    // ---- Group 13: buttons come back after a failure ----

    [Theory(DisplayName = "Should enable the action buttons again after a recompute fails with a 500, a 409 or an exception")]
    [InlineData("500")]
    [InlineData("409")]
    [InlineData("throws")]
    public async Task Recompute_ShouldEnableButtons_AfterFailure(string failure)
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8));
        switch (failure)
        {
            case "500":
                _host.SetupRecompute("q1", _host.FailedWrite(HttpStatusCode.InternalServerError));
                break;
            case "409":
                _host.SetupRecompute("q1", await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText));
                break;
            default:
                _host.SetupRecompute("q1", () => Task.FromException<IApiResponse>(new HttpRequestException("The connection dropped.")));
                break;
        }

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        ActionButtons(cut).Count.ShouldBe(5);
        ActionButtons(cut).ShouldAllBe(b => !b.HasAttribute("disabled"));
        _host.Toasts.Current.ShouldNotBeNull();
    }

    [Theory(DisplayName = "Should enable the action buttons again after Pick squad fails with a 500, a 422 or an exception")]
    [InlineData("500")]
    [InlineData("422")]
    [InlineData("throws")]
    public async Task PickSquad_ShouldEnableButtons_AfterFailure(string failure)
    {
        // Arrange
        var cut = RenderPage(Advancement(7));
        _host.SetupSquads("s1", Squads());
        switch (failure)
        {
            case "500":
                _host.SetupPickSquad("s1", _host.FailedWrite(HttpStatusCode.InternalServerError));
                break;
            case "422":
                _host.SetupPickSquad("s1", await _host.RejectedWriteAsync(HttpStatusCode.UnprocessableEntity, "Not a squad of this tournament."));
                break;
            default:
                _host.SetupPickSquad("s1", () => Task.FromException<IApiResponse>(new HttpRequestException("The connection dropped.")));
                break;
        }

        // Act
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        ActionButtons(cut).Count.ShouldBe(5);
        ActionButtons(cut).ShouldAllBe(b => !b.HasAttribute("disabled"));
    }

    // ---- Group 14: stale view ----

    [Fact(DisplayName = "Should send the Version from the reload between two recomputes, 7 then 8")]
    public async Task Version_ShouldComeFromTheReload_BetweenTwoRecomputes()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8), Advancement(9));
        var requests = _host.SetupRecompute("q1", _host.NoContent());

        // Act
        await RecomputeAsync(cut, "q1");
        await RecomputeAsync(cut, "q1");

        // Assert
        requests.Select(r => r.Recomputation.Version).ShouldBe([7, 8]);
        _host.VerifyAdvancementCalls(Times.Exactly(3));
    }

    [Fact(DisplayName = "Should send the Version from the reload to Pick squad after a recompute, 7 then 8")]
    public async Task Version_ShouldComeFromTheReload_BetweenRecomputeAndPickSquad()
    {
        // Arrange
        var cut = RenderPage(Advancement(7), Advancement(8), Advancement(9));
        var recomputes = _host.SetupRecompute("q1", _host.NoContent());
        _host.SetupSquads("s1", Squads());
        var squadWrites = _host.SetupPickSquad("s1", _host.NoContent());

        // Act
        await RecomputeAsync(cut, "q1");
        await SaveSquadAsync(cut, "squad-other");

        // Assert
        recomputes.ShouldHaveSingleItem().Recomputation.Version.ShouldBe(7);
        squadWrites.ShouldHaveSingleItem().Squad.Version.ShouldBe(8);
    }

    [Fact(DisplayName = "Should keep the old Version and data and toast Reload Failed when the reload after a write fails")]
    public async Task Version_ShouldStay_WhenReloadFails()
    {
        // Arrange
        _host.SignInAsManager();
        _host.SetupDetailFor(Advancement(7));
        _host.SetupAdvancementSequence(
            _host.OkResponse(Advancement(7)),
            _host.FailedAdvancementResponse(HttpStatusCode.InternalServerError),
            _host.OkResponse(Advancement(9)));
        var cut = _host.Render();
        var requests = _host.SetupRecompute("q1", _host.NoContent());
        var pillsBefore = cut.FindAll("[data-testid='stage-status']").Select(p => p.TextContent).ToList();

        // Act
        await RecomputeAsync(cut, "q1");
        var toast = _host.Toasts.Current;
        var cardsAfterFailedReload = cut.FindAll("[data-testid='series-stage-card']").Count;
        var pillsAfter = cut.FindAll("[data-testid='stage-status']").Select(p => p.TextContent).ToList();
        await RecomputeAsync(cut, "q1");

        // Assert
        toast.ShouldNotBeNull();
        toast.Title.ShouldBe("Reload Failed");
        toast.Message.ShouldBe("The Stage status could not refresh. Reload the page to see the latest.");
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        cardsAfterFailedReload.ShouldBe(4);
        pillsAfter.ShouldBe(pillsBefore);
        requests.Select(r => r.Recomputation.Version).ShouldBe([7, 7]);
    }

    // ---- Override controls by permission ----

    [Theory(DisplayName = "Should render Add bowler and Remove override on the Qualifying Stage for an Admin and no override control for a Manager")]
    [InlineData(false, 0, 0)]
    [InlineData(true, 1, 2)]
    public async Task Overrides_ShouldGateControls_ByPermission(bool admin, int expectedAddButtons, int expectedRemoveButtons)
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(
            stages:
            [
                StageAdvancementResponseFactory.Create(
                    stageId: "q1",
                    order: 1,
                    status: "Computed",
                    overrides: [StageOverrideResponseFactory.Create(kind: "Add"), StageOverrideResponseFactory.Create(overrideId: "ov-2", kind: "Clear")]),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed"),
            ]);
        if (admin)
        {
            _host.SignInAsAdmin();
        }
        else
        {
            _host.SignInAsManager();
        }

        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        var cut = _host.Render();

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");
        await SeriesDetailHost.ExpandAsync(cut, "s1");

        // Assert
        cut.FindAll("[data-testid='stage-output']").Count.ShouldBe(2);
        var labels = cut.FindAll("button")
            .Select(b => (b.TextContent + " " + b.GetAttribute("aria-label")).ToUpperInvariant())
            .ToList();
        labels.Count(l => l.Contains("ADD BOWLER", StringComparison.Ordinal)).ShouldBe(expectedAddButtons);
        labels.Count(l => l.Contains("REMOVE OVERRIDE", StringComparison.Ordinal)).ShouldBe(expectedRemoveButtons);
        labels.ShouldNotContain(l => l.Contains("CLEAR SLOT", StringComparison.Ordinal));
        SeriesDetailHost.Card(cut, "s1").QuerySelectorAll("[data-testid='stage-output'] button").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-overrides']").Count.ShouldBe(1);
    }
}