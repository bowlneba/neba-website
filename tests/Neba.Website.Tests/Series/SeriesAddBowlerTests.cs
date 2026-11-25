using System.Net;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;

using Refit;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAddBowlerTests : IDisposable
{
    private const string AddButton = "[data-testid='stage-add-bowler']";
    private const string Modal = "[data-testid='stage-override-modal']";
    private const string Save = "[data-testid='stage-override-save']";
    private const string Reason = "[data-testid='stage-override-reason']";
    private const string DiscardConfirm = ".confirm-action-modal-confirm";
    private const string DiscardCancel = ".confirm-action-modal-cancel";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>
    /// Two Computed Qualifying Stages, a Survivor and a Placement Stage. Alice is in Stage 1's field, Bea in Stage 2's,
    /// and Stage 2 holds an Add override for Dee.
    /// </summary>
    private static SeriesAdvancementResponse Advancement(int version = 7)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(
                    stageId: "q1",
                    order: 1,
                    status: "Computed",
                    entries: [FieldEntryResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", sourceStageId: "q1", place: 1)],
                    overrides: []),
                StageAdvancementResponseFactory.Create(
                    stageId: "q2",
                    order: 2,
                    status: "Computed",
                    entries: [FieldEntryResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", sourceStageId: "q2", place: 1)],
                    overrides: [StageOverrideResponseFactory.Create(overrideId: "ov-q2", bowlerId: "b4", bowlerName: "Dee Dunn", kind: "Add")]),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Pending"),
            ]);

    private static StageResultSummaryResponse[] Rows()
        =>
        [
            StageResultSummaryResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", place: 1),
            StageResultSummaryResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", place: 2),
            StageResultSummaryResponseFactory.Create(bowlerId: "b3", bowlerName: "Cara Cole", place: 3),
            StageResultSummaryResponseFactory.Create(bowlerId: "b4", bowlerName: "Dee Dunn", place: 4),
            StageResultSummaryResponseFactory.Create(bowlerId: "b5", bowlerName: "Eve Ellis", place: 5),
        ];

    /// <summary>Renders the page as an Admin; the advancement calls return <paramref name="sequence"/> in order.</summary>
    private IRenderedComponent<SeriesDetail> RenderPage(params SeriesAdvancementResponse[] sequence)
    {
        _host.SignInAsAdmin();
        _host.SetupDetailFor(sequence[0]);
        _host.SetupAdvancementSequence(sequence);
        return _host.Render();
    }

    private static async Task OpenAddAsync(IRenderedComponent<SeriesDetail> cut, string stageId)
    {
        if (SeriesDetailHost.Card(cut, stageId).QuerySelector("[data-testid='stage-output']") is null)
        {
            await SeriesDetailHost.ExpandAsync(cut, stageId);
        }

        await SeriesDetailHost.Card(cut, stageId).QuerySelector(AddButton)!.ClickAsync();
    }

    private static Task FocusPickerAsync(IRenderedComponent<SeriesDetail> cut)
        => cut.InvokeAsync(() => cut.Find("#override-bowler").TriggerEvent("onfocus", new FocusEventArgs()));

    private static string[] OptionLabels(IRenderedComponent<SeriesDetail> cut)
        => [.. cut.FindAll(".neba-autocomplete-option").Select(o => o.TextContent.Trim())];

    private static async Task PickBowlerAsync(IRenderedComponent<SeriesDetail> cut, string label)
    {
        await FocusPickerAsync(cut);
        await cut.FindAll(".neba-autocomplete-option").Single(o => o.TextContent.Trim() == label).ClickAsync();
    }

    private static Task TypeReasonAsync(IRenderedComponent<SeriesDetail> cut, string text)
        => cut.InvokeAsync(() => cut.Find(Reason).Input(text));

    private static bool SaveDisabled(IRenderedComponent<SeriesDetail> cut) => cut.Find(Save).HasAttribute("disabled");

    private static IElement? ReasonError(IRenderedComponent<SeriesDetail> cut) => cut.FindAll("#override-reason-error").SingleOrDefault();

    private static Task CancelAsync(IRenderedComponent<SeriesDetail> cut) => cut.Find("[data-testid='stage-override-cancel']").ClickAsync();

    /// <summary>The path Escape, the backdrop and the X take: the dialog's own OnClose.</summary>
    private static Task RequestCloseAsync(IRenderedComponent<SeriesDetail> cut)
    {
        var modal = cut.FindComponents<NebaModal>().Single(m => m.Instance.AriaLabelledById == "stage-override-title");
        return cut.InvokeAsync(() => modal.Instance.OnClose.InvokeAsync());
    }

    // ---- Opening ----

    [Fact(DisplayName = "Should open the dialog in Add mode with the loading skeleton and load the Stage's results once")]
    public async Task Open_ShouldShowLoadingSkeleton_AndLoadResultsOnce()
    {
        // Arrange
        var completion = new TaskCompletionSource<IApiResponse<CollectionResponse<StageResultSummaryResponse>>>();
        _host.SetupResultsPending("q1", completion);
        var cut = RenderPage(Advancement());
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Act
        var click = SeriesDetailHost.Card(cut, "q1").QuerySelector(AddButton)!.ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Find("[data-testid='stage-override-loading']"));
        var busy = cut.Find("[data-testid='stage-override-loading']").GetAttribute("aria-busy");
        var kind = cut.Find(Modal).GetAttribute("data-kind");
        var pickerWhileLoading = cut.FindAll("[data-testid='stage-override-bowler']").Count;
        completion.SetResult(_host.ResultsResponse(Rows()));
        await click;

        // Assert
        busy.ShouldBe("true");
        kind.ShouldBe("Add");
        pickerWhileLoading.ShouldBe(0);
        cut.FindAll("[data-testid='stage-override-loading']").ShouldBeEmpty();
        cut.Find("[data-testid='stage-override-bowler']").ShouldNotBeNull();
        cut.FindAll("[data-testid='stage-override-bowler-fixed']").ShouldBeEmpty();
        _host.VerifyResultsCalls("q1", Times.Once());
        _host.VerifyAnyResultsCalls(Times.Once());
    }

    [Fact(DisplayName = "Should title and label the dialog for Add bowler and describe the reason box")]
    public async Task Open_ShouldLabelDialog_AndDescribeReason()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());

        // Act
        await OpenAddAsync(cut, "q1");

        // Assert
        cut.Find("#stage-override-title").TextContent.ShouldBe("Add bowler");
        cut.Find("[role='dialog']").GetAttribute("aria-labelledby").ShouldBe("stage-override-title");
        cut.Find("[data-testid='stage-override-lead']").TextContent
            .ShouldBe("1st tournament, Tournament 1. Pick any bowler with a result in this tournament, whatever the place or membership. The field can then be larger than the target.");
        cut.Find("#override-bowler").GetAttribute("role").ShouldBe("combobox");
        cut.Find(Reason).GetAttribute("id").ShouldBe("override-reason");
        cut.Find(Reason).GetAttribute("aria-describedby").ShouldBe("override-reason-hint override-reason-error");
        cut.Find(Save).TextContent.Trim().ShouldBe("Add bowler");
        cut.Find("[data-testid='stage-override-count']").TextContent.ShouldBe("0 / 500");
        SaveDisabled(cut).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should make no second results call when the same Stage reopens")]
    public async Task Reopen_ShouldReuseResults_ForSameStage()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await CancelAsync(cut);

        // Act
        await OpenAddAsync(cut, "q1");

        // Assert
        _host.VerifyResultsCalls("q1", Times.Once());
        _host.VerifyAnyResultsCalls(Times.Once());
        cut.Find("[data-testid='stage-override-bowler']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should call the results endpoint again for a different Stage")]
    public async Task Open_ShouldLoadResultsAgain_ForDifferentStage()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        _host.SetupResults("q2", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await CancelAsync(cut);

        // Act
        await OpenAddAsync(cut, "q2");

        // Assert
        _host.VerifyResultsCalls("q1", Times.Once());
        _host.VerifyResultsCalls("q2", Times.Once());
        _host.VerifyAnyResultsCalls(Times.Exactly(2));
    }

    [Fact(DisplayName = "Should show the load error without a picker when the results fail, then retry when the dialog reopens")]
    public async Task Open_ShouldShowLoadError_ThenRetryOnReopen()
    {
        // Arrange
        _host.SetupResultsSequence("q1", _host.FailedResultsResponse(HttpStatusCode.InternalServerError), _host.ResultsResponse(Rows()));
        var cut = RenderPage(Advancement());

        // Act
        await OpenAddAsync(cut, "q1");
        var errorText = cut.Find("[data-testid='stage-override-load-error']").TextContent;
        var pickerAfterFailure = cut.FindAll("[data-testid='stage-override-bowler']").Count;
        var loadingAfterFailure = cut.FindAll("[data-testid='stage-override-loading']").Count;
        var saveDisabledAfterFailure = SaveDisabled(cut);
        await CancelAsync(cut);
        await OpenAddAsync(cut, "q1");

        // Assert
        errorText.ShouldContain("An unexpected error occurred. Please try again.");
        pickerAfterFailure.ShouldBe(0);
        loadingAfterFailure.ShouldBe(0);
        saveDisabledAfterFailure.ShouldBeTrue();
        cut.FindAll("[data-testid='stage-override-load-error']").ShouldBeEmpty();
        cut.Find("[data-testid='stage-override-bowler']").ShouldNotBeNull();
        _host.VerifyResultsCalls("q1", Times.Exactly(2));
    }

    // ---- Candidates ----

    [Fact(DisplayName = "Should list only bowlers who are in no Stage's field, so Alice and Bea are left out of Stage 1")]
    public async Task Candidates_ShouldExcludeBowlersInAnyField()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");

        // Act
        await FocusPickerAsync(cut);

        // Assert
        OptionLabels(cut).ShouldBe(["Cara Cole (3rd)", "Dee Dunn (4th)", "Eve Ellis (5th)"]);
    }

    [Fact(DisplayName = "Should also leave out a bowler who already has an override on that Stage, so Dee is left out of Stage 2")]
    public async Task Candidates_ShouldExcludeBowlersWithAnOverrideOnTheStage()
    {
        // Arrange
        _host.SetupResults("q2", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q2");

        // Act
        await FocusPickerAsync(cut);

        // Assert
        OptionLabels(cut).ShouldBe(["Cara Cole (3rd)", "Eve Ellis (5th)"]);
    }

    // ---- Save gating and the reason ----

    [Fact(DisplayName = "Should keep Save disabled until a bowler is picked and the reason has text, and block a whitespace reason")]
    public async Task Save_ShouldBeDisabled_UntilBowlerAndReason()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        var atStart = SaveDisabled(cut);

        // Act
        await TypeReasonAsync(cut, "Confirmed by the director.");
        var reasonOnly = SaveDisabled(cut);
        await TypeReasonAsync(cut, string.Empty);
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        var bowlerOnly = SaveDisabled(cut);
        await TypeReasonAsync(cut, "   ");
        var whitespaceReason = SaveDisabled(cut);
        await TypeReasonAsync(cut, "Confirmed by the director.");
        var both = SaveDisabled(cut);

        // Assert
        atStart.ShouldBeTrue();
        reasonOnly.ShouldBeTrue();
        bowlerOnly.ShouldBeTrue();
        whitespaceReason.ShouldBeTrue();
        both.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should show the required-reason message in an alert when the reason is emptied")]
    public async Task Reason_ShouldShowRequiredMessage_WhenEmptied()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        var beforeTyping = ReasonError(cut);

        // Act
        await TypeReasonAsync(cut, "Something");
        await TypeReasonAsync(cut, string.Empty);

        // Assert
        beforeTyping.ShouldBeNull();
        var error = ReasonError(cut).ShouldNotBeNull();
        error.TextContent.ShouldBe("A reason is required.");
        error.GetAttribute("role").ShouldBe("alert");
    }

    [Fact(DisplayName = "Should turn the count red, show N / 500, show the length message and keep Save disabled for a 501-character reason")]
    public async Task Reason_ShouldFlagOverLimit()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");

        // Act
        await TypeReasonAsync(cut, new string('x', 500));
        var atLimit = (Text: cut.Find("[data-testid='stage-override-count']").TextContent, Over: cut.Find("[data-testid='stage-override-count']").ClassList.Contains("stage-override__count--over"), Disabled: SaveDisabled(cut));
        await TypeReasonAsync(cut, new string('x', 501));

        // Assert
        atLimit.ShouldBe(("500 / 500", false, false));
        var count = cut.Find("[data-testid='stage-override-count']");
        count.TextContent.ShouldBe("501 / 500");
        count.ClassList.ShouldContain("stage-override__count--over");
        SaveDisabled(cut).ShouldBeTrue();
        ReasonError(cut).ShouldNotBeNull().TextContent.ShouldBe("The reason must be 500 characters or fewer.");
    }

    // ---- Submit ----

    [Fact(DisplayName = "Should send Kind 1, the bowler, the trimmed reason and the page Version when Save is pressed")]
    public async Task Submit_ShouldSendKindBowlerTrimmedReasonAndVersion()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var requests = _host.SetupCreateOverride("q1", _host.Created());
        var cut = RenderPage(Advancement(7), Advancement(8));
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "  Confirmed by the director.  ");

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.Id.ShouldBe(SeriesDetailHost.SeriesId);
        request.StageId.ShouldBe("q1");
        request.StageOverride.Kind.ShouldBe(1);
        request.StageOverride.BowlerId.ShouldBe("b3");
        request.StageOverride.Reason.ShouldBe("Confirmed by the director.");
        request.StageOverride.Version.ShouldBe(7);
    }

    [Fact(DisplayName = "Should toast Bowler Added, reload the advancement once and close the dialog after a successful Add")]
    public async Task Submit_ShouldToastReloadAndClose_OnSuccess()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        _host.SetupCreateOverride("q1", _host.Created());
        var cut = RenderPage(Advancement(7), Advancement(8));
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "Confirmed by the director.");

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Bowler Added");
        toast.Message.ShouldBe("Cara Cole added to the 1st tournament.");
        toast.Severity.ShouldBe(NotifySeverity.Success);
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        _host.VerifySeriesCalls(Times.Once());
        cut.FindAll(Modal).ShouldBeEmpty();
        cut.FindAll(DiscardConfirm).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should send the reloaded Version, not the one in the Create response, on the next write")]
    public async Task Submit_ShouldSendReloadedVersion_OnTheNextWrite()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var requests = _host.SetupCreateOverride("q1", _host.Created(version: 500));
        var cut = RenderPage(Advancement(7), Advancement(8), Advancement(9));
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "First.");
        await cut.Find(Save).ClickAsync();

        // Act
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Eve Ellis (5th)");
        await TypeReasonAsync(cut, "Second.");
        await cut.Find(Save).ClickAsync();

        // Assert
        requests.Select(r => r.StageOverride.Version).ShouldBe([7, 8]);
        requests.Select(r => r.StageOverride.BowlerId).ShouldBe(["b3", "b5"]);
        _host.VerifyResultsCalls("q1", Times.Once());
        _host.VerifyAdvancementCalls(Times.Exactly(3));
    }

    [Fact(DisplayName = "Should not submit when the form is submitted with Enter")]
    public async Task Enter_ShouldNotSubmit()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "Confirmed by the director.");

        // Act
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        _host.VerifyAnyCreateCalls(Times.Never());
        cut.Find(Modal).ShouldNotBeNull();
        SaveDisabled(cut).ShouldBeFalse();
    }

    // ---- Cancel and the dirty guard ----

    [Fact(DisplayName = "Should close with no call and no confirmation when Cancel is pressed with nothing typed")]
    public async Task Cancel_ShouldCloseWithoutCall_WhenNothingTyped()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");

        // Act
        await CancelAsync(cut);

        // Assert
        cut.FindAll(Modal).ShouldBeEmpty();
        cut.FindAll(DiscardConfirm).ShouldBeEmpty();
        _host.VerifyAnyCreateCalls(Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Should ask to discard when the dialog asks to close after a reason is typed")]
    public async Task Close_ShouldAskToDiscard_WhenReasonTyped()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await TypeReasonAsync(cut, "Half a thought");

        // Act
        await RequestCloseAsync(cut);

        // Assert
        cut.Find(Modal).ShouldNotBeNull();
        var confirm = cut.FindComponents<ConfirmActionModal>().Single(c => c.Instance.IsOpen);
        confirm.Instance.Title.ShouldBe("Discard changes?");
        cut.Find(DiscardConfirm).TextContent.Trim().ShouldBe("Discard");
        cut.Find(DiscardCancel).TextContent.Trim().ShouldBe("Keep editing");
        _host.VerifyAnyCreateCalls(Times.Never());
    }

    [Fact(DisplayName = "Should keep the dialog and the typed text when Keep editing is pressed")]
    public async Task Discard_ShouldKeepText_WhenKeepEditing()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "Half a thought");
        await RequestCloseAsync(cut);

        // Act
        await cut.Find(DiscardCancel).ClickAsync();

        // Assert
        cut.Find(Modal).ShouldNotBeNull();
        cut.FindAll(DiscardConfirm).ShouldBeEmpty();
        cut.Find(Reason).GetAttribute("value").ShouldBe("Half a thought");
        cut.Find("[data-testid='stage-override-count']").TextContent.ShouldBe("14 / 500");
        cut.Find("#override-bowler").GetAttribute("value").ShouldBe("Cara Cole (3rd)");
        SaveDisabled(cut).ShouldBeFalse();
    }

    [Fact(DisplayName = "Should close the dialog with no call when Discard is pressed")]
    public async Task Discard_ShouldCloseWithoutCall_WhenDiscarded()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await TypeReasonAsync(cut, "Half a thought");
        await RequestCloseAsync(cut);

        // Act
        await cut.Find(DiscardConfirm).ClickAsync();

        // Assert
        cut.FindAll(Modal).ShouldBeEmpty();
        cut.FindAll(DiscardConfirm).ShouldBeEmpty();
        _host.VerifyAnyCreateCalls(Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Fact(DisplayName = "Should ask to discard when only a bowler is picked and guard the page while the form is dirty")]
    public async Task Dirty_ShouldBeSet_ByPickingABowlerAlone()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        var guard = cut.FindComponent<StageOverrideModal>().FindComponent<DirtyFormGuard>();
        var dirtyAtStart = guard.Instance.IsDirty;

        // Act
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        var dirtyAfterPick = guard.Instance.IsDirty;
        await CancelAsync(cut);

        // Assert
        dirtyAtStart.ShouldBeFalse();
        dirtyAfterPick.ShouldBeTrue();
        cut.Find(Modal).ShouldNotBeNull();
        cut.Find(DiscardConfirm).TextContent.Trim().ShouldBe("Discard");
    }

    [Fact(DisplayName = "Should start clean with no bowler, no reason and no message when the dialog reopens after a discard")]
    public async Task Reopen_ShouldStartClean()
    {
        // Arrange
        _host.SetupResults("q1", Rows());
        var cut = RenderPage(Advancement());
        await OpenAddAsync(cut, "q1");
        await PickBowlerAsync(cut, "Cara Cole (3rd)");
        await TypeReasonAsync(cut, "Something");
        await TypeReasonAsync(cut, string.Empty);
        await CancelAsync(cut);
        await cut.Find(DiscardConfirm).ClickAsync();

        // Act
        await OpenAddAsync(cut, "q1");

        // Assert
        cut.Find("#override-bowler").GetAttribute("value").ShouldBeNullOrEmpty();
        cut.Find(Reason).GetAttribute("value").ShouldBeNullOrEmpty();
        cut.Find("[data-testid='stage-override-count']").TextContent.ShouldBe("0 / 500");
        ReasonError(cut).ShouldBeNull();
        SaveDisabled(cut).ShouldBeTrue();
        cut.FindAll("[data-testid='stage-override-error']").ShouldBeEmpty();
        cut.FindAll(DiscardConfirm).ShouldBeEmpty();
        cut.FindComponent<StageOverrideModal>().FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeFalse();
    }
}