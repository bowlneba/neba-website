using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TeamEligibilityEvaluatorTests
{
    private static readonly DateOnly Start = new(2026, 3, 7);
    private static readonly DateOnly End = new(2026, 3, 8);
    private static readonly DateOnly OverDateOfBirth = new(1960, 1, 1);
    private static readonly DateOnly UnderDateOfBirth = new(1990, 1, 1);

    private static TeamEligibilityContext Context(TournamentType type, DateOnly? start = null, DateOnly? end = null)
        => new(type, start ?? Start, end ?? End);

    private static NebaTitle Title(DateOnly date) => new(date, date);

    private static NebaTitle RecentTitle() => Title(new DateOnly(2024, 10, 5));

    private static TeamMemberFacts Champion(DateOnly? dateOfBirth = null, int titles = 1)
        => TeamMemberFactsFactory.Create(
            dateOfBirth: dateOfBirth,
            titles: [.. Enumerable.Range(0, titles).Select(index => Title(new DateOnly(2024 - index, 10, 5)))]);

    private static TeamMemberFacts NonChampion(DateOnly? dateOfBirth = null)
        => TeamMemberFactsFactory.Create(dateOfBirth: dateOfBirth);

    private static string[] Codes(TeamEligibilityResult result)
        => [.. result.Reasons.Select(reason => reason.Code)];

    // ---- Doubles (Rule 401)

    [Fact(DisplayName = "Evaluate returns Ineligible with Champions.TooMany when a doubles team has two champions in the last 10 years")]
    public void Evaluate_ShouldReturnTooManyChampions_WhenDoublesTeamHasTwoChampions()
    {
        // Arrange
        var members = new[] { Champion(), Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooMany"]);
    }

    [Fact(DisplayName = "Evaluate returns Eligible when a doubles team has one champion and one non-champion")]
    public void Evaluate_ShouldReturnEligible_WhenDoublesTeamHasOneChampion()
    {
        // Arrange
        var members = new[] { Champion(), NonChampion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
        result.Reasons.ShouldBeEmpty();
        result.DateChecks.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Evaluate uses a 5-year window for a doubles bowler over 60, so an 8-year-old title does not count")]
    public void Evaluate_ShouldUseFiveYearWindow_WhenDoublesBowlerIsOverSixty()
    {
        // Arrange
        var oldTitle = Title(new DateOnly(2018, 3, 1));
        var senior = TeamMemberFactsFactory.Create(dateOfBirth: new DateOnly(1962, 1, 1), titles: [oldTitle]);
        var members = new[] { senior, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate uses the 10-year window for a doubles bowler with no date of birth, with no date of birth in the reason")]
    public void Evaluate_ShouldUseTenYearWindow_WhenDoublesBowlerHasNoDateOfBirth()
    {
        // Arrange
        var oldTitle = Title(new DateOnly(2018, 3, 1));
        var unknownAge = new TeamMemberFacts { BowlerId = Neba.Api.Features.Bowlers.Domain.BowlerId.New(), Titles = [oldTitle] };
        var members = new[] { unknownAge, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooMany"]);
        result.Reasons.ShouldAllBe(reason => !reason.Message.Contains("birth", StringComparison.OrdinalIgnoreCase));
    }

    [Theory(DisplayName = "Evaluate counts a doubles title that ended exactly 10 years before the date, but not one day earlier")]
    [InlineData("2016-03-07", true, TestDisplayName = "Exactly 10 years before the start date")]
    [InlineData("2016-03-06", false, TestDisplayName = "One day earlier than 10 years")]
    public void Evaluate_ShouldHonorTenYearWindowEdge(string titleEnd, bool counts)
    {
        // Arrange
        var edge = TeamMemberFactsFactory.Create(titles: [Title(DateOnly.Parse(titleEnd, System.Globalization.CultureInfo.InvariantCulture))]);
        var members = new[] { edge, Champion() };
        var context = Context(TournamentType.Doubles, Start, Start);

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(context, members);

        // Assert
        result.Outcome.ShouldBe(counts ? TeamEligibilityOutcome.Ineligible : TeamEligibilityOutcome.Eligible);
    }

    [Theory(DisplayName = "Evaluate uses a 5-year window for Trios and counts a title that ended exactly 5 years before the date")]
    [InlineData("2021-03-07", true, TestDisplayName = "Exactly 5 years before")]
    [InlineData("2021-03-06", false, TestDisplayName = "One day earlier than 5 years")]
    public void Evaluate_ShouldHonorFiveYearWindowEdge_ForTrios(string titleEnd, bool counts)
    {
        // Arrange: the edge title is the only champion; with it counted the team has two non-champions left, so check multi-time instead.
        var edge = TeamMemberFactsFactory.Create(titles: [Title(DateOnly.Parse(titleEnd, System.Globalization.CultureInfo.InvariantCulture)), RecentTitle()]);
        var members = new[] { edge, Champion(titles: 2), NonChampion() };
        var context = Context(TournamentType.Trios, Start, Start);

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(context, members);

        // Assert: edge counts => two multi-time champions => ineligible; otherwise the edge bowler has one title => eligible.
        result.Outcome.ShouldBe(counts ? TeamEligibilityOutcome.Ineligible : TeamEligibilityOutcome.Eligible);
    }

    [Theory(DisplayName = "Evaluate switches a doubles bowler to the 5-year window on the 60th birthday")]
    [InlineData("1966-03-07", TestDisplayName = "Turns 60 on the date")]
    [InlineData("1960-01-01", TestDisplayName = "Turned 60 long ago")]
    public void Evaluate_ShouldUseFiveYearWindow_FromSixtiethBirthday(string dateOfBirth)
    {
        // Arrange
        var oldTitle = Title(new DateOnly(2018, 3, 1));
        var senior = TeamMemberFactsFactory.Create(
            dateOfBirth: DateOnly.Parse(dateOfBirth, System.Globalization.CultureInfo.InvariantCulture),
            titles: [oldTitle]);
        var members = new[] { senior, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles, Start, Start), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate keeps the 10-year window for a doubles bowler who turns 60 the day after the date")]
    public void Evaluate_ShouldKeepTenYearWindow_WhenSixtiethBirthdayIsTheNextDay()
    {
        // Arrange
        var oldTitle = Title(new DateOnly(2018, 3, 1));
        var almostSenior = TeamMemberFactsFactory.Create(dateOfBirth: new DateOnly(1966, 3, 8), titles: [oldTitle]);
        var members = new[] { almostSenior, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles, Start, Start), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
    }

    [Fact(DisplayName = "Evaluate ignores a title won in a tournament that overlaps this tournament's dates")]
    public void Evaluate_ShouldIgnoreTitle_WhenTournamentOverlapsSameWeekend()
    {
        // Arrange
        var sameWeekend = new NebaTitle(new DateOnly(2026, 3, 6), new DateOnly(2026, 3, 7));
        var winner = TeamMemberFactsFactory.Create(titles: [sameWeekend]);
        var members = new[] { winner, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate ignores this tournament's own title")]
    public void Evaluate_ShouldIgnoreOwnTitle()
    {
        // Arrange
        var ownTitle = new NebaTitle(Start, End);
        var winner = TeamMemberFactsFactory.Create(titles: [ownTitle]);
        var members = new[] { winner, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate ignores a title won after the date being checked")]
    public void Evaluate_ShouldIgnoreTitle_WhenWonAfterTheDate()
    {
        // Arrange
        var future = Title(new DateOnly(2026, 6, 1));
        var winner = TeamMemberFactsFactory.Create(titles: [future]);
        var members = new[] { winner, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate treats a bowler with no title data as a non-champion")]
    public void Evaluate_ShouldTreatNoTitleDataAsNonChampion()
    {
        // Arrange
        var members = new[] { Champion(), TeamMemberFactsFactory.Unknown() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    // ---- Over/Under (Rule 401 with the age rule)

    [Theory(DisplayName = "Evaluate returns Age.SameAgeGroup when both bowlers are over or both are under")]
    [InlineData(true, TestDisplayName = "Both over 50")]
    [InlineData(false, TestDisplayName = "Both under 50")]
    public void Evaluate_ShouldReturnSameAgeGroup_WhenBothInSameGroup(bool bothOver)
    {
        // Arrange
        var dateOfBirth = bothOver ? OverDateOfBirth : UnderDateOfBirth;
        var members = new[] { NonChampion(dateOfBirth), NonChampion(dateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Age.SameAgeGroup"]);
    }

    [Fact(DisplayName = "Evaluate returns Eligible when an over/under team has one over, one under and one champion")]
    public void Evaluate_ShouldReturnEligible_WhenOverUnderTeamIsValid()
    {
        // Arrange
        var members = new[] { Champion(OverDateOfBirth), NonChampion(UnderDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate returns only Age.NotVerified when an over/under bowler has no date of birth")]
    public void Evaluate_ShouldReturnAgeNotVerified_WhenDateOfBirthIsMissing()
    {
        // Arrange
        var noDateOfBirth = new TeamMemberFacts { BowlerId = Neba.Api.Features.Bowlers.Domain.BowlerId.New() };
        var members = new[] { noDateOfBirth, NonChampion(UnderDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Age.NotVerified"]);
    }

    [Fact(DisplayName = "Evaluate gives the same reason for a bowler not in the system as for a missing date of birth")]
    public void Evaluate_ShouldReturnSameReason_ForUnknownBowlerAndMissingDateOfBirth()
    {
        // Arrange
        var noDateOfBirth = new TeamMemberFacts { BowlerId = Neba.Api.Features.Bowlers.Domain.BowlerId.New() };
        var withMissing = new[] { noDateOfBirth, NonChampion(UnderDateOfBirth) };
        var withUnknown = new[] { TeamMemberFactsFactory.Unknown(), NonChampion(UnderDateOfBirth) };
        var context = Context(TournamentType.OverUnderFiftyDoubles);

        // Act
        var missingResult = TeamEligibilityEvaluator.Evaluate(context, withMissing);
        var unknownResult = TeamEligibilityEvaluator.Evaluate(context, withUnknown);

        // Assert
        unknownResult.Reasons.ShouldBe(missingResult.Reasons);
    }

    [Fact(DisplayName = "Evaluate skips the age rules when the champion rules already fail")]
    public void Evaluate_ShouldStopAtComposition_WhenChampionRulesFail()
    {
        // Arrange: two champions who are both over 50, so the age rule would fail too.
        var members = new[] { Champion(OverDateOfBirth), Champion(OverDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooMany"]);
    }

    [Fact(DisplayName = "Evaluate returns DependsOnDate when a bowler turns 50 between the start and end date")]
    public void Evaluate_ShouldReturnDependsOnDate_WhenBowlerTurnsFiftyDuringTournament()
    {
        // Arrange: bowler is under 50 on the start date and 50 on the end date, so both bowlers are over on the last day.
        var turnsFifty = NonChampion(new DateOnly(1976, 3, 8));
        var members = new[] { turnsFifty, NonChampion(OverDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.DependsOnDate);
        result.Reasons.ShouldBeEmpty();
        result.DateChecks.Select(check => check.AsOf).ShouldBe([Start, End]);
        result.DateChecks[0].Eligible.ShouldBeTrue();
        result.DateChecks[1].Eligible.ShouldBeFalse();
        result.DateChecks[1].Reasons.Select(reason => reason.Code).ShouldBe(["Age.SameAgeGroup"]);
    }

    [Fact(DisplayName = "Evaluate returns Eligible for an over/under team that is fine on both dates")]
    public void Evaluate_ShouldReturnEligible_WhenEligibleOnBothDates()
    {
        // Arrange
        var members = new[] { NonChampion(OverDateOfBirth), NonChampion(UnderDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
        result.DateChecks.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Evaluate returns Ineligible, not DependsOnDate, when the champion rules fail on only one date")]
    public void Evaluate_ShouldReturnIneligible_WhenCompositionFailsOnOnlyOneDate()
    {
        // Arrange: a title that ended on the start date's 10-year mark leaves the window on the end date.
        var fallsOut = TeamEligibilityFactsWithTitle(new DateOnly(2016, 3, 7));
        var members = new[] { fallsOut, Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooMany"]);
        result.DateChecks.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Evaluate removes duplicate reasons when the team is ineligible on both dates")]
    public void Evaluate_ShouldMergeDuplicateReasons_WhenIneligibleOnBothDates()
    {
        // Arrange
        var members = new[] { Champion(), Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles), members);

        // Assert
        result.Reasons.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Evaluate runs the rules once for a one-day tournament")]
    public void Evaluate_ShouldRunOnce_WhenTournamentIsOneDay()
    {
        // Arrange
        var members = new[] { Champion(), Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Doubles, Start, Start), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        result.DateChecks.ShouldBeEmpty();
    }

    // ---- Trios (Rule 402)

    [Fact(DisplayName = "Evaluate returns Champions.NoNonChampion when every Trios bowler is a champion and none is multi-time")]
    public void Evaluate_ShouldReturnNoNonChampion_WhenAllTriosBowlersAreChampions()
    {
        // Arrange
        var members = new[] { Champion(), Champion(), Champion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Trios), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.NoNonChampion"]);
    }

    [Fact(DisplayName = "Evaluate returns Champions.TooManyMultiTime when a Trios team has two multi-time champions")]
    public void Evaluate_ShouldReturnTooManyMultiTime_WhenTriosTeamHasTwoMultiTimeChampions()
    {
        // Arrange
        var members = new[] { Champion(titles: 2), Champion(titles: 2), NonChampion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Trios), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooManyMultiTime"]);
    }

    [Fact(DisplayName = "Evaluate returns Eligible for a Trios team with one multi-time champion, one champion and one non-champion")]
    public void Evaluate_ShouldReturnEligible_WhenTriosTeamIsValid()
    {
        // Arrange
        var members = new[] { Champion(titles: 2), Champion(), NonChampion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Trios), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    // ---- Baker (Rule 403)

    [Fact(DisplayName = "Evaluate returns HallOfFame.TooMany when a baker team has three Superior Performance members")]
    public void Evaluate_ShouldReturnTooManyHallOfFame_WhenBakerTeamHasThree()
    {
        // Arrange
        var members = new[]
        {
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            NonChampion(),
            NonChampion()
        };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Baker), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["HallOfFame.TooMany"]);
    }

    [Fact(DisplayName = "Evaluate counts unknown bowlers as non-champions but still counts the other members' Hall of Fame status")]
    public void Evaluate_ShouldReturnTooManyHallOfFame_WhenBakerTeamHasTwoSuperiorPerformanceAndThreeUnknown()
    {
        // Arrange
        var members = new[]
        {
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            TeamMemberFactsFactory.Unknown(),
            TeamMemberFactsFactory.Unknown(),
            TeamMemberFactsFactory.Unknown()
        };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Baker), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["HallOfFame.TooMany"]);
    }

    [Fact(DisplayName = "Evaluate returns Champions.TooManyMultiTime when a baker team has three multi-time champions")]
    public void Evaluate_ShouldReturnTooManyMultiTime_WhenBakerTeamHasThreeMultiTimeChampions()
    {
        // Arrange
        var members = new[] { Champion(titles: 2), Champion(titles: 2), Champion(titles: 2), NonChampion(), NonChampion() };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Baker), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        Codes(result).ShouldBe(["Champions.TooManyMultiTime"]);
    }

    [Fact(DisplayName = "Evaluate returns Eligible for a baker team of one Hall of Fame member, two multi-time champions and two non-champions")]
    public void Evaluate_ShouldReturnEligible_WhenBakerTeamIsValid()
    {
        // Arrange
        var members = new[]
        {
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            Champion(titles: 2),
            Champion(titles: 2),
            NonChampion(),
            NonChampion()
        };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Baker), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate does not require ages for a baker team")]
    public void Evaluate_ShouldNotCheckAge_ForBakerTeam()
    {
        // Arrange
        var members = TeamMemberFactsFactory.Bogus(5, seed: 11)
            .Select(member => member with { DateOfBirth = null })
            .ToList();

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.Baker), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "Evaluate uses an age limit of 40 for Over/Under 40 Doubles")]
    public void Evaluate_ShouldUseFortyAgeLimit_ForOverUnderFortyDoubles()
    {
        // Arrange: 45 would be "under" in the 50 format but is "over" in the 40 format.
        var members = new[] { NonChampion(new DateOnly(1981, 1, 1)), NonChampion(UnderDateOfBirth) };

        // Act
        var result = TeamEligibilityEvaluator.Evaluate(Context(TournamentType.OverUnderFortyDoubles), members);

        // Assert
        result.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    private static TeamMemberFacts TeamEligibilityFactsWithTitle(DateOnly titleDate)
        => TeamMemberFactsFactory.Create(titles: [Title(titleDate)]);

    // ---- Explain (the facts shown with every answer)

    [Fact(DisplayName = "Explain shows both title windows for every doubles bowler, whatever the bowler's age")]
    public void Explain_ShouldShowBothWindows_ForDoubles()
    {
        // Arrange: one title 7 years before the start date. A 62-year-old uses the 5-year window, a 40-year-old the 10-year one.
        var sevenYearsAgo = Title(new DateOnly(2019, 3, 1));
        var senior = TeamMemberFactsFactory.Create(dateOfBirth: new DateOnly(1962, 1, 1), titles: [sevenYearsAgo]);
        var younger = TeamMemberFactsFactory.Create(dateOfBirth: new DateOnly(1986, 1, 1), titles: [sevenYearsAgo]);

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Doubles), [senior, younger]);

        // Assert
        explanation.Members.Select(member => member.TitlesLast5Years).ShouldBe([0, 0]);
        explanation.Members.Select(member => member.TitlesLast10Years).ShouldBe([1, 1]);
        explanation.Members.Select(member => member.CountsAs).ShouldBe([ChampionStanding.NonChampion, ChampionStanding.OneTimeChampion]);
        explanation.Members.ShouldAllBe(member => member.LastTitleYear == 2019);
    }

    [Fact(DisplayName = "Explain leaves out the 10-year count, the age fact and the Hall of Fame fact when the format does not use them")]
    public void Explain_ShouldLeaveOutFactsThatDoNotApply()
    {
        // Arrange
        var members = new[] { Champion(), NonChampion(), NonChampion() };

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios), members);

        // Assert
        explanation.Members.ShouldAllBe(member => member.TitlesLast10Years == null && member.AgeVerified == null && member.HallOfFameSuperiorPerformance == null);
    }

    [Fact(DisplayName = "Explain tallies the trios rules and marks the failing one")]
    public void Explain_ShouldTallyTriosRules()
    {
        // Arrange
        var members = new[] { Champion(titles: 2), Champion(titles: 2), NonChampion() };

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios), members);

        // Assert
        explanation.Checks.Select(check => (check.Label, check.Passed)).ShouldBe(
        [
            ("Non-champions: 1 (needs at least 1)", true),
            ("Multi-time champions: 2 (at most 1)", false)
        ]);
    }

    [Fact(DisplayName = "Explain tallies the baker rules, with the Hall of Fame count, and marks Hall of Fame members")]
    public void Explain_ShouldTallyBakerRules()
    {
        // Arrange
        var members = new[]
        {
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
            NonChampion(), NonChampion(), TeamMemberFactsFactory.Unknown()
        };

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Baker), members);

        // Assert
        explanation.Checks[^1].ShouldBe(new TeamEligibilityCheckLine("Hall of Fame members for Superior Performance: 2 (at most 1)", false));
        explanation.Members.Select(member => member.HallOfFameSuperiorPerformance).ShouldBe([true, true, false, false, false]);
    }

    [Fact(DisplayName = "Explain tallies how many over/under bowlers have an age we can check, without saying who is over or under")]
    public void Explain_ShouldTallyAgeCheck_ForOverUnder()
    {
        // Arrange
        var members = new[] { NonChampion(OverDateOfBirth), TeamMemberFactsFactory.Unknown() };

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.OverUnderFiftyDoubles), members);

        // Assert
        explanation.Checks[^1].ShouldBe(new TeamEligibilityCheckLine("Bowlers with an age we can check: 1 of 2 (needs all)", false));
        explanation.Members.Select(member => member.AgeVerified).ShouldBe([true, false]);
    }

    [Fact(DisplayName = "Explain describes the champion window and the dates checked")]
    public void Explain_ShouldDescribeWindowAndDates()
    {
        // Arrange & Act
        var doubles = TeamEligibilityEvaluator.Explain(Context(TournamentType.Doubles), [NonChampion(), NonChampion()]);
        var oneDay = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios, Start, Start), [NonChampion(), NonChampion(), NonChampion()]);

        // Assert
        doubles.Summary.ShouldContain("10 years");
        doubles.Summary.ShouldContain("5 years once a bowler has turned 60");
        doubles.Summary.ShouldContain("The dates checked are Mar 7, 2026 and Mar 8, 2026.");
        oneDay.Summary.ShouldContain("5 years");
        oneDay.Summary.ShouldContain("The date checked is Mar 7, 2026.");
    }

    [Fact(DisplayName = "Explain gives the facts for a bowler with no titles and no date of birth without a last title year")]
    public void Explain_ShouldHandleUnknownBowler()
    {
        // Arrange & Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios), [TeamMemberFactsFactory.Unknown(), NonChampion(), NonChampion()]);

        // Assert
        var unknown = explanation.Members[0];
        unknown.LastTitleYear.ShouldBeNull();
        unknown.TitlesLast5Years.ShouldBe(0);
        unknown.CountsAs.ShouldBe(ChampionStanding.NonChampion);
    }

    [Fact(DisplayName = "Explain marks only the multi-time champions when a trios team has too many of them")]
    public void Explain_ShouldMarkMultiTimeChampions_WhenTriosRuleFails()
    {
        // Arrange
        var members = new[] { Champion(titles: 2), Champion(titles: 2), NonChampion() };

        // Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios), members);

        // Assert
        explanation.Members.Select(member => member.TitlesBreakRule).ShouldBe([true, true, false]);
    }

    [Fact(DisplayName = "Explain marks every champion when a doubles team has two")]
    public void Explain_ShouldMarkChampions_WhenDoublesRuleFails()
    {
        // Arrange & Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Doubles), [Champion(), Champion()]);

        // Assert
        explanation.Members.ShouldAllBe(member => member.TitlesBreakRule);
    }

    [Fact(DisplayName = "Explain marks every bowler when a trios team has no non-champion")]
    public void Explain_ShouldMarkAll_WhenNoNonChampion()
    {
        // Arrange & Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Trios), [Champion(), Champion(), Champion()]);

        // Assert
        explanation.Members.ShouldAllBe(member => member.TitlesBreakRule);
    }

    [Fact(DisplayName = "Explain marks nothing when the team passes")]
    public void Explain_ShouldMarkNothing_WhenTeamPasses()
    {
        // Arrange & Act
        var explanation = TeamEligibilityEvaluator.Explain(Context(TournamentType.Doubles), [Champion(), NonChampion()]);

        // Assert
        explanation.Members.ShouldAllBe(member => !member.TitlesBreakRule && !member.AgeBreaksRule && !member.HallOfFameBreaksRule);
    }

    [Fact(DisplayName = "Explain marks the bowlers without an age, and the Hall of Fame members when the limit is broken")]
    public void Explain_ShouldMarkAgeAndHallOfFame()
    {
        // Arrange
        var overUnder = TeamEligibilityEvaluator.Explain(
            Context(TournamentType.OverUnderFiftyDoubles), [NonChampion(OverDateOfBirth), TeamMemberFactsFactory.Unknown()]);
        var baker = TeamEligibilityEvaluator.Explain(
            Context(TournamentType.Baker),
            [
                TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
                TeamMemberFactsFactory.Create(hallOfFameSuperiorPerformance: true),
                NonChampion(), NonChampion(), NonChampion()
            ]);

        // Assert
        overUnder.Members.Select(member => member.AgeBreaksRule).ShouldBe([false, true]);
        baker.Members.Select(member => member.HallOfFameBreaksRule).ShouldBe([true, true, false, false, false]);
    }
}