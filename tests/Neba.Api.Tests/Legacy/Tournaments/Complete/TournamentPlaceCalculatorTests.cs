using Neba.Api.Legacy.Tournaments.Complete;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

// This is the one file that locks down every case of "who gets which Place when the legacy row
// doesn't already have one" - singles cut-fill, team roster-composite scoring, forfeits, partner
// swaps, and re-entries. TournamentPlaceCalculator has no I/O, so every case below is a plain
// constructed-input/expected-output assertion.

[UnitTest]
[Component("Legacy")]
public sealed class TournamentPlaceCalculatorTests
{
    private static readonly IReadOnlyCollection<LegacyTeamRow> NoTeams = [];
    private static readonly IReadOnlyCollection<LegacyTeamMemberRow> NoTeamMembers = [];
    private static readonly IReadOnlyCollection<LegacyTeamSquadRow> NoTeamSquads = [];

    // --- Singles ---

    [Fact(DisplayName = "ComputePlaces should leave already-placed bowlers unchanged and fill missing ones from max(Place)+1")]
    public void ComputePlaces_ShouldLeaveAlreadyPlacedUnchangedAndFillMissingSequentially_ForSingles()
    {
        // Arrange
        var results = new List<LegacyResultRow>
        {
            new(BowlerId: 1, Place: 1, PrizeMoney: 500, Points: 100),
            new(BowlerId: 2, Place: 2, PrizeMoney: 300, Points: 90),
            new(BowlerId: 3, Place: null, PrizeMoney: 0, Points: 50),
            new(BowlerId: 4, Place: null, PrizeMoney: 0, Points: 50)
        };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(BowlerId: 3, SquadId: 1, Score: 1200, Games: 6, HighGame: 220),
            new(BowlerId: 4, SquadId: 1, Score: 1300, Games: 6, HighGame: 230)
        };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert
        places[1].ShouldBe(1);
        places[2].ShouldBe(2);
        places[4].ShouldBe(3); // higher Score ranks ahead of bowler 3
        places[3].ShouldBe(4);
    }

    [Fact(DisplayName = "ComputePlaces should rank missing singles bowlers by Games desc, then Score desc, then HighGame desc")]
    public void ComputePlaces_ShouldRankMissingSinglesBowlers_ByGamesThenScoreThenHighGame()
    {
        // Arrange
        var results = new List<LegacyResultRow>
        {
            new(1, null, 0, 50),
            new(2, null, 0, 50),
            new(3, null, 0, 50)
        };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, 1, Score: 1000, Games: 4, HighGame: 200), // fewer games than 2/3 - ranks last
            new(2, 1, Score: 1100, Games: 6, HighGame: 200), // ties bowler 3 on Games/Score, loses on HighGame
            new(3, 1, Score: 1100, Games: 6, HighGame: 210)
        };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert
        places[3].ShouldBe(1);
        places[2].ShouldBe(2);
        places[1].ShouldBe(3);
    }

    [Fact(DisplayName = "ComputePlaces should reduce a singles bowler's re-entries to their highest-Score entry before ranking")]
    public void ComputePlaces_ShouldReduceSinglesReentriesToHighestScoreEntry()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            // Bowler 1 re-entered: squad 1 attempt is worse than squad 2 attempt.
            new(1, SquadId: 1, Score: 900, Games: 6, HighGame: 180),
            new(1, SquadId: 2, Score: 1400, Games: 6, HighGame: 250),
            new(2, SquadId: 1, Score: 1300, Games: 6, HighGame: 240)
        };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert - bowler 1's counting entry (1400) outranks bowler 2 (1300)
        places[1].ShouldBe(1);
        places[2].ShouldBe(2);
    }

    [Fact(DisplayName = "ComputePlaces should break a tie between a singles bowler's own re-entries using the higher HighGame")]
    public void ComputePlaces_ShouldBreakTieBetweenOwnReentries_UsingHigherHighGame()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            // Bowler 1's two entries tie on Score (1200) - the one with the higher HighGame (240) counts.
            new(1, SquadId: 1, Score: 1200, Games: 6, HighGame: 200),
            new(1, SquadId: 2, Score: 1200, Games: 6, HighGame: 240),
            new(2, SquadId: 1, Score: 1200, Games: 6, HighGame: 230)
        };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert - bowler 1's counting HighGame (240) beats bowler 2's (230)
        places[1].ShouldBe(1);
        places[2].ShouldBe(2);
    }

    [Fact(DisplayName = "ComputePlaces should exclude a singles bowler with no qualifying row at all")]
    public void ComputePlaces_ShouldExcludeSinglesBowler_WhenNoQualifyingRowExists()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, [], NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert
        places.ContainsKey(1).ShouldBeFalse();
    }

    [Fact(DisplayName = "ComputePlaces should start singles fill at 1 when no bowler already has a Place")]
    public void ComputePlaces_ShouldStartSinglesFillAtOne_WhenNoBowlerAlreadyHasPlace()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow> { new(1, 1, Score: 1000, Games: 6, HighGame: 200) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, NoTeams, NoTeamMembers, NoTeamSquads);

        // Assert
        places[1].ShouldBe(1);
    }

    // --- Team ---

    [Fact(DisplayName = "ComputePlaces should sum a roster's members' scores for the same squad, never a different squad")]
    public void ComputePlaces_ShouldSumRosterMembersScores_OnlyForTheSameSquad()
    {
        // Arrange - roster {1,2} bowled squad 1 together (composite 1900). Bowler 2 also has a
        // qualifying row on squad 2 from a *different* roster - that must never contribute here.
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, SquadId: 1, Score: 1000, Games: 6, HighGame: 200),
            new(2, SquadId: 1, Score: 900, Games: 6, HighGame: 190),
            new(2, SquadId: 2, Score: 5000, Games: 6, HighGame: 300) // must not leak into team 100's composite
        };
        var teams = new List<LegacyTeamRow> { new(TeamId: 100, Forfeit: false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(TeamId: 100, SquadId: 1, HighGame: 210) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert - both roster members share the one computed place regardless of member 2's other roster's score
        places[1].ShouldBe(1);
        places[2].ShouldBe(1);
    }

    [Fact(DisplayName = "ComputePlaces should reduce a roster's re-entries to its single best composite squad-entry")]
    public void ComputePlaces_ShouldReduceRosterReentries_ToBestCompositeSquadEntry()
    {
        // Arrange - roster A/B: squad 1 (A=1000,B=900 => 1900), squad 2 (A=950,B=1200 => 2150).
        // Squad 2 wins, and its own HighGame (999) travels with it, not squad 1's.
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, SquadId: 1, Score: 1000, Games: 6, HighGame: 200),
            new(2, SquadId: 1, Score: 900, Games: 6, HighGame: 190),
            new(1, SquadId: 2, Score: 950, Games: 6, HighGame: 210),
            new(2, SquadId: 2, Score: 1200, Games: 6, HighGame: 260)
        };
        var teams = new List<LegacyTeamRow> { new(100, false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100) };
        var teamSquads = new List<LegacyTeamSquadRow>
        {
            new(100, SquadId: 1, HighGame: 205),
            new(100, SquadId: 2, HighGame: 999)
        };

        // Also add a lower-ranked competing roster so the winning squad's rank is observable.
        var competitorResults = new List<LegacyResultRow> { new(3, null, 0, 50), new(4, null, 0, 50) };
        results.AddRange(competitorResults);
        qualifying.Add(new LegacyQualifyingRow(3, SquadId: 1, Score: 100, Games: 6, HighGame: 50));
        qualifying.Add(new LegacyQualifyingRow(4, SquadId: 1, Score: 100, Games: 6, HighGame: 50));
        teams.Add(new LegacyTeamRow(200, false));
        teamMembers.Add(new LegacyTeamMemberRow(3, 200));
        teamMembers.Add(new LegacyTeamMemberRow(4, 200));
        teamSquads.Add(new LegacyTeamSquadRow(200, SquadId: 1, HighGame: 60));

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert - roster 100's counting composite is squad 2's 2150, which outranks roster 200's 200
        places[1].ShouldBe(1);
        places[2].ShouldBe(1);
        places[3].ShouldBe(2);
        places[4].ShouldBe(2);
    }

    [Fact(DisplayName = "ComputePlaces should exclude a forfeited roster from ranking even if it has the highest composite score")]
    public void ComputePlaces_ShouldExcludeForfeitedRoster_EvenWithHighestComposite()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50), new(3, null, 0, 50), new(4, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, 1, Score: 9999, Games: 6, HighGame: 300), // forfeited roster - highest score, must not rank
            new(2, 1, Score: 9999, Games: 6, HighGame: 300),
            new(3, 1, Score: 100, Games: 6, HighGame: 50),
            new(4, 1, Score: 100, Games: 6, HighGame: 50)
        };
        var teams = new List<LegacyTeamRow> { new(100, Forfeit: true), new(200, Forfeit: false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100), new(3, 200), new(4, 200) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 300), new(200, 1, HighGame: 50) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert - the forfeited roster's members get the shared last place, not rank 1
        places[3].ShouldBe(1);
        places[4].ShouldBe(1);
        places[1].ShouldBe(2);
        places[2].ShouldBe(2);
    }

    [Fact(DisplayName = "ComputePlaces should place a bowler through their surviving roster after a partner swap and give the abandoned partner the shared last place")]
    public void ComputePlaces_ShouldPlaceThroughSurvivingRoster_AfterPartnerSwap()
    {
        // Arrange - {A,B} forfeited (B has no other roster), {A,C} is A's counting roster.
        const int bowlerA = 1;
        const int bowlerB = 2;
        const int bowlerC = 3;
        var results = new List<LegacyResultRow> { new(bowlerA, null, 0, 50), new(bowlerB, null, 0, 50), new(bowlerC, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(bowlerA, SquadId: 1, Score: 900, Games: 6, HighGame: 180),  // A/B squad
            new(bowlerB, SquadId: 1, Score: 850, Games: 6, HighGame: 170),
            new(bowlerA, SquadId: 2, Score: 1200, Games: 6, HighGame: 230), // A/C squad
            new(bowlerC, SquadId: 2, Score: 1100, Games: 6, HighGame: 220)
        };
        var teams = new List<LegacyTeamRow> { new(TeamId: 10, Forfeit: true), new(TeamId: 20, Forfeit: false) };
        var teamMembers = new List<LegacyTeamMemberRow>
        {
            new(bowlerA, 10), new(bowlerB, 10),
            new(bowlerA, 20), new(bowlerC, 20)
        };
        var teamSquads = new List<LegacyTeamSquadRow> { new(10, SquadId: 1, HighGame: 190), new(20, SquadId: 2, HighGame: 240) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert - A and C are placed together via roster 20; B (no non-forfeited roster) is not part of that rank.
        places[bowlerA].ShouldBe(1);
        places[bowlerC].ShouldBe(1);
        places[bowlerB].ShouldBe(2); // shared last place, one past the only ranked roster
    }

    [Fact(DisplayName = "ComputePlaces should give every bowler with no non-forfeited roster the same shared last place")]
    public void ComputePlaces_ShouldGiveSharedLastPlace_ToEveryBowlerWithNoNonForfeitedRoster()
    {
        // Arrange - two entirely separate rosters, both forfeited; no ranked rosters at all.
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50), new(3, null, 0, 50) };
        var teams = new List<LegacyTeamRow> { new(100, Forfeit: true), new(200, Forfeit: true) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100), new(3, 200) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 200), new(200, 1, HighGame: 190) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, [], teams, teamMembers, teamSquads);

        // Assert - all three share the same place (nextPlace, starting at 1 since none were pre-placed)
        places[1].ShouldBe(1);
        places[2].ShouldBe(1);
        places[3].ShouldBe(1);
    }

    [Fact(DisplayName = "ComputePlaces should assign the same Place to every member of a roster where all members are missing Place")]
    public void ComputePlaces_ShouldAssignSamePlace_ToEveryMemberOfRoster()
    {
        // Arrange
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50), new(3, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, 1, Score: 500, Games: 3, HighGame: 200),
            new(2, 1, Score: 400, Games: 3, HighGame: 190)
            // bowler 3 has no qualifying row at all - still gets the roster's place
        };
        var teams = new List<LegacyTeamRow> { new(100, false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100), new(3, 100) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 210) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert
        places[1].ShouldBe(1);
        places[2].ShouldBe(1);
        places[3].ShouldBe(1);
    }

    [Fact(DisplayName = "ComputePlaces should break a tie between two rosters using HighGame after Games and Score both tie")]
    public void ComputePlaces_ShouldBreakTieBetweenRosters_UsingHighGame()
    {
        // Arrange - both rosters have identical composite Games/Score; roster 200's HighGame is higher.
        var results = new List<LegacyResultRow> { new(1, null, 0, 50), new(2, null, 0, 50), new(3, null, 0, 50), new(4, null, 0, 50) };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(1, 1, Score: 500, Games: 3, HighGame: 200),
            new(2, 1, Score: 500, Games: 3, HighGame: 200),
            new(3, 1, Score: 500, Games: 3, HighGame: 210),
            new(4, 1, Score: 500, Games: 3, HighGame: 210)
        };
        var teams = new List<LegacyTeamRow> { new(100, false), new(200, false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(2, 100), new(3, 200), new(4, 200) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 205), new(200, 1, HighGame: 215) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert - roster 200's higher HighGame (215) wins the tie
        places[3].ShouldBe(1);
        places[4].ShouldBe(1);
        places[1].ShouldBe(2);
        places[2].ShouldBe(2);
    }

    [Fact(DisplayName = "ComputePlaces should throw when a bowler belongs to two non-forfeited rosters at once")]
    public void ComputePlaces_ShouldThrow_WhenBowlerBelongsToTwoNonForfeitedRosters()
    {
        // Arrange - data anomaly asserted not to happen under tournament rules; must surface, not resolve silently.
        var results = new List<LegacyResultRow> { new(1, null, 0, 50) };
        var teams = new List<LegacyTeamRow> { new(100, false), new(200, false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(1, 100), new(1, 200) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 200), new(200, 1, HighGame: 200) };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            TournamentPlaceCalculator.ComputePlaces(results, [], teams, teamMembers, teamSquads));
    }

    [Fact(DisplayName = "ComputePlaces should start team fill at 1 past the highest already-assigned Place")]
    public void ComputePlaces_ShouldStartTeamFill_OnePastHighestAlreadyAssignedPlace()
    {
        // Arrange - bowlers 1/2 already placed 1st/2nd by hand (match play); roster 100 is the first cut team.
        var results = new List<LegacyResultRow>
        {
            new(1, 1, 1000, 200),
            new(2, 2, 500, 150),
            new(3, null, 0, 50),
            new(4, null, 0, 50)
        };
        var qualifying = new List<LegacyQualifyingRow>
        {
            new(3, 1, Score: 500, Games: 3, HighGame: 200),
            new(4, 1, Score: 400, Games: 3, HighGame: 190)
        };
        var teams = new List<LegacyTeamRow> { new(100, false) };
        var teamMembers = new List<LegacyTeamMemberRow> { new(3, 100), new(4, 100) };
        var teamSquads = new List<LegacyTeamSquadRow> { new(100, 1, HighGame: 210) };

        // Act
        var places = TournamentPlaceCalculator.ComputePlaces(results, qualifying, teams, teamMembers, teamSquads);

        // Assert
        places[3].ShouldBe(3);
        places[4].ShouldBe(3);
    }
}