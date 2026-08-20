using Neba.Api.Legacy.Tournaments.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Stats;

[UnitTest]
[Component("Legacy")]
public sealed class LegacySeasonStatsCalculatorTests
{
    private static readonly DateOnly SeasonEndDate = new(2026, 12, 31);
    private const int NewMembershipTypeId = 999;

    private static LegacyBowlerSeasonStatsResult ComputeSingle(
        IReadOnlyCollection<LegacySeasonTournamentRow>? seasonTournaments = null,
        IReadOnlyCollection<LegacyQualifyingStatsRow>? qualifyingStats = null,
        IReadOnlyCollection<LegacyMatchPlayStatsRow>? matchPlayStats = null,
        IReadOnlyCollection<LegacyBowlerResultRow>? results = null,
        IReadOnlyCollection<LegacyBowlerRow>? bowlers = null,
        IReadOnlyCollection<LegacyMembershipRow>? memberships = null,
        IReadOnlyCollection<LegacyCreditRow>? credits = null,
        IReadOnlyCollection<LegacyCupResultRow>? cupResults = null)
    {
        var computed = LegacySeasonStatsCalculator.Compute(
            SeasonEndDate,
            NewMembershipTypeId,
            seasonTournaments ?? [],
            qualifyingStats ?? [],
            matchPlayStats ?? [],
            results ?? [],
            bowlers ?? [],
            memberships ?? [],
            credits ?? [],
            cupResults ?? []);

        return computed.Single();
    }

    [Fact(DisplayName = "Compute should mark a bowler a member when their membership row's EndDate matches the season's EndDate")]
    public void Compute_ShouldMarkMember_WhenMembershipEndDateMatchesSeasonEndDate()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var memberships = new[] { new LegacyMembershipRow(1, 1, SeasonEndDate) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, memberships: memberships);

