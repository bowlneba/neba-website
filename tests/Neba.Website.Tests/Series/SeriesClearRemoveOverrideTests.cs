using System.Net;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;

using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;

using Refit;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesClearRemoveOverrideTests : IDisposable
{
    private const string ApiText = "Stage 1 became official while you were looking.";
    private const string Modal = "[data-testid='stage-override-modal']";
    private const string Save = "[data-testid='stage-override-save']";
    private const string Reason = "[data-testid='stage-override-reason']";
    private const string ConfirmButton = ".confirm-action-modal-confirm";
    private const string CancelButton = ".confirm-action-modal-cancel";
    private const string AllButtons =
        "[data-testid='stage-recompute'], [data-testid='stage-pick-squad'], [data-testid='stage-add-bowler'], [data-testid='entry-clear'], [data-testid='override-remove']";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>
    /// Stage 1 is Computed with two entries it qualified and an Add and a Clear override; Stage 2 is a Survivor Stage and Stage 3 a Placement Stage.
    /// With Stage 1 expanded the page shows 9 action buttons: 3 Recompute, 1 Pick squad, 1 Add bowler, 2 Clear slot and 2 Remove override.
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
                    entries:
                    [
                        FieldEntryResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", sourceStageId: "q1", place: 1),
                        FieldEntryResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", sourceStageId: "q1", place: 2),
                    ],
                    overrides:
                    [
                        StageOverrideResponseFactory.Create(overrideId: "ov-add", bowlerId: "b3", bowlerName: "Cara Cole", kind: "Add"),
                        StageOverrideResponseFactory.Create(overrideId: "ov-clear", bowlerId: "b9", bowlerName: "Zed Zane", kind: "Clear"),
                    ]),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Computed"),
            ]);

    /// <summary>Renders the page as an Admin with Stage 1 expanded; the advancement calls return <paramref name="sequence"/> in order.</summary>
    private async Task<IRenderedComponent<SeriesDetail>> RenderExpandedAsync(params SeriesAdvancementResponse[] sequence)
    {
        _host.SignInAsAdmin();
        _host.SetupDetailFor(sequence[0]);
        _host.SetupAdvancementSequence(sequence);
        var cut = _host.Render();
        await SeriesDetailHost.ExpandAsync(cut, "q1");
        return cut;
    }

    private static Task ClearSlotAsync(IRenderedComponent<SeriesDetail> cut, string bowlerId)
        => cut.Find($"[data-testid='entry-clear'][data-bowler-id='{bowlerId}']").ClickAsync();

    private static Task RemoveOverrideAsync(IRenderedComponent<SeriesDetail> cut, string overrideId)
        => cut.Find($"[data-testid='override-remove'][data-override-id='{overrideId}']").ClickAsync();

    private static Task TypeReasonAsync(IRenderedComponent<SeriesDetail> cut, string text)
        => cut.InvokeAsync(() => cut.Find(Reason).Input(text));

    private static bool SaveDisabled(IRenderedComponent<SeriesDetail> cut) => cut.Find(Save).HasAttribute("disabled");

    private static IReadOnlyList<IElement> Buttons(IRenderedComponent<SeriesDetail> cut) => [.. cut.FindAll(AllButtons)];

    /// <summary>Clears Alice's slot with a reason, ready to press Save.</summary>
    private static async Task FillClearAsync(IRenderedComponent<SeriesDetail> cut)
    {
        await ClearSlotAsync(cut, "b1");
        await TypeReasonAsync(cut, "Withdrew before the Stage.");
    }

    /// <summary>The Remove confirmation's own component, so a test can raise its callbacks the way a second click would.</summary>
    private static IRenderedComponent<ConfirmActionModal> RemoveConfirm(IRenderedComponent<SeriesDetail> cut)
        => cut.FindComponents<ConfirmActionModal>().Single(c => c.Instance.Title == "Remove this override?");

    // ---- Clear slot ----

    [Fact(DisplayName = "Should open the dialog in Clear mode with the bowler fixed, no picker and no results call")]
    public async Task Clear_ShouldOpenWithFixedBowler_AndNoPicker()
    {
        // Arrange
        var cut = await RenderExpandedAsync(Advancement());

        // Act
        await ClearSlotAsync(cut, "b1");

        // Assert
        cut.Find(Modal).GetAttribute("data-kind").ShouldBe("Clear");
        cut.Find("#stage-override-title").TextContent.ShouldBe("Clear slot");
        cut.Find("[data-testid='stage-override-bowler-fixed']").TextContent.ShouldBe("Alice Adams · 1st place");
        cut.Find("[data-testid='stage-override-lead']").TextContent
            .ShouldBe("Stage 1, Tournament 1. The slot goes vacant and the Backfill fills it. This bowler can't come back through the Backfill.");
        cut.FindAll("[data-testid='stage-override-bowler']").ShouldBeEmpty();
        cut.FindAll("#override-bowler").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-override-loading']").ShouldBeEmpty();
        cut.Find(Save).TextContent.Trim().ShouldBe("Clear slot");
        _host.VerifyAnyResultsCalls(Times.Never());
    }

    [Fact(DisplayName = "Should keep Save disabled until the reason has text and block a whitespace reason in Clear mode")]
    public async Task Clear_ShouldRequireReason()
    {
        // Arrange
        var cut = await RenderExpandedAsync(Advancement());
        await ClearSlotAsync(cut, "b1");
        var atStart = SaveDisabled(cut);

        // Act
        await TypeReasonAsync(cut, "   ");
        var whitespace = SaveDisabled(cut);
        await TypeReasonAsync(cut, "Withdrew before the Stage.");
        var withReason = SaveDisabled(cut);

        // Assert
        atStart.ShouldBeTrue();
        whitespace.ShouldBeTrue();
        withReason.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should send Kind 2 with the entry's bowler, the trimmed reason and the page Version when Clear is saved")]
    public async Task Clear_ShouldSendKind2BowlerAndVersion()
    {
        // Arrange
        var requests = _host.SetupCreateOverride("q1", _host.Created());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await ClearSlotAsync(cut, "b2");
        await TypeReasonAsync(cut, "  Withdrew before the Stage.  ");

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.Id.ShouldBe(SeriesDetailHost.SeriesId);
        request.StageId.ShouldBe("q1");
        request.StageOverride.Kind.ShouldBe(2);
        request.StageOverride.BowlerId.ShouldBe("b2");
        request.StageOverride.Reason.ShouldBe("Withdrew before the Stage.");
        request.StageOverride.Version.ShouldBe(7);
    }

    [Fact(DisplayName = "Should toast the slot-cleared text, reload once and close the dialog after a successful Clear")]
    public async Task Clear_ShouldToastReloadAndClose_OnSuccess()
    {
        // Arrange
        _host.SetupCreateOverride("q1", _host.Created());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Slot Cleared");
        toast.Message.ShouldBe(SeriesDisplay.SlotClearedMessage);
        toast.Severity.ShouldBe(NotifySeverity.Success);
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        cut.FindAll(Modal).ShouldBeEmpty();
    }

    // ---- Remove override ----

    [Theory(DisplayName = "Should ask to confirm with the remove text for an Add override and for a Clear override")]
    [InlineData("ov-add", "Cara Cole leaves the field for Stage 1.")]
    [InlineData("ov-clear", "Zed Zane takes the slot back, and the Backfill no longer refills it.")]
    public async Task Remove_ShouldAskToConfirm_WithKindText(string overrideId, string message)
    {
        // Arrange
        var cut = await RenderExpandedAsync(Advancement());

        // Act
        await RemoveOverrideAsync(cut, overrideId);

        // Assert
        var confirm = RemoveConfirm(cut);
        confirm.Instance.IsOpen.ShouldBeTrue();
        confirm.Instance.Message.ShouldBe(message);
        confirm.Instance.ConfirmLabel.ShouldBe("Remove override");
        confirm.Instance.BusyLabel.ShouldBe("Removing...");
        cut.Find(ConfirmButton).TextContent.Trim().ShouldBe("Remove override");
        _host.VerifyAnyRemoveCalls(Times.Never());
    }

    [Fact(DisplayName = "Should close the confirmation and make no call when Remove is cancelled")]
    public async Task Remove_ShouldMakeNoCall_WhenCancelled()
    {
        // Arrange
        var cut = await RenderExpandedAsync(Advancement());
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(CancelButton).ClickAsync();

        // Assert
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        _host.VerifyAnyRemoveCalls(Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Should send the Stage, the override and the page Version when Remove is confirmed")]
    public async Task Remove_ShouldSendOverrideAndVersion_WhenConfirmed()
    {
        // Arrange
        var requests = _host.SetupRemoveOverride("q1", _host.NoContent());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await RemoveOverrideAsync(cut, "ov-clear");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        requests.ShouldHaveSingleItem().ShouldBe(("ov-clear", 7));
    }

    [Fact(DisplayName = "Should toast Override removed, reload once and close the confirmation after a successful Remove")]
    public async Task Remove_ShouldToastReloadAndClose_OnSuccess()
    {
        // Arrange
        _host.SetupRemoveOverride("q1", _host.NoContent());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Override Removed");
        toast.Message.ShouldBe("Override removed.");
        toast.Severity.ShouldBe(NotifySeverity.Success);
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should send the reloaded Version on a Remove that follows a Clear")]
    public async Task Remove_ShouldSendReloadedVersion_AfterAClear()
    {
        // Arrange
        _host.SetupCreateOverride("q1", _host.Created(version: 500));
        var removes = _host.SetupRemoveOverride("q1", _host.NoContent());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8), Advancement(9));
        await FillClearAsync(cut);
        await cut.Find(Save).ClickAsync();

        // Act
        await RemoveOverrideAsync(cut, "ov-add");
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        removes.ShouldHaveSingleItem().Version.ShouldBe(8);
    }

    // ---- 409 ----

    [Fact(DisplayName = "Should toast Series Changed with the API text, reload and close the dialog when Create gets a 409")]
    public async Task Create_ShouldToastReloadAndClose_On409()
    {
        // Arrange
        _host.SetupCreateOverride("q1", await _host.RejectedCreateAsync(HttpStatusCode.Conflict, ApiText));
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Series Changed");
        toast.Message.ShouldBe(ApiText);
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        cut.FindAll(Modal).ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-override-error']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should toast Series Changed with the API text, reload and close the confirmation when Remove gets a 409")]
    public async Task Remove_ShouldToastReloadAndClose_On409()
    {
        // Arrange
        _host.SetupRemoveOverride("q1", await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText));
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Series Changed");
        toast.Message.ShouldBe(ApiText);
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        _host.VerifyAdvancementCalls(Times.Exactly(2));
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    // ---- 422 and other failures ----

    [Fact(DisplayName = "Should keep the dialog open with the error text, the typed reason and no toast when Create gets a 422")]
    public async Task Create_ShouldStayOpenWithError_On422()
    {
        // Arrange
        const string reason = "That bowler has no result row in this tournament.";
        var requests = _host.SetupCreateOverride("q1", await _host.RejectedCreateAsync(HttpStatusCode.UnprocessableEntity, reason));
        var cut = await RenderExpandedAsync(Advancement(7));
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        cut.Find(Modal).ShouldNotBeNull();
        cut.Find("[data-testid='stage-override-error']").TextContent.ShouldContain(reason);
        cut.Find(Reason).GetAttribute("value").ShouldBe("Withdrew before the Stage.");
        cut.Find(Save).TextContent.Trim().ShouldBe("Clear slot");
        SaveDisabled(cut).ShouldBeFalse();
        requests.ShouldHaveSingleItem();
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }

    [Theory(DisplayName = "Should toast Clear Slot Failed and close the dialog when Create gets a 403, 404 or 500, with no reload")]
    [InlineData(HttpStatusCode.Forbidden, "An unexpected error occurred. Please try again.")]
    [InlineData(HttpStatusCode.NotFound, "The requested resource was not found.")]
    [InlineData(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")]
    public async Task Clear_ShouldToastFailureAndClose_OnOtherFailure(HttpStatusCode status, string message)
    {
        // Arrange
        _host.SetupCreateOverride("q1", _host.FailedCreate(status));
        var cut = await RenderExpandedAsync(Advancement(7));
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Clear Slot Failed");
        toast.Message.ShouldBe(message);
        toast.Severity.ShouldBe(NotifySeverity.Error);
        cut.FindAll(Modal).ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Theory(DisplayName = "Should toast Add Bowler Failed and close the dialog when an Add gets a 403, 404 or 500, with no reload")]
    [InlineData(HttpStatusCode.Forbidden, "An unexpected error occurred. Please try again.")]
    [InlineData(HttpStatusCode.NotFound, "The requested resource was not found.")]
    [InlineData(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")]
    public async Task Add_ShouldToastFailureAndClose_OnOtherFailure(HttpStatusCode status, string message)
    {
        // Arrange
        _host.SetupResults("q1", StageResultSummaryResponseFactory.Create(bowlerId: "b7", bowlerName: "Gus Gray", place: 7));
        _host.SetupCreateOverride("q1", _host.FailedCreate(status));
        var cut = await RenderExpandedAsync(Advancement(7));
        await cut.Find("[data-testid='stage-add-bowler']").ClickAsync();
        await cut.InvokeAsync(() => cut.Find("#override-bowler").TriggerEvent("onfocus", new FocusEventArgs()));
        await cut.Find(".neba-autocomplete-option").ClickAsync();
        await TypeReasonAsync(cut, "Confirmed by the director.");

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Add Bowler Failed");
        toast.Message.ShouldBe(message);
        toast.Severity.ShouldBe(NotifySeverity.Error);
        cut.FindAll(Modal).ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Theory(DisplayName = "Should toast Remove Failed, close the confirmation and make no reload when Remove fails")]
    [InlineData(HttpStatusCode.Forbidden, "An unexpected error occurred. Please try again.")]
    [InlineData(HttpStatusCode.NotFound, "The requested resource was not found.")]
    [InlineData(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")]
    public async Task Remove_ShouldToastFailureAndClose_OnFailure(HttpStatusCode status, string message)
    {
        // Arrange
        _host.SetupRemoveOverride("q1", _host.FailedWrite(status));
        var cut = await RenderExpandedAsync(Advancement(7));
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Remove Failed");
        toast.Message.ShouldBe(message);
        toast.Severity.ShouldBe(NotifySeverity.Error);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Once());
    }

    // ---- A write in flight ----

    [Fact(DisplayName = "Should render every action button disabled while a Create is pending and send no second request")]
    public async Task Create_ShouldDisableEveryButtonAndIgnoreClicks_WhilePending()
    {
        // Arrange
        var completion = new TaskCompletionSource<IApiResponse<CreatedStageOverrideResponse>>();
        var requests = _host.SetupCreateOverride("q1", completion);
        var recomputes = _host.SetupRecompute("q1", _host.NoContent());
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await FillClearAsync(cut);
        var buttonsBefore = Buttons(cut).Count;

        // Act
        var click = cut.Find(Save).ClickAsync();
        await cut.WaitForAssertionAsync(() => Buttons(cut).ShouldAllBe(b => b.HasAttribute("disabled")));
        var disabledWhilePending = Buttons(cut).Count;
        var saveLabel = cut.Find(Save).TextContent.Trim();
        var cancelDisabled = cut.Find("[data-testid='stage-override-cancel']").HasAttribute("disabled");
        await cut.Find("[data-testid='stage-add-bowler']").ClickAsync();
        await cut.Find("[data-testid='entry-clear'][data-bowler-id='b2']").ClickAsync();
        await cut.Find("[data-testid='override-remove'][data-override-id='ov-add']").ClickAsync();
        await cut.Find(Save).ClickAsync();
        completion.SetResult(_host.Created());
        await click;

        // Assert
        buttonsBefore.ShouldBe(9);
        disabledWhilePending.ShouldBe(9);
        saveLabel.ShouldBe("Saving...");
        cancelDisabled.ShouldBeTrue();
        requests.ShouldHaveSingleItem();
        recomputes.ShouldBeEmpty();
        _host.VerifyAnyRemoveCalls(Times.Never());
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        await cut.WaitForAssertionAsync(() => Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
    }

    [Theory(DisplayName = "Should enable every action button again after a Create succeeds, gets a 409, fails with a 500 or throws")]
    [InlineData("success")]
    [InlineData("409")]
    [InlineData("500")]
    [InlineData("throws")]
    public async Task Create_ShouldEnableButtons_AfterTheWriteEnds(string outcome)
    {
        // Arrange
        var completion = new TaskCompletionSource<IApiResponse<CreatedStageOverrideResponse>>();
        _host.SetupCreateOverride("q1", completion);
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await FillClearAsync(cut);
        var click = cut.Find(Save).ClickAsync();
        await cut.WaitForAssertionAsync(() => Buttons(cut).ShouldAllBe(b => b.HasAttribute("disabled")));

        // Act
        switch (outcome)
        {
            case "success":
                completion.SetResult(_host.Created());
                break;
            case "409":
                completion.SetResult(await _host.RejectedCreateAsync(HttpStatusCode.Conflict, ApiText));
                break;
            case "500":
                completion.SetResult(_host.FailedCreate(HttpStatusCode.InternalServerError));
                break;
            default:
                completion.SetException(new HttpRequestException("The connection dropped."));
                break;
        }

        await click;

        // Assert
        await cut.WaitForAssertionAsync(() => Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
        Buttons(cut).Count.ShouldBe(9);
        _host.Toasts.Current.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should enable every action button again when a Create gets a 422 and the dialog stays open")]
    public async Task Create_ShouldEnableButtons_After422()
    {
        // Arrange
        _host.SetupCreateOverride("q1", await _host.RejectedCreateAsync(HttpStatusCode.UnprocessableEntity, "No result row."));
        var cut = await RenderExpandedAsync(Advancement(7));
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        Buttons(cut).Count.ShouldBe(9);
        Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled"));
        cut.Find(Modal).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show the busy confirmation, disable the action buttons and ignore a second confirm while a Remove is pending")]
    public async Task Remove_ShouldIgnoreSecondConfirm_WhilePending()
    {
        // Arrange
        var completion = new TaskCompletionSource<IApiResponse>();
        var requests = _host.SetupRemoveOverride("q1", completion);
        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await RemoveOverrideAsync(cut, "ov-add");
        var confirm = RemoveConfirm(cut);

        // Act
        var click = cut.Find(ConfirmButton).ClickAsync();
        await cut.WaitForAssertionAsync(() => cut.Find(ConfirmButton).HasAttribute("disabled").ShouldBeTrue());
        var label = cut.Find(ConfirmButton).TextContent.Trim();
        var cancelDisabled = cut.Find(CancelButton).HasAttribute("disabled");
        var buttonsDisabled = Buttons(cut).All(b => b.HasAttribute("disabled"));
        await cut.InvokeAsync(() => confirm.Instance.OnConfirm.InvokeAsync());
        await cut.InvokeAsync(() => confirm.Instance.OnCancel.InvokeAsync());
        var stillOpen = cut.FindAll(ConfirmButton).Count;
        completion.SetResult(_host.NoContent());
        await click;

        // Assert
        label.ShouldBe("Removing...");
        cancelDisabled.ShouldBeTrue();
        buttonsDisabled.ShouldBeTrue();
        stillOpen.ShouldBe(1);
        requests.ShouldHaveSingleItem();
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        await cut.WaitForAssertionAsync(() => Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled")));
    }

    [Theory(DisplayName = "Should enable every action button again after a Remove gets a 409 or fails")]
    [InlineData("409")]
    [InlineData("500")]
    [InlineData("throws")]
    public async Task Remove_ShouldEnableButtons_AfterFailure(string outcome)
    {
        // Arrange
        switch (outcome)
        {
            case "409":
                _host.SetupRemoveOverride("q1", await _host.RejectedWriteAsync(HttpStatusCode.Conflict, ApiText));
                break;
            case "500":
                _host.SetupRemoveOverride("q1", _host.FailedWrite(HttpStatusCode.InternalServerError));
                break;
            default:
                _host.SetupRemoveOverride("q1", () => Task.FromException<IApiResponse>(new HttpRequestException("The connection dropped.")));
                break;
        }

        var cut = await RenderExpandedAsync(Advancement(7), Advancement(8));
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        Buttons(cut).Count.ShouldBe(9);
        Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled"));
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    // ---- A failed reload ----

    [Fact(DisplayName = "Should replace the success toast with Reload Failed and keep the data when the reload after a Clear fails")]
    public async Task Clear_ShouldToastReloadFailed_WhenReloadFails()
    {
        // Arrange
        _host.SignInAsAdmin();
        _host.SetupDetailFor(Advancement(7));
        _host.SetupAdvancementSequence(_host.OkResponse(Advancement(7)), _host.FailedAdvancementResponse(HttpStatusCode.InternalServerError));
        _host.SetupCreateOverride("q1", _host.Created());
        var cut = _host.Render();
        await SeriesDetailHost.ExpandAsync(cut, "q1");
        await FillClearAsync(cut);

        // Act
        await cut.Find(Save).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Reload Failed");
        toast.Message.ShouldBe("The Stage status could not refresh. Reload the page to see the latest.");
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        cut.FindAll(Modal).ShouldBeEmpty();
        cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(3);
        Buttons(cut).ShouldAllBe(b => !b.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Should replace the success toast with Reload Failed and close the confirmation when the reload after a Remove fails")]
    public async Task Remove_ShouldToastReloadFailed_WhenReloadFails()
    {
        // Arrange
        _host.SignInAsAdmin();
        _host.SetupDetailFor(Advancement(7));
        _host.SetupAdvancementSequence(_host.OkResponse(Advancement(7)), _host.FailedAdvancementResponse(HttpStatusCode.InternalServerError));
        _host.SetupRemoveOverride("q1", _host.NoContent());
        var cut = _host.Render();
        await SeriesDetailHost.ExpandAsync(cut, "q1");
        await RemoveOverrideAsync(cut, "ov-add");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var toast = _host.Toasts.Current.ShouldNotBeNull();
        toast.Title.ShouldBe("Reload Failed");
        toast.Severity.ShouldBe(NotifySeverity.Warning);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        cut.FindAll("[data-testid='series-stage-card']").Count.ShouldBe(3);
    }
}