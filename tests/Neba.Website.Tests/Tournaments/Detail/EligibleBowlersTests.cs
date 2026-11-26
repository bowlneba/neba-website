using Bunit;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EligibleBowlers")]
public sealed class EligibleBowlersTests : IDisposable
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private readonly BunitContext _ctx = new();
    private readonly Mock<ITournamentApiService> _service = new(MockBehavior.Strict);

    public EligibleBowlersTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton(_service.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show the heading and loading state while the list is pending")]
    public void Render_ShouldShowLoading_WhilePending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<EligibleBowlersViewModel>>();
        _service
            .Setup(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find("h2").TextContent.Trim().ShouldBe("Eligible bowlers");
        cut.Find(".eligible-bowlers__loading").ShouldNotBeNull();
        cut.FindAll(".eligible-bowlers__table, .eligible-bowlers__search, .eligible-bowlers__empty, .eligible-bowlers__error").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should load the list for the TournamentId parameter once")]
    public void OnInit_ShouldLoadOnce_ForTournamentId()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create());

        // Act
        RenderComponent();

        // Assert
        _service.Verify(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should show names and USBC IDs in the order returned")]
    public void Render_ShouldShowRows_InReturnedOrder()
    {
        // Arrange: not alphabetical, so a re-sort in the component would fail.
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            EligibleBowlerViewModelFactory.Create(name: "Zed Young", usbcId: "2222-22222"),
            EligibleBowlerViewModelFactory.Create(name: "Ann Adams", usbcId: "1111-11111")
        ]));

        // Act
        var cut = RenderComponent();

        // Assert
        Names(cut).ShouldBe(["Zed Young", "Ann Adams"]);
        Ids(cut).ShouldBe(["2222-22222", "1111-11111"]);
        cut.FindAll(".eligible-bowlers__table th[scope=col]").Select(th => th.TextContent.Trim()).ShouldBe(["Name", "USBC ID"]);
    }

    [Fact(DisplayName = "Should show No USBC ID for a bowler without one")]
    public void Render_ShouldShowNoUsbcId_WhenBowlerHasNone()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers: [EligibleBowlerViewModelFactory.Create(usbcId: null)]));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__no-id").TextContent.Trim().ShouldBe("No USBC ID");
    }

    [Fact(DisplayName = "Should take the number in the subtitle from the response")]
    public void Render_ShouldShowThresholdInSubtitle()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: 5));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Markup.ShouldContain("Bowlers with a result in 5 or more tournaments this season.");
    }

    [Fact(DisplayName = "Should give the scroll box keyboard focus and a name, and label the search box")]
    public void Render_ShouldBeAccessible()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create());

        // Act
        var cut = RenderComponent();

        // Assert
        var scroll = cut.Find(".eligible-bowlers__scroll");
        scroll.GetAttribute("tabindex").ShouldBe("0");
        scroll.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        var search = cut.Find("input.eligible-bowlers__search");
        search.GetAttribute("autocomplete").ShouldBe("off");
        cut.Find($"label[for='{search.Id}']").TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "Should show the empty text with the response's number and no search box")]
    [InlineData(3, "No bowlers have reached 3 tournaments yet this season.", TestDisplayName = "Three")]
    [InlineData(5, "No bowlers have reached 5 tournaments yet this season.", TestDisplayName = "Five")]
    public void Render_ShouldShowEmptyText_WhenNobodyQualifies(int minimum, string expected)
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: minimum, bowlers: []));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__empty").TextContent.Trim().ShouldBe(expected);
        cut.FindAll(".eligible-bowlers__search, .eligible-bowlers__table, .eligible-bowlers__count").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render nothing when the list does not apply")]
    public void Render_ShouldRenderNothing_WhenListDoesNotApply()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.CreateNotApplicable());

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Markup.ShouldBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Should show the error text and a Try again button, keeping the heading")]
    public void Render_ShouldShowError_WhenLoadFails()
    {
        // Arrange
        SetupResult(Error.Failure());

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find("h2").TextContent.Trim().ShouldBe("Eligible bowlers");
        cut.Find(".eligible-bowlers__error").TextContent.ShouldContain("Could not load the eligible bowlers.");
        cut.Find("button.eligible-bowlers__retry").TextContent.Trim().ShouldBe("Try again");
        cut.FindAll(".eligible-bowlers__table, .eligible-bowlers__empty").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should reload and show the list when Try again succeeds")]
    public void Retry_ShouldShowList_WhenSecondLoadSucceeds()
    {
        // Arrange
        _service
            .SetupSequence(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)Error.Failure())
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)EligibleBowlersViewModelFactory.Create());
        var cut = RenderComponent();

        // Act
        cut.Find("button.eligible-bowlers__retry").Click();

        // Assert
        Names(cut).ShouldBe([EligibleBowlerViewModelFactory.ValidName]);
        cut.FindAll(".eligible-bowlers__error").ShouldBeEmpty();
        _service.Verify(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "Should show loading and hide the error while a retry is pending")]
    public void Retry_ShouldShowLoading_WhilePending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<EligibleBowlersViewModel>>();
        _service
            .SetupSequence(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)Error.Failure())
            .Returns(pending.Task);
        var cut = RenderComponent();

        // Act
        cut.Find("button.eligible-bowlers__retry").Click();

        // Assert
        cut.Find(".eligible-bowlers__loading").ShouldNotBeNull();
        cut.FindAll(".eligible-bowlers__error").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should count the bowlers with a singular or plural noun")]
    [InlineData(1, "1 bowler", TestDisplayName = "One")]
    [InlineData(24, "24 bowlers", TestDisplayName = "Many")]
    public void Render_ShouldShowCount(int count, string expected)
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers: EligibleBowlerViewModelFactory.Bogus(count)));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe(expected);
    }

    [Fact(DisplayName = "Should show the filtered and total counts while filtering")]
    public void Search_ShouldShowFilteredCount()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            .. Enumerable.Range(0, 21).Select(_ => EligibleBowlerViewModelFactory.Create()),
            .. Enumerable.Range(0, 3).Select(_ => EligibleBowlerViewModelFactory.Create(name: "Zed Quill"))
        ]));
        var cut = RenderComponent();

        // Act
        cut.Find("input.eligible-bowlers__search").Input("quill");

        // Assert
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe("3 of 24 bowlers");
    }

    [Theory(DisplayName = "Should filter by name or USBC ID")]
    [InlineData("smith", "Dave Smith", TestDisplayName = "Part of a name")]
    [InlineData("SMITH", "Dave Smith", TestDisplayName = "Ignores case")]
    [InlineData("smith dave", "Dave Smith", TestDisplayName = "Words in any order")]
    [InlineData("  jones  ", "Mary Ann Jones", TestDisplayName = "Trims the term")]
    [InlineData("o'bri", "Pat O'Brien", TestDisplayName = "Apostrophe")]
    [InlineData("4821-09317", "Dave Smith", TestDisplayName = "ID with dash")]
    [InlineData("482109317", "Dave Smith", TestDisplayName = "ID without dash")]
    [InlineData("4821 09317", "Dave Smith", TestDisplayName = "ID with a space")]
    [InlineData("11-112", "Pat O'Brien", TestDisplayName = "Part of an ID with a misplaced dash")]
    public void Search_ShouldFilter(string term, string expected)
    {
        // Arrange
        var cut = RenderSearchableList();

        // Act
        cut.Find("input.eligible-bowlers__search").Input(term);

        // Assert
        Names(cut).ShouldBe([expected]);
    }

    [Theory(DisplayName = "Should show the no-match message with the term")]
    [InlineData("zzz", TestDisplayName = "Unknown name")]
    [InlineData("dave jones", TestDisplayName = "Every word must match the same bowler")]
    [InlineData("-", TestDisplayName = "A term with no digit does not search IDs")]
    public void Search_ShouldShowNoMatch(string term)
    {
        // Arrange
        var cut = RenderSearchableList();

        // Act
        cut.Find("input.eligible-bowlers__search").Input(term);

        // Assert
        cut.Find(".eligible-bowlers__no-match").TextContent.Trim().ShouldBe($"No bowlers match “{term}”.");
        Names(cut).ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should restore the full list when the term is cleared")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Search_ShouldRestoreList_WhenCleared(string cleared)
    {
        // Arrange
        var cut = RenderSearchableList();
        cut.Find("input.eligible-bowlers__search").Input("zzz");

        // Act
        cut.Find("input.eligible-bowlers__search").Input(cleared);

        // Assert
        Names(cut).ShouldBe(["Dave Smith", "Pat O'Brien", "Mary Ann Jones"]);
        cut.FindAll(".eligible-bowlers__no-match").ShouldBeEmpty();
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe("3 bowlers");
    }

    [Fact(DisplayName = "Should word the subtitle and empty text without a number when no minimum applies")]
    public void Render_ShouldOmitNumber_WhenMinimumIsNull()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: null, bowlers: []));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Markup.ShouldContain("Bowlers who have qualified this season.");
        cut.Find(".eligible-bowlers__empty").TextContent.Trim().ShouldBe("No bowlers have qualified yet this season.");
    }

    [Fact(DisplayName = "Should match a straight apostrophe when the term has a curly one")]
    public void Search_ShouldTreatCurlyApostropheAsStraight()
    {
        // Arrange
        var cut = RenderSearchableList();

        // Act
        cut.Find("input.eligible-bowlers__search").Input("O\u2019Brien");

        // Assert
        Names(cut).ShouldBe(["Pat O'Brien"]);
    }

    [Fact(DisplayName = "Should raise no error when disposed while the load is pending")]
    public async Task Dispose_ShouldCancelLoad_WhenPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<EligibleBowlersViewModel>>();
        var token = CancellationToken.None;
        _service
            .Setup(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, ct) => token = ct)
            .Returns(pending.Task);
        RenderComponent();

        // Act
        await _ctx.DisposeComponentsAsync();
        pending.SetResult(Error.Failure());

        // Assert
        token.IsCancellationRequested.ShouldBeTrue();
        _ctx.Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
    }

    private void SetupResult(ErrorOr<EligibleBowlersViewModel> result)
        => _service
            .Setup(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private IRenderedComponent<EligibleBowlers> RenderComponent()
        => _ctx.Render<EligibleBowlers>(p => p.Add(x => x.TournamentId, TournamentId));

    private IRenderedComponent<EligibleBowlers> RenderSearchableList()
    {
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            EligibleBowlerViewModelFactory.Create(name: "Dave Smith", usbcId: "4821-09317"),
            EligibleBowlerViewModelFactory.Create(name: "Pat O'Brien", usbcId: "1111-22222"),
            EligibleBowlerViewModelFactory.Create(name: "Mary Ann Jones", usbcId: null)
        ]));
        return RenderComponent();
    }

    private static string[] Names(IRenderedComponent<EligibleBowlers> cut)
        => [.. cut.FindAll(".eligible-bowlers__table tbody tr > :first-child").Select(cell => cell.TextContent.Trim())];

    private static string[] Ids(IRenderedComponent<EligibleBowlers> cut)
        => [.. cut.FindAll(".eligible-bowlers__table tbody tr > :nth-child(2)").Select(cell => cell.TextContent.Trim())];
}
