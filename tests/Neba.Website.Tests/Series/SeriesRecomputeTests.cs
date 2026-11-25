using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesRecomputeTests : IDisposable
{
    private const string ConfirmButton = ".confirm-action-modal-confirm";
    private const string CancelButton = ".confirm-action-modal-cancel";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>Stage 1 Official, 2 Computed, 3 Survivor Official, 4 Placement Computed.</summary>
    private static SeriesAdvancementResponse WithOfficialStages(int version = 7)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
                StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Official"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Computed"),
            ]);

    private IRenderedComponent<SeriesDetail> RenderPage(SeriesAdvancementResponse advancement, bool admin = false)
    {
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
        return _host.Render();
    }

    private static Task RecomputeAsync(IRenderedComponent<SeriesDetail> cut, string stageId)
        => SeriesDetailHost.Card(cut, stageId).QuerySelector("[data-testid='stage-recompute']")!.ClickAsync();

    // ---- Where the button shows ----

    [Theory(DisplayName = "Should show Recompute from here on every Stage kind and status for a Manager and an Admin")]
    [InlineData(false)]
    [InlineData(true)]
    public void Button_ShouldShowOnEveryStage(bool admin)
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Pending"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Pending"),
            StageAdvancementResponseFactory.Create(stageId: "p2", order: 5, kind: "Placement", status: "Computed"),
        };

        // Act
        var cut = RenderPage(SeriesAdvancementResponseFactory.Create(stages: stages), admin);

        // Assert
        foreach (var stage in stages)
        {
            var button = SeriesDetailHost.Card(cut, stage.StageId).QuerySelector("[data-testid='stage-recompute']");
            button.ShouldNotBeNull();
            button.TextContent.Trim().ShouldBe("Recompute from here");
            button.GetAttribute("aria-label").ShouldBe("Recompute from the " + SeriesDisplay.TournamentOrdinal(stage.Order));
            button.HasAttribute("disabled").ShouldBeFalse();
        }
    }

    [Fact(DisplayName = "Should render no Recompute button when the viewer lacks Series.EditSeries")]
    public void Button_ShouldNotRender_WithoutEditSeries()
    {
        // Arrange
        var advancement = WithOfficialStages();
        _host.SignInWithoutEditSeries();
        _host.SetupDetailFor(advancement);

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='stage-recompute']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-pick-squad']").ShouldBeEmpty();
    }

    // ---- Plain run ----

    [Theory(DisplayName = "Should recompute at once with no confirmation and ConfirmResetOfficial false when no Stage is Official, from a Qualifying, Survivor or Placement Stage")]
    [InlineData("q1")]
    [InlineData("s1")]
    [InlineData("p1")]
    public async Task Recompute_ShouldRunAtOnce_WhenNoOfficialStageExists(string stageId)
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(
            version: 7,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Pending"),
            ]);
        _host.SetupAdvancementSequence(advancement, advancement);
        var requests = _host.SetupRecompute(stageId, _host.NoContent());
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        var cut = _host.Render();

        // Act
        await RecomputeAsync(cut, stageId);

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.StageId.ShouldBe(stageId);
        request.Recomputation.ConfirmResetOfficial.ShouldBeFalse();
        request.Recomputation.Version.ShouldBe(7);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should send one recompute for the Stage with the page Version when only earlier Stages are Official")]
    public async Task Recompute_ShouldSendOnce_WhenOnlyEarlierStagesAreOfficial()
    {
        // Arrange
        // Stage 1 is Official but the click is on Stage 2; Stage 3 is Computed here, so nothing in range is Official.
        var advancement = SeriesAdvancementResponseFactory.Create(
            version: 7,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
                StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Computed"),
            ]);
        _host.SetupAdvancementSequence(advancement, advancement);
        var requests = _host.SetupRecompute("q2", _host.NoContent());
        _host.SignInAsAdmin();
        _host.SetupDetailFor(advancement);
        var cut = _host.Render();

        // Act
        await RecomputeAsync(cut, "q2");

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.Id.ShouldBe(SeriesDetailHost.SeriesId);
        request.StageId.ShouldBe("q2");
        request.Recomputation.ConfirmResetOfficial.ShouldBeFalse();
        request.Recomputation.Version.ShouldBe(7);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    // ---- Confirmation ----

    [Fact(DisplayName = "Should open the confirmation with the title and only the Official Stages at or after the clicked Stage")]
    public async Task Recompute_ShouldConfirm_ListingOfficialStagesFromClickedOrder()
    {
        // Arrange
        var cut = RenderPage(WithOfficialStages());

        // Act
        await RecomputeAsync(cut, "q2");

        // Assert
        var modal = cut.Find(".neba-modal-content");
        modal.QuerySelector(".neba-modal-title")!.TextContent.ShouldBe("Recompute from the 2nd tournament?");
        modal.QuerySelector("p")!.TextContent.ShouldBe("The 3rd tournament is official. Recomputing resets it to Computed.");
        cut.Find(ConfirmButton).TextContent.Trim().ShouldBe("Recompute");
    }

    [Fact(DisplayName = "Should list every Official Stage from the first Stage when recomputing from Stage 1")]
    public async Task Recompute_ShouldListAllOfficialStages_FromFirstStage()
    {
        // Arrange
        var cut = RenderPage(WithOfficialStages());

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        var modal = cut.Find(".neba-modal-content");
        modal.QuerySelector(".neba-modal-title")!.TextContent.ShouldBe("Recompute from the 1st tournament?");
        modal.QuerySelector("p")!.TextContent.ShouldBe("The 1st and 3rd tournaments are official. Recomputing resets them to Computed.");
    }

    [Fact(DisplayName = "Should make no recompute call until the confirmation is accepted")]
    public async Task Recompute_ShouldMakeNoCall_WhileConfirmationIsOpen()
    {
        // Arrange
        var cut = RenderPage(WithOfficialStages());

        // Act
        await RecomputeAsync(cut, "q1");

        // Assert
        cut.Find(ConfirmButton).ShouldNotBeNull();
        _host.Api.Verify(x => x.RecomputeSeriesAdvancementAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Neba.Api.Contracts.Series.RecomputeSeriesAdvancement.RecomputeSeriesAdvancementRequest>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Should send ConfirmResetOfficial true and the page Version after the confirmation is accepted")]
    public async Task Recompute_ShouldSendConfirmResetOfficial_WhenConfirmed()
    {
        // Arrange
        var advancement = WithOfficialStages(version: 7);
        _host.SetupAdvancementSequence(advancement, WithOfficialStages(version: 8));
        var requests = _host.SetupRecompute("q2", _host.NoContent());
        _host.SignInAsAdmin();
        _host.SetupDetailFor(advancement);
        var cut = _host.Render();
        await RecomputeAsync(cut, "q2");

        // Act
        await cut.Find(ConfirmButton).ClickAsync();

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.StageId.ShouldBe("q2");
        request.Recomputation.ConfirmResetOfficial.ShouldBeTrue();
        request.Recomputation.Version.ShouldBe(7);
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should close the confirmation and make no call when it is cancelled")]
    public async Task Recompute_ShouldMakeNoCall_WhenCancelled()
    {
        // Arrange
        var cut = RenderPage(WithOfficialStages());
        await RecomputeAsync(cut, "q1");

        // Act
        await cut.Find(CancelButton).ClickAsync();

        // Assert
        cut.FindAll(ConfirmButton).ShouldBeEmpty();
        _host.Api.Verify(x => x.RecomputeSeriesAdvancementAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Neba.Api.Contracts.Series.RecomputeSeriesAdvancement.RecomputeSeriesAdvancementRequest>(), It.IsAny<CancellationToken>()), Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }
}