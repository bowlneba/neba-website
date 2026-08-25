using Neba.Api.Features.Stats.Domain;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

// TopTiedBy has no I/O - every case below is a plain constructed-input/expected-output assertion,
// same shape as TournamentPlaceCalculatorTests.
[UnitTest]
[Component("Legacy")]
public sealed class BowlerSeasonStatsRankingTests
{
    [Fact(DisplayName = "TopTiedBy should return an empty collection when candidates is empty")]
    public void TopTiedBy_ShouldReturnEmpty_WhenCandidatesIsEmpty()
    {
        // Arrange
        var candidates = Array.Empty<BowlerSeasonStats>();

        // Act
        var result = BowlerSeasonStatsRanking.TopTiedBy(candidates, s => s.BowlerOfTheYearPoints);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "TopTiedBy should return the single candidate when there is only one")]
    public void TopTiedBy_ShouldReturnSingleCandidate_WhenOnlyOneExists()
    {
        // Arrange
        var only = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 200);

        // Act
        var result = BowlerSeasonStatsRanking.TopTiedBy([only], s => s.BowlerOfTheYearPoints);

        // Assert
        result.ShouldHaveSingleItem().ShouldBe(only);
    }

    [Fact(DisplayName = "TopTiedBy should return only the single highest candidate when there is no tie")]
    public void TopTiedBy_ShouldReturnSingleWinner_WhenNoTie()
    {
        // Arrange
        var leader = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 500);
        var trailer = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 100);

        // Act
        var result = BowlerSeasonStatsRanking.TopTiedBy([leader, trailer], s => s.BowlerOfTheYearPoints);

        // Assert
        result.ShouldHaveSingleItem().ShouldBe(leader);
    }

    [Fact(DisplayName = "TopTiedBy should return every candidate tied for the maximum value")]
    public void TopTiedBy_ShouldReturnAllTiedCandidates_WhenMultipleShareMaximum()
    {
        // Arrange
        var winner1 = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 500);
        var winner2 = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 500);
        var trailer = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 100);

        // Act
        var result = BowlerSeasonStatsRanking.TopTiedBy([winner1, winner2, trailer], s => s.BowlerOfTheYearPoints);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(winner1);
        result.ShouldContain(winner2);
    }

    [Fact(DisplayName = "TopTiedBy should rank by the selector, not by insertion order")]
    public void TopTiedBy_ShouldRankBySelector_RegardlessOfInsertionOrder()
    {
        // Arrange - the highest-points candidate is listed first, to prove the result isn't a
        // side effect of enumeration order.
        var leader = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 900);
        var trailer1 = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 300);
        var trailer2 = BowlerSeasonStatsFactory.Create(bowlerOfTheYearPoints: 600);

        // Act
        var result = BowlerSeasonStatsRanking.TopTiedBy([leader, trailer1, trailer2], s => s.BowlerOfTheYearPoints);

        // Assert
        result.ShouldHaveSingleItem().ShouldBe(leader);
    }
}