        // Assert
        result.IsMember.ShouldBeTrue();
    }

    [Fact(DisplayName = "Compute should mark a bowler a rookie when the member's most recent membership is a New Member type")]
    public void Compute_ShouldMarkRookie_WhenMostRecentMembershipIsNewMemberType()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var memberships = new[]
        {
            new LegacyMembershipRow(1, 1, SeasonEndDate.AddYears(-1)),
            new LegacyMembershipRow(1, NewMembershipTypeId, SeasonEndDate)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, memberships: memberships);

        // Assert
        result.IsMember.ShouldBeTrue();
        result.IsRookie.ShouldBeTrue();
    }

    [Fact(DisplayName = "Compute should not mark a bowler a rookie when they are not a member for this season")]
    public void Compute_ShouldNotMarkRookie_WhenNotAMember()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var memberships = new[] { new LegacyMembershipRow(1, NewMembershipTypeId, SeasonEndDate.AddYears(-1)) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, memberships: memberships);

        // Assert
        result.IsMember.ShouldBeFalse();
        result.IsRookie.ShouldBeFalse();
    }

    [Theory(DisplayName = "Compute should classify Senior/SuperSenior by age as of the season end date")]
    [InlineData(1976, 8, 19, true, false, TestDisplayName = "Age 50 - Senior only")]
    [InlineData(1966, 8, 19, true, true, TestDisplayName = "Age 60 - Senior and SuperSenior")]
    public void Compute_ShouldClassifyByAge_WhenDateOfBirthProvided(
        int year, int month, int day, bool expectedSenior, bool expectedSuperSenior)
    {
        // Arrange - Age 60 also crosses the Senior threshold, so Senior and SuperSenior are not
        // mutually exclusive - assert both independently rather than assuming otherwise.
        var dateOfBirth = new DateOnly(year, month, day);
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, bowlers: bowlers);

        // Assert
        result.IsSenior.ShouldBe(expectedSenior);
        result.IsSuperSenior.ShouldBe(expectedSuperSenior);
    }

    [Fact(DisplayName = "Compute should not classify Senior/SuperSenior when DateOfBirth is null")]
    public void Compute_ShouldNotClassifyByAge_WhenDateOfBirthIsNull()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, null) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, bowlers: bowlers);

        // Assert
        result.IsSenior.ShouldBeFalse();
        result.IsSuperSenior.ShouldBeFalse();
    }

    [Fact(DisplayName = "Compute should classify IsYouth from earned YouthOfTheYearPoints, not raw age")]
    public void Compute_ShouldClassifyYouth_FromEarnedYouthPoints_NotRawAge()
    {
        // Arrange - a 16-year-old with qualifying stats but no result in any youth-eligible
        // tournament earns zero YouthOfTheYearPoints, so IsYouth must be false despite being under 18.
        var dateOfBirth = new DateOnly(2010, 1, 1);
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, bowlers: bowlers);

        // Assert
        result.IsYouth.ShouldBeFalse();
        result.YouthOfTheYearPoints.ShouldBe(0);
    }

    [Fact(DisplayName = "Compute should classify a bowler as Woman when legacy Gender is 1")]
    public void Compute_ShouldClassifyWoman_WhenGenderIsOne()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var bowlers = new[] { new LegacyBowlerRow(1, 1, null) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, bowlers: bowlers);

        // Assert
        result.IsWoman.ShouldBeTrue();
    }

    [Fact(DisplayName = "Compute should count only stat-eligible tournaments toward EligibleTournaments, but every entered tournament toward TotalTournaments")]
    public void Compute_ShouldSplitEligibleAndTotalTournaments_ByYearlyStatEligible()
    {
        // Arrange
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, false, 1)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 180, 1, 180)
        };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying);

        // Assert
        result.TotalTournaments.ShouldBe(2);
        result.EligibleTournaments.ShouldBe(1);
        result.TotalEntries.ShouldBe(2);
        result.EligibleEntries.ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should count Cashes as the number of results rows with PrizeMoney greater than zero")]
    public void Compute_ShouldCountCashes_AsResultsWithPositivePrizeMoney()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 10, null),
            new LegacyBowlerResultRow(1, 101, 8, 0m, 5, null)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, results: results);

        // Assert
        result.Cashes.ShouldBe(1);
    }

    [Fact(DisplayName = "Compute should only include qualifying rows with exactly 5 games toward HighBlock")]
    public void Compute_ShouldOnlyIncludeExactlyFiveGameRows_TowardHighBlock()
    {
        // Arrange - the 6-game block scores higher but is excluded, matching the Software's own
        // inherited limitation (no sliding-window computation) documented in the plan.
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 900, 5, 220),
            new LegacyQualifyingStatsRow(1, 101, 2, 1100, 6, 220)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying);

        // Assert
        result.HighBlock.ShouldBe(900);
    }

    [Fact(DisplayName = "Compute should leave HighFinish and AverageFinish null when the bowler has no results rows")]
    public void Compute_ShouldLeaveFinishStatsNull_WhenNoResultsRows()
    {
        // Arrange - qualifying/match-play stats with no corresponding TournamentResult (e.g. an
        // unmapped or unplaceable bowler upstream) - the defensive case the plan calls out.
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying);

        // Assert
        result.HighFinish.ShouldBeNull();
        result.AverageFinish.ShouldBeNull();
    }

    [Fact(DisplayName = "Compute should set HighFinish to the best (lowest) Place and AverageFinish to the mean Place across all results")]
    public void Compute_ShouldComputeHighAndAverageFinish_FromResultsRows()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 2, 50m, 10, null),
            new LegacyBowlerResultRow(1, 101, 8, 0m, 5, null)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, results: results);

        // Assert
        result.HighFinish.ShouldBe(2);
        result.AverageFinish.ShouldBe(5m);
    }

    [Fact(DisplayName = "Compute should sum TournamentWinnings across all results")]
    public void Compute_ShouldSumTournamentWinnings_AcrossAllResults()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 10, null),
            new LegacyBowlerResultRow(1, 101, 1, 500m, 20, null)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, results: results);

        // Assert
        result.TournamentWinnings.ShouldBe(550m);
    }

    [Fact(DisplayName = "Compute should only include CupResults whose Cup.End year matches the season's end year toward CupEarnings")]
    public void Compute_ShouldOnlyIncludeMatchingCupYear_TowardCupEarnings()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var cupResults = new[]
        {
            new LegacyCupResultRow(1, 100m, new DateTime(SeasonEndDate.Year, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            new LegacyCupResultRow(1, 250m, new DateTime(SeasonEndDate.Year - 1, 6, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, cupResults: cupResults);

        // Assert
        result.CupEarnings.ShouldBe(100m);
    }

    [Fact(DisplayName = "Compute should sum only Taxable credit rows already filtered by the caller toward Credits")]
    public void Compute_ShouldSumCreditRows_TowardCredits()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var credits = new[]
        {
            new LegacyCreditRow(1, 25m),
            new LegacyCreditRow(1, 10m)
        };

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, credits: credits);

        // Assert
        result.Credits.ShouldBe(35m);
    }

    [Fact(DisplayName = "Compute should exclude the qualifying/eligible tournament count and entries, but not the total counts, for the Non-Champions winner's forced Tournament of Champions berth")]
    public void Compute_ShouldExcludeTocFromEligibleCountsOnly_ForNonChampionsWinner()
    {
        // Arrange - the Non-Champions single-day winner earns a bye into TOC; that forced entry
        // must not count as a second eligible tournament/entry for the winner, but the raw Total
        // counts (which include every tournament entered, eligible or not) are unaffected.
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 1), // Non-Champions, single-day
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, true, 4) // Champions (TOC)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 180, 1, 180)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 1, 500m, 100, null), // won the Non-Champions event
            new LegacyBowlerResultRow(1, 101, 5, 0m, 20, null)
        };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results);

        // Assert
        result.EligibleTournaments.ShouldBe(1);
        result.EligibleEntries.ShouldBe(1);
        result.TotalTournaments.ShouldBe(2);
        result.TotalEntries.ShouldBe(2);
        // TOC points are excluded entirely for the winner - only the Non-Champions win's 100 points count.
        result.BowlerOfTheYearPoints.ShouldBe(100);
    }

    [Fact(DisplayName = "Compute should compute BowlerOfTheYearPoints from main-cut results only, plus a flat bonus per side-cut finals appearance")]
    public void Compute_ShouldComputeBowlerOfTheYearPoints_FromMainCutPlusSideCutBonus()
    {
        // Arrange
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, true, 0)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 180, 1, 180)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 30, null), // main cut - counts in full
            new LegacyBowlerResultRow(1, 101, 8, 0m, 15, 1) // advanced via a Senior side cut - flat bonus only
        };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results);

        // Assert - 30 (main cut) + 5 (one side-cut finals appearance), the side-cut row's own 15 points excluded.
        result.BowlerOfTheYearPoints.ShouldBe(35);
    }

    [Fact(DisplayName = "Compute should scope SeniorOfTheYearPoints per-tournament by the bowler's age as of each tournament's end date")]
    public void Compute_ShouldScopeSeniorPoints_ByAgeAsOfEachTournamentEndDate()
    {
        // Arrange - the bowler turns 50 between the two tournaments; only the second (where they
        // were already 50 as of that tournament's own end date) counts toward Senior points, even
        // though both are ordinary eligible tournaments they entered during the same season.
        var dateOfBirth = new DateOnly(1976, 6, 1);
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 0), // bowler is 49
            new LegacySeasonTournamentRow(101, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), true, 0) // bowler is 50
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 200, 1, 200)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 40, null),
            new LegacyBowlerResultRow(1, 101, 2, 100m, 60, null)
        };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert
        result.SeniorOfTheYearPoints.ShouldBe(60);
    }

    [Fact(DisplayName = "Compute should exclude Woman/SuperSenior side-cut points from SeniorOfTheYearPoints but add a bonus for SuperSenior side-cut finals")]
    public void Compute_ShouldExcludeSuperSeniorAndWomanSideCuts_FromSeniorPoints_ButBonusSuperSeniorFinals()
    {
        // Arrange
        var dateOfBirth = new DateOnly(1970, 1, 1); // 56 at season end - Senior only
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(102, DateTime.Today, DateTime.Today, true, 0)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 102, 3, 200, 1, 200)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 40, null), // main cut - counts in full
            new LegacyBowlerResultRow(1, 101, 5, 0m, 25, 2), // SuperSenior side cut - excluded, but bonused
            new LegacyBowlerResultRow(1, 102, 6, 0m, 20, 3) // Woman side cut - fully excluded
        };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert - 40 (main cut) + 5 (one SuperSenior side-cut finals bonus); the 25 and 20 point rows are excluded.
        result.SeniorOfTheYearPoints.ShouldBe(45);
    }

    [Fact(DisplayName = "Compute should exclude Woman side-cut points from SuperSeniorOfTheYearPoints")]
    public void Compute_ShouldExcludeWomanSideCut_FromSuperSeniorPoints()
    {
        // Arrange
        var dateOfBirth = new DateOnly(1960, 1, 1); // 66 at season end - Senior and SuperSenior
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, true, 0)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 200, 1, 200)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 40, null),
            new LegacyBowlerResultRow(1, 101, 6, 0m, 20, 3) // Woman side cut - excluded from SuperSenior too
        };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert
        result.SuperSeniorOfTheYearPoints.ShouldBe(40);
    }

    [Fact(DisplayName = "Compute should add SeniorWithWomen tournament points to SeniorOfTheYearPoints and SuperSeniorOfTheYearPoints independent of the ordinary eligible set")]
    public void Compute_ShouldAddSeniorWithWomenPoints_ToSeniorAndSuperSeniorPoints()
    {
        // Arrange - a SeniorWithWomen tournament is not YearlyStatEligible and not a plain Senior
        // type, so it only ever contributes through this dedicated path.
        var dateOfBirth = new DateOnly(1960, 1, 1); // 66 at season end - Senior and SuperSenior
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, false, 8) // SeniorWithWomen
        };
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var results = new[] { new LegacyBowlerResultRow(1, 100, 2, 75m, 35, null) };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert
        result.SeniorOfTheYearPoints.ShouldBe(35);
        result.SuperSeniorOfTheYearPoints.ShouldBe(35);
    }

    [Fact(DisplayName = "Compute should sum WomanOfTheYearPoints from BowlerOfTheYearPoints plus Woman/Combined side-cut points plus women's-tournament results")]
    public void Compute_ShouldComputeWomanOfTheYearPoints_FromBoyPlusSideCutPlusWomensTournaments()
    {
        // Arrange
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0), // ordinary eligible
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, true, 0), // ordinary eligible, Woman side cut
            new LegacySeasonTournamentRow(102, DateTime.Today, DateTime.Today, false, 3) // Women-only tournament, not otherwise eligible
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 102, 3, 200, 1, 200)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 3, 50m, 40, null), // main cut BOY points
            new LegacyBowlerResultRow(1, 101, 5, 0m, 25, 3), // Woman side cut
            new LegacyBowlerResultRow(1, 102, 1, 200m, 100, null) // Women-only tournament result
        };
        var bowlers = new[] { new LegacyBowlerRow(1, 1, null) }; // Gender = 1 (Female)

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert - 40 (main cut BOY) + 25 (Woman side cut) + 100 (women's tournament) = 165.
        result.WomanOfTheYearPoints.ShouldBe(165);
    }

    [Fact(DisplayName = "Compute should not compute WomanOfTheYearPoints for a bowler who is not classified Woman")]
    public void Compute_ShouldNotComputeWomanOfTheYearPoints_WhenNotWoman()
    {
        // Arrange
        var qualifying = new[] { new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200) };
        var results = new[] { new LegacyBowlerResultRow(1, 100, 1, 500m, 100, null) };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, null) }; // Gender = 0 (Male)

        // Act
        var result = ComputeSingle(qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert
        result.WomanOfTheYearPoints.ShouldBe(0);
    }

    [Fact(DisplayName = "Compute should scope YouthOfTheYearPoints per-tournament by the bowler's age as of each tournament's end date, and derive IsYouth from earning any")]
    public void Compute_ShouldScopeYouthPoints_ByAgeAsOfEachTournamentEndDate()
    {
        // Arrange - the bowler turns 18 between the two tournaments; only the first (where they
        // were still under 18 as of that tournament's own end date) counts toward Youth points.
        var dateOfBirth = new DateOnly(2008, 6, 1);
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 0), // bowler is 17
            new LegacySeasonTournamentRow(101, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), true, 0) // bowler is 18
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 200, 1, 200),
            new LegacyQualifyingStatsRow(1, 101, 2, 200, 1, 200)
        };
        var results = new[]
        {
            new LegacyBowlerResultRow(1, 100, 2, 50m, 45, null),
            new LegacyBowlerResultRow(1, 101, 3, 25m, 30, null)
        };
        var bowlers = new[] { new LegacyBowlerRow(1, 0, dateOfBirth) };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, results: results, bowlers: bowlers);

        // Assert
        result.YouthOfTheYearPoints.ShouldBe(45);
        result.IsYouth.ShouldBeTrue();
    }

    [Fact(DisplayName = "Compute should scope QualifyingHighGame and MatchPlay stats across the whole season, not just eligible tournaments")]
    public void Compute_ShouldScopeQualifyingHighGameAndMatchPlayStats_AcrossWholeSeason()
    {
        // Arrange - BowlerSeasonStats has no Eligible/Total split for these fields, so (unlike
        // Tournaments/Entries) they intentionally span every tournament entered, eligible or not.
        var seasonTournaments = new[]
        {
            new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0),
            new LegacySeasonTournamentRow(101, DateTime.Today, DateTime.Today, false, 1)
        };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 500, 2, 250),
            new LegacyQualifyingStatsRow(1, 101, 2, 550, 2, 280) // ineligible tournament, still counts
        };
        var matchPlay = new[]
        {
            new LegacyMatchPlayStatsRow(1, 100, 200, 1, 200, true),
            new LegacyMatchPlayStatsRow(1, 101, 210, 1, 210, false) // ineligible tournament, still counts
        };

        // Act
        var result = ComputeSingle(seasonTournaments: seasonTournaments, qualifyingStats: qualifying, matchPlayStats: matchPlay);

        // Assert
        result.QualifyingHighGame.ShouldBe(280);
        result.MatchPlayWins.ShouldBe(1);
        result.MatchPlayLosses.ShouldBe(1);
        result.MatchPlayHighGame.ShouldBe(210);
    }

    [Fact(DisplayName = "Compute should net FieldAverage as the bowler's qualifying average minus the field's average across the eligible tournaments the bowler personally entered")]
    public void Compute_ShouldComputeFieldAverage_FromEligibleTournamentsBowlerEntered()
    {
        // Arrange - bowler 1 averages 210 across their one eligible entry; the field (bowlers 1 and
        // 2 combined) averages 200 across that same tournament, so bowler 1's FieldAverage is +10.
        var seasonTournaments = new[] { new LegacySeasonTournamentRow(100, DateTime.Today, DateTime.Today, true, 0) };
        var qualifying = new[]
        {
            new LegacyQualifyingStatsRow(1, 100, 1, 210, 1, 210),
            new LegacyQualifyingStatsRow(2, 100, 2, 190, 1, 190)
        };

        // Act
        var computed = LegacySeasonStatsCalculator.Compute(
            SeasonEndDate, NewMembershipTypeId, seasonTournaments, qualifying, [], [], [], [], [], []);
        var result = computed.Single(r => r.BowlerId == 1);

        // Assert
        result.FieldAverage.ShouldBe(10m);
    }
}