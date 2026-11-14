using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.TeamEligibility;
using Neba.Website.Tests.TestSupport;

using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.CheckTeamEligibility")]
public sealed class CheckTeamEligibilityTests : IDisposable
{
    private const string TournamentId = "01JSTX1234567890ABCDEFGHIJ";

    private readonly BunitContext _ctx = new();
    private readonly Mock<ITournamentsApi> _tournamentsApi = new(MockBehavior.Strict);
    private readonly Mock<ITeamEligibilityService> _service = new(MockBehavior.Strict);
    private readonly Mock<IRulesSectionService> _rules = new(MockBehavior.Strict);

    public CheckTeamEligibilityTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _rules
            .Setup(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RulesSection("<p>Rules text</p>", null));

        _ctx.Services.AddSingleton(_tournamentsApi.Object);
        _ctx.Services.AddSingleton(_service.Object);
        _ctx.Services.AddSingleton(_rules.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    private void SetupTournament(TournamentType type, int teamSize)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = TournamentDetailResponseFactory.Create(
                id: TournamentId, name: "Joe's Pizza Open",
                startDate: new DateOnly(2027, 3, 7), endDate: new DateOnly(2027, 3, 8), tournamentType: type, teamSize: teamSize)
        };

        _tournamentsApi
            .Setup(x => x.GetTournamentAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupTournamentFailure(System.Net.HttpStatusCode status)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = status,
            Content = null
        };

