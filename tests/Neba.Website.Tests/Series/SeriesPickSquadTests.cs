using System.Net;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

using Refit;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesPickSquadTests : IDisposable
{
    private const string SurvivorId = "s1";
    private const string CurrentSquadId = "squad-current";
    private const string OtherSquadId = "squad-other";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private static SeriesAdvancementResponse Advancement(bool needsSquad = false, int version = 7)
        => SeriesAdvancementResponseFactory.Create(
            version: version,
            stages:
            [
                StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: SurvivorId, order: 3, kind: "Survivor", status: "Computed", needsSquad: needsSquad),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement", status: "Pending"),
            ]);

    private static StageSquadSummaryResponse[] TwoSquads()
        =>
        [
            StageSquadSummaryResponseFactory.Create(
                squadId: CurrentSquadId, date: new DateOnly(2025, 7, 12), startTime: new TimeOnly(9, 0), hasScores: true, isSelected: true),
            StageSquadSummaryResponseFactory.Create(
                squadId: OtherSquadId, date: new DateOnly(2025, 7, 13), startTime: new TimeOnly(13, 30), hasScores: false, isSelected: false),
        ];

    private IRenderedComponent<SeriesDetail> RenderPage(bool admin = false, SeriesAdvancementResponse? advancement = null)
    {
        advancement ??= Advancement();
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

    private static Task OpenPickerAsync(IRenderedComponent<SeriesDetail> cut)
        => SeriesDetailHost.Card(cut, SurvivorId).QuerySelector("[data-testid='stage-pick-squad']")!.ClickAsync();

    private static IElement Option(IRenderedComponent<SeriesDetail> cut, string squadId)
        => cut.Find($"[data-testid='stage-squad-option'][data-squad-id='{squadId}']");

    private static IElement Radio(IRenderedComponent<SeriesDetail> cut, string squadId)
        => Option(cut, squadId).QuerySelector("input[type='radio']")!;

    private static Task ChooseAsync(IRenderedComponent<SeriesDetail> cut, string squadId)
        => Radio(cut, squadId).ChangeAsync(new ChangeEventArgs { Value = squadId });

    private static bool SaveDisabled(IRenderedComponent<SeriesDetail> cut)
        => cut.Find("[data-testid='stage-squad-save']").HasAttribute("disabled");

    // ---- Where the button shows ----

    [Theory(DisplayName = "Should show Pick squad on a Survivor Stage and not on a Qualifying or Placement Stage for a Manager and an Admin")]
    [InlineData(false)]
    [InlineData(true)]
    public void Button_ShouldShowOnSurvivorOnly(bool admin)
    {
        // Arrange
        var cut = RenderPage(admin);

        // Act
        var survivor = SeriesDetailHost.Card(cut, SurvivorId).QuerySelector("[data-testid='stage-pick-squad']");

        // Assert
        survivor.ShouldNotBeNull();
        survivor.TextContent.Trim().ShouldBe("Pick squad");
        survivor.GetAttribute("aria-label").ShouldBe("Pick squad for the 3rd tournament");
        survivor.HasAttribute("disabled").ShouldBeFalse();
        SeriesDetailHost.Card(cut, "q1").QuerySelector("[data-testid='stage-pick-squad']").ShouldBeNull();
        SeriesDetailHost.Card(cut, "q2").QuerySelector("[data-testid='stage-pick-squad']").ShouldBeNull();
        SeriesDetailHost.Card(cut, "p1").QuerySelector("[data-testid='stage-pick-squad']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should not render the squad dialog until Pick squad is pressed")]
    public void Dialog_ShouldBeClosed_Initially()
    {
        // Arrange
        // Act
        var cut = RenderPage();

        // Assert
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
        _host.VerifySquadCalls(SurvivorId, Times.Never());
    }

    // ---- Opening ----

    [Fact(DisplayName = "Should list the Stage's squads once and show each squad's Label and Detail")]
    public async Task Open_ShouldListSquadsOnce_WithLabelAndDetail()
    {
        // Arrange
        _host.SetupSquads(SurvivorId, TwoSquads());
        var cut = RenderPage();

        // Act
        await OpenPickerAsync(cut);

        // Assert
        _host.VerifySquadCalls(SurvivorId, Times.Once());
        cut.Find("[data-testid='stage-squad-list']").GetAttribute("aria-label").ShouldBe("Squads");
        var options = cut.FindAll("[data-testid='stage-squad-option']");
        options.Count.ShouldBe(2);
        options.Select(o => o.GetAttribute("data-squad-id")).ShouldBe([CurrentSquadId, OtherSquadId]);
        options[0].QuerySelector("b")!.TextContent.ShouldBe("9:00 AM on 7/12");
        options[0].QuerySelector("small")!.TextContent.ShouldBe("Has scores · Current");
        options[1].QuerySelector("b")!.TextContent.ShouldBe("1:30 PM on 7/13");
        options[1].QuerySelector("small")!.TextContent.ShouldBe("No scores yet");
        options.ShouldAllBe(o => o.QuerySelector("input[type='radio']")!.GetAttribute("name") == "stage-squad");
        cut.Find("[data-testid='stage-squad-modal']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should preselect the current squad and keep Use this squad disabled until the selection differs")]
    public async Task Open_ShouldPreselectCurrent_AndGateSave()
    {
        // Arrange
        _host.SetupSquads(SurvivorId, TwoSquads());
        var cut = RenderPage();

        // Act
        await OpenPickerAsync(cut);
        var initiallyChecked = Radio(cut, CurrentSquadId).HasAttribute("checked");
        var otherInitiallyChecked = Radio(cut, OtherSquadId).HasAttribute("checked");
        var saveDisabledAtStart = SaveDisabled(cut);
        await ChooseAsync(cut, OtherSquadId);
        var saveDisabledAfterChange = SaveDisabled(cut);
        await ChooseAsync(cut, CurrentSquadId);
        var saveDisabledAfterRevert = SaveDisabled(cut);

        // Assert
        initiallyChecked.ShouldBeTrue();
        otherInitiallyChecked.ShouldBeFalse();
        saveDisabledAtStart.ShouldBeTrue();
        saveDisabledAfterChange.ShouldBeFalse();
        saveDisabledAfterRevert.ShouldBeTrue();
        cut.Find("[data-testid='stage-squad-save']").TextContent.Trim().ShouldBe("Use this squad");
    }

    [Fact(DisplayName = "Should preselect nothing and disable Use this squad when no squad is the Stage's current one")]
    public async Task Open_ShouldPreselectNothing_WhenNoneIsSelected()
    {
        // Arrange
        _host.SetupSquads(
            SurvivorId,
            StageSquadSummaryResponseFactory.Create(squadId: CurrentSquadId, isSelected: false),
            StageSquadSummaryResponseFactory.Create(squadId: OtherSquadId, isSelected: false));
        var cut = RenderPage(advancement: Advancement(needsSquad: true));

        // Act
        await OpenPickerAsync(cut);
        var saveDisabledAtStart = SaveDisabled(cut);
        await ChooseAsync(cut, OtherSquadId);

        // Assert
        saveDisabledAtStart.ShouldBeTrue();
        cut.FindAll("[data-testid='stage-squad-option'] input[checked]").ShouldBe([Radio(cut, OtherSquadId)]);
        SaveDisabled(cut).ShouldBeFalse();
    }

    [Theory(DisplayName = "Should say no squad matches exactly only when the Stage needs a squad")]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Lead_ShouldMentionNoMatch_OnlyWhenNeedsSquad(bool needsSquad, bool mentionsNoMatch)
    {
        // Arrange
        _host.SetupSquads(SurvivorId, TwoSquads());
        var cut = RenderPage(advancement: Advancement(needsSquad));

        // Act
        await OpenPickerAsync(cut);

        // Assert
        var lead = cut.Find("[data-testid='stage-squad-lead']").TextContent;
        lead.StartsWith("3rd tournament, Tournament 3.", StringComparison.Ordinal).ShouldBeTrue();
        lead.Contains("No squad matches it exactly", StringComparison.Ordinal).ShouldBe(mentionsNoMatch);
        lead.Contains("Choose the squad this round uses.", StringComparison.Ordinal).ShouldBe(!mentionsNoMatch);
    }

    // ---- Empty, failed and loading ----

    [Fact(DisplayName = "Should show the no-squads text and keep Use this squad disabled when the tournament has no squads")]
    public async Task Open_ShouldShowEmptyState_WhenNoSquads()
    {
        // Arrange
        _host.SetupSquads(SurvivorId);
        var cut = RenderPage();

        // Act
        await OpenPickerAsync(cut);

        // Assert
        cut.Find("[data-testid='stage-squad-empty']").TextContent.ShouldBe(SeriesDisplay.NoSquads);
        cut.FindAll("[data-testid='stage-squad-list']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-squad-load-error']").ShouldBeEmpty();
        SaveDisabled(cut).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show the load-error alert and no list when the squads fail to load")]
    public async Task Open_ShouldShowLoadError_WhenListFails()
    {
        // Arrange
        _host.SetupSquadsFailure(SurvivorId, HttpStatusCode.InternalServerError);
        var cut = RenderPage();

        // Act
        await OpenPickerAsync(cut);

        // Assert
        cut.Find("[data-testid='stage-squad-load-error']").TextContent.ShouldContain("An unexpected error occurred. Please try again.");
        cut.FindAll("[data-testid='stage-squad-list']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-squad-empty']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-squad-loading']").ShouldBeEmpty();
        SaveDisabled(cut).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show the busy skeleton while the squads load, then the options")]
    public async Task Open_ShouldShowLoadingSkeleton_WhilePending()
    {
        // Arrange
        var completion = new TaskCompletionSource<IApiResponse<CollectionResponse<StageSquadSummaryResponse>>>();
        _host.SetupSquadsPending(SurvivorId, completion);
        var cut = RenderPage();

        // Act
        var click = OpenPickerAsync(cut);
        await cut.WaitForAssertionAsync(() => cut.Find("[data-testid='stage-squad-loading']"));
        var busy = cut.Find("[data-testid='stage-squad-loading']").GetAttribute("aria-busy");
        var optionsWhileLoading = cut.FindAll("[data-testid='stage-squad-option']").Count;
        completion.SetResult(_host.SquadsResponse(TwoSquads()));
        await click;

        // Assert
        busy.ShouldBe("true");
        optionsWhileLoading.ShouldBe(0);
        await cut.WaitForAssertionAsync(() => cut.FindAll("[data-testid='stage-squad-option']").Count.ShouldBe(2));
        cut.FindAll("[data-testid='stage-squad-loading']").ShouldBeEmpty();
    }

    // ---- Cancel and save ----

    [Fact(DisplayName = "Should close the dialog and make no write when Cancel is pressed")]
    public async Task Cancel_ShouldCloseAndMakeNoWrite()
    {
        // Arrange
        _host.SetupSquads(SurvivorId, TwoSquads());
        var cut = RenderPage();
        await OpenPickerAsync(cut);
        await ChooseAsync(cut, OtherSquadId);

        // Act
        await cut.Find("[data-testid='stage-squad-cancel']").ClickAsync();

        // Assert
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
        _host.Api.Verify(x => x.PickStageSquadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PickStageSquadRequest>(), It.IsAny<CancellationToken>()), Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
        _host.Toasts.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Should save the chosen squad with its id and the page Version, then close the dialog")]
    public async Task Save_ShouldSendSquadIdAndVersion_AndClose()
    {
        // Arrange
        _host.SetupSquads(SurvivorId, TwoSquads());
        var advancement = Advancement(version: 7);
        _host.SetupAdvancementSequence(advancement, Advancement(version: 8));
        var requests = _host.SetupPickSquad(SurvivorId, _host.NoContent());
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        var cut = _host.Render();
        await OpenPickerAsync(cut);
        await ChooseAsync(cut, OtherSquadId);

        // Act
        await cut.Find("[data-testid='stage-squad-save']").ClickAsync();

        // Assert
        var request = requests.ShouldHaveSingleItem();
        request.Id.ShouldBe(SeriesDetailHost.SeriesId);
        request.StageId.ShouldBe(SurvivorId);
        request.Squad.SquadId.ShouldBe(OtherSquadId);
        request.Squad.Version.ShouldBe(7);
        cut.FindAll("[data-testid='stage-squad-modal']").ShouldBeEmpty();
    }
}