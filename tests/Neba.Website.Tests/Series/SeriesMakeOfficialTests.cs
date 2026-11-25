using System.Net;

using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesMakeOfficialTests : IDisposable
{
    private const string ConfirmButton = ".confirm-action-modal-confirm";
    private const string CancelButton = ".confirm-action-modal-cancel";
    private const string MakeOfficialButton = "[data-testid='stage-make-official']";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>Stage 1 is ready to mark Official. Stage 2 waits on it. The Placement Stage never shows the button.</summary>
    private static SeriesAdvancementResponse Ready(int version = 7)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed", canMakeOfficial: true),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Computed"),
            ]);

    private IRenderedComponent<SeriesDetail> RenderAs(Action signIn, SeriesAdvancementResponse? advancement = null)
    {
        advancement ??= Ready();
        signIn();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        return _host.Render();
    }

    private static Task MakeOfficialAsync(IRenderedComponent<SeriesDetail> cut, string stageId)
        => SeriesDetailHost.Card(cut, stageId).QuerySelector(MakeOfficialButton)!.ClickAsync();

    // ---- Who sees the button ----

    [Fact(DisplayName = "Should show Mark official only on the Stage that is ready, for a Manager")]
    public void Button_ShouldShowOnlyOnReadyStage_ForManager()
    {
        // Arrange & Act
        var cut = RenderAs(_host.SignInAsManager);

        // Assert
        var button = SeriesDetailHost.Card(cut, "q1").QuerySelector(MakeOfficialButton)!;
        button.TextContent.Trim().ShouldBe("Mark official");
        button.GetAttribute("aria-label").ShouldBe("Mark the 1st tournament official");
        SeriesDetailHost.Card(cut, "s1").QuerySelector(MakeOfficialButton).ShouldBeNull();
        SeriesDetailHost.Card(cut, "p1").QuerySelector(MakeOfficialButton).ShouldBeNull();
        cut.FindAll(MakeOfficialButton).Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should show the season rail and Mark official, but no Recompute or Pick squad, for a Tournament Director")]
    public void Button_ShouldShow_ForTournamentDirector_WithoutEditActions()
    {
        // Arrange & Act
        var cut = RenderAs(_host.SignInAsTournamentDirector);

        // Assert
        cut.FindAll("[data-testid='series-progress']").Count.ShouldBe(1);
        cut.FindAll(MakeOfficialButton).Count.ShouldBe(1);
        cut.FindAll("[data-testid='stage-recompute']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-pick-squad']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not show Mark official to an editor without Series.MakeStageOfficial")]
    public void Button_ShouldNotShow_WithoutMakeStageOfficial()
    {
        // Arrange & Act
        var cut = RenderAs(_host.SignInAsEditorWithoutMakeOfficial);

        // Assert
        cut.FindAll("[data-testid='stage-recompute']").ShouldNotBeEmpty();
        cut.FindAll(MakeOfficialButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the public list, not the rail, and no Mark official button to a user without either permission")]
    public void Button_ShouldNotShow_ForPublicViewer()
    {
        // Arrange
        var advancement = Ready();
        _host.SignInWithoutEditSeries();
        _host.SetupDetailFor(advancement);

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-progress']").ShouldBeEmpty();
        cut.FindAll(MakeOfficialButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should no longer show a Becomes official time on a Computed Stage")]
    public void Flags_ShouldNotShowDueTime()
    {
        // Arrange & Act
        var cut = RenderAs(_host.SignInAsManager);

        // Assert
        cut.FindAll("[data-testid='stage-flag-due']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Becomes official");
    }

    // ---- Confirmation and write ----

    [Fact(DisplayName = "Should open a confirmation naming the Stage and send nothing until the user confirms")]
    public async Task MakeOfficial_ShouldAskFirst()
    {
        // Arrange
        var requests = _host.SetupMakeOfficial("q1", _host.NoContent());
        var cut = RenderAs(_host.SignInAsManager);

        // Act
        await MakeOfficialAsync(cut, "q1");

        // Assert
        cut.Markup.ShouldContain("Mark the 1st tournament official?");
        cut.Markup.ShouldContain("public Series page");
        cut.FindAll(ConfirmButton).Count.ShouldBe(1);
        requests.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should send the Stage and the page Version on confirm, show a success toast and reload")]
    public async Task MakeOfficial_ShouldSendOnConfirm_AndReload()
    {
        // Arrange
        var requests = _host.SetupMakeOfficial("q1", _host.NoContent());
        var advancement = Ready(version: 7);
        var after = SeriesAdvancementResponseFactory.Create(
            version: 8,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed", canMakeOfficial: true),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Computed"),
            ]);
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancementSequence(advancement, after);
        var cut = _host.Render();
        await MakeOfficialAsync(cut, "q1");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.Id.ShouldBe(SeriesDetailHost.SeriesId);
        request.StageId.ShouldBe("q1");
        request.Official.Version.ShouldBe(7);
        _host.Toasts.Current.ShouldNotBeNull();
        _host.Toasts.Current.Message.ShouldBe("The 1st tournament is official.");
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        SeriesDetailHost.Card(cut, "q1").QuerySelector(MakeOfficialButton).ShouldBeNull();
        SeriesDetailHost.Card(cut, "s1").QuerySelector(MakeOfficialButton).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should close the confirmation and send nothing on cancel")]
    public async Task MakeOfficial_ShouldSendNothing_OnCancel()
    {
        // Arrange
        var requests = _host.SetupMakeOfficial("q1", _host.NoContent());
        var cut = RenderAs(_host.SignInAsManager);
        await MakeOfficialAsync(cut, "q1");

        // Act
        await cut.Find(CancelButton).ClickAsync();

        // Assert
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        requests.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the API's reason in a warning toast and reload when the Series changed (409)")]
    public async Task MakeOfficial_ShouldWarnAndReload_OnConflict()
    {
        // Arrange
        var rejected = await _host.RejectedWriteAsync(HttpStatusCode.Conflict, "Stage 1 must be official first.");
        _host.SetupMakeOfficial("q1", rejected);
        var advancement = Ready();
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancementSequence(advancement, advancement);
        var cut = _host.Render();
        await MakeOfficialAsync(cut, "q1");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        _host.Toasts.Current.ShouldNotBeNull();
        _host.Toasts.Current.Title.ShouldBe("Series Changed");
        _host.Toasts.Current.Message.ShouldBe("Stage 1 must be official first.");
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show an error toast and close the confirmation when the write fails")]
    public async Task MakeOfficial_ShouldShowErrorToast_OnFailure()
    {
        // Arrange
        _host.SetupMakeOfficial("q1", _host.FailedWrite(HttpStatusCode.InternalServerError));
        var cut = RenderAs(_host.SignInAsManager);
        await MakeOfficialAsync(cut, "q1");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        _host.Toasts.Current.ShouldNotBeNull();
        _host.Toasts.Current.Title.ShouldBe("Mark Official Failed");
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }
}