        _tournamentsApi
            .Setup(x => x.GetTournamentAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private IRenderedComponent<CheckTeamEligibility> Render()
        => _ctx.Render<CheckTeamEligibility>(p => p.Add(x => x.Id, TournamentId));

    private static Task PickAsync(IRenderedComponent<CheckTeamEligibility> cut, int index, BowlerSearchResultViewModel bowler)
    {
        var picker = cut.FindComponents<BowlerPicker>()[index];
        return cut.InvokeAsync(() => picker.Instance.SelectedChanged.InvokeAsync(bowler));
    }

    private static Task MarkUnknownAsync(IRenderedComponent<CheckTeamEligibility> cut, int index)
    {
        var picker = cut.FindComponents<BowlerPicker>()[index];
        return cut.InvokeAsync(() => picker.Instance.IsUnknownChanged.InvokeAsync(true));
    }

    private static BowlerSearchResultViewModel Bowler(int number)
        => new("id-" + number, "Bowler " + number, null);

    private void SetupCheck(ErrorOr<TeamEligibilityResultViewModel> result, Action<IReadOnlyCollection<string>, int>? capture = null)
        => _service
            .Setup(x => x.CheckAsync(TournamentId, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<string>, int, CancellationToken>((_, ids, unknown, _) => capture?.Invoke(ids, unknown))
            .ReturnsAsync(result);

    [Fact(DisplayName = "Should show the loading state until the tournament loads")]
    public void Render_ShouldShowLoading_WhileTournamentLoads()
    {
        // Arrange
        _tournamentsApi
            .Setup(x => x.GetTournamentAsync(TournamentId, It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<Refit.IApiResponse<TournamentDetailResponse>>().Task);

        // Act
        var cut = Render();

        // Assert
        cut.Markup.ShouldContain("Loading...");
    }

    [Fact(DisplayName = "Should navigate to /not-found when the tournament does not exist")]
    public void Render_ShouldNavigateToNotFound_WhenTournamentIsMissing()
    {
        // Arrange
        SetupTournamentFailure(System.Net.HttpStatusCode.NotFound);

        // Act
        Render();

        // Assert
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/not-found");
    }

    [Fact(DisplayName = "Should show an error alert when the tournament cannot be loaded")]
    public void Render_ShouldShowAlert_WhenTournamentFailsToLoad()
    {
        // Arrange
        SetupTournamentFailure(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = Render();

        // Assert
        cut.Markup.ShouldContain("Unable to load this tournament");
    }

    [Fact(DisplayName = "Should say the tournament is not a team event for a singles format")]
    public void Render_ShouldSayNotATeamEvent_ForSingles()
    {
        // Arrange
        SetupTournament(TournamentType.Singles, 1);

        // Act
        var cut = Render();

        // Assert
        cut.Find("h1").TextContent.ShouldBe("This is not a team event");
        cut.FindAll(".bowler-picker").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should show one picker per team member")]
    [InlineData(2, TestDisplayName = "Doubles")]
    [InlineData(3, TestDisplayName = "Trios")]
    [InlineData(5, TestDisplayName = "Baker")]
    public void Render_ShouldShowOnePickerPerMember(int teamSize)
    {
        // Arrange
        var type = TournamentType.List.Single(candidate => candidate.TeamSize == teamSize && candidate.ActiveFormat && candidate != TournamentType.OverUnderFiftyDoubles);
        SetupTournament(type, teamSize);

        // Act
        var cut = Render();

        // Assert
        cut.FindComponents<BowlerPicker>().Count.ShouldBe(teamSize);
        cut.Find("h1").TextContent.ShouldBe("Check team eligibility");
        cut.Find(".team-eligibility__lead").TextContent.ShouldContain("Joe's Pizza Open");
        cut.Markup.ShouldContain("All " + teamSize + " bowlers are required.");
    }

    [Fact(DisplayName = "Should show the team rules for the format")]
    public void Render_ShouldShowTeamRules()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);

        // Act
        var cut = Render();

        // Assert
        cut.Find(".team-rules__body").TextContent.ShouldContain("Rules text");
    }

    [Fact(DisplayName = "Should enable Check team only once every slot has a bowler or is marked as not in the system")]
    public async Task Check_ShouldEnableOnlyWhenEverySlotIsFilled()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        var cut = Render();
        AngleSharp.Dom.IElement button() => cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team");
        button().HasAttribute("disabled").ShouldBeTrue();

        // Act
        await PickAsync(cut, 0, Bowler(1));
        var afterOne = button().HasAttribute("disabled");
        await MarkUnknownAsync(cut, 1);

        // Assert
        afterOne.ShouldBeTrue();
        button().HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should send the chosen bowler IDs and the unknown count when Check team is clicked, and show the answer")]
    public async Task Check_ShouldSendTeamAndShowAnswer()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        IReadOnlyCollection<string>? sentIds = null;
        var sentUnknown = -1;
        SetupCheck(
            new TeamEligibilityResultViewModel { Outcome = TeamEligibilityOutcomeKind.Eligible },
            (ids, unknown) => { sentIds = ids; sentUnknown = unknown; });
        var cut = Render();
        await PickAsync(cut, 0, Bowler(1));
        await MarkUnknownAsync(cut, 1);

        // Act
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").ClickAsync();

        // Assert
        sentIds.ShouldBe(["id-1"]);
        sentUnknown.ShouldBe(1);
        cut.Markup.ShouldContain("This team can bowl together.");
    }

    [Fact(DisplayName = "Should show the reasons when the team is ineligible")]
    public async Task Check_ShouldShowReasons_WhenIneligible()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        SetupCheck(new TeamEligibilityResultViewModel
        {
            Outcome = TeamEligibilityOutcomeKind.Ineligible,
            Reasons = ["Too many champions."]
        });
        var cut = Render();
        await PickAsync(cut, 0, Bowler(1));
        await PickAsync(cut, 1, Bowler(2));

        // Act
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").ClickAsync();

        // Assert
        cut.Markup.ShouldContain("This team can't bowl together.");
        cut.Markup.ShouldContain("Too many champions.");
        cut.Markup.ShouldContain("info@bowlneba.com");
    }

    [Theory(DisplayName = "Should show the API's reason for a validation error and a generic message for any other error")]
    [InlineData(true, "This tournament needs exactly 2 bowlers.", TestDisplayName = "Validation")]
    [InlineData(false, "We couldn't check this team.", TestDisplayName = "Other")]
    public async Task Check_ShouldShowErrorMessage_WhenCheckFails(bool validation, string expected)
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        Error error = validation
            ? Error.Validation("TeamEligibility.TeamSizeMismatch", "This tournament needs exactly 2 bowlers.")
            : Error.Failure("x", "boom");
        SetupCheck(error);
        var cut = Render();
        await PickAsync(cut, 0, Bowler(1));
        await PickAsync(cut, 1, Bowler(2));

        // Act
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").ClickAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to check this team");
        cut.Markup.ShouldContain(expected);
        cut.FindAll(".result").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should clear the answer when a slot changes and when Check another team is clicked, keeping the slots")]
    public async Task Check_ShouldClearAnswer_WhenSlotChangesOrCheckAnother()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        SetupCheck(new TeamEligibilityResultViewModel { Outcome = TeamEligibilityOutcomeKind.Eligible });
        var cut = Render();
        await PickAsync(cut, 0, Bowler(1));
        await PickAsync(cut, 1, Bowler(2));
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").ClickAsync();
        cut.FindAll(".result").Count.ShouldBe(1);

        // Act
        await PickAsync(cut, 1, Bowler(3));
        var clearedByEdit = cut.FindAll(".result").Count;
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").ClickAsync();
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Check another team").ClickAsync();

        // Assert
        clearedByEdit.ShouldBe(0);
        cut.FindAll(".result").ShouldBeEmpty();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check team").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should pass the bowlers chosen in other slots so one bowler cannot fill two")]
    public async Task Render_ShouldExcludeChosenBowlers_FromOtherSlots()
    {
        // Arrange
        SetupTournament(TournamentType.Doubles, 2);
        var cut = Render();

        // Act
        await PickAsync(cut, 0, Bowler(1));

        // Assert
        cut.FindComponents<BowlerPicker>()[1].Instance.ExcludedIds.ShouldContain("id-1");
    }
}