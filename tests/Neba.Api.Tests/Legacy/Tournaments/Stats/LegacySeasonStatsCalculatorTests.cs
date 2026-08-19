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

    [Theory(DisplayName = "Compute should classify Senior/SuperSenior/Youth by age as of the season end date")]
    [InlineData(1976, 8, 19, true, false, false, TestDisplayName = "Age 50 - Senior only")]
    [InlineData(1966, 8, 19, true, true, false, TestDisplayName = "Age 60 - Senior and SuperSenior")]
    [InlineData(2010, 1, 1, false, false, true, TestDisplayName = "Age 16 - Youth")]
    public void Compute_ShouldClassifyByAge_WhenDateOfBirthProvided(
        int year, int month, int day, bool expectedSenior, bool expectedSuperSenior, bool expectedYouth)
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
        result.IsYouth.ShouldBe(expectedYouth);
    }

    [Fact(DisplayName = "Compute should not classify Senior/SuperSenior/Youth when DateOfBirth is null")]
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
        result.IsYouth.ShouldBeFalse();
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
            new LegacyCupResultRow(1, 100m, new DateOnly(SeasonEndDate.Year, 6, 1)),
            new LegacyCupResultRow(1, 250m, new DateOnly(SeasonEndDate.Year - 1, 6, 1))
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
}
