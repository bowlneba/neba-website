using Ardalis.SmartEnum;

using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.TournamentRound")]
public sealed class TournamentRoundTests
{
    [Fact(DisplayName = "Should have 4 tournament rounds")]
    public void TournamentRound_ShouldHave4Rounds()
    {
        TournamentRound.List.Count.ShouldBe(4);
    }

    [Fact(DisplayName = "Round bit values should be non-overlapping powers of two")]
    public void TournamentRound_RoundValues_ShouldBeCorrectPowersOfTwo()
    {
        TournamentRound.Qualifying.Value.ShouldBe(1);
        TournamentRound.Cashers.Value.ShouldBe(2);
        TournamentRound.MatchPlay.Value.ShouldBe(4);
        TournamentRound.StepLadder.Value.ShouldBe(8);
    }

    [Theory(DisplayName = "Tournament round properties should be correct")]
    [InlineData("Qualifying", 1, TestDisplayName = "Qualifying should have value 1")]
    [InlineData("Cashers", 2, TestDisplayName = "Cashers should have value 2")]
    [InlineData("Match Play", 4, TestDisplayName = "Match Play should have value 4")]
    [InlineData("Step Ladder", 8, TestDisplayName = "Step Ladder should have value 8")]
    public void TournamentRound_ShouldHaveCorrectProperties(string expectedName, int expectedValue)
    {
        var round = SmartFlagEnum<TournamentRound>.FromValue(expectedValue).First();

        round.Name.ShouldBe(expectedName);
        round.Value.ShouldBe(expectedValue);
    }

    [Theory(DisplayName = "Combined round values should resolve to correct constituent flags")]
    [InlineData(3, new[] { "Qualifying", "Cashers" }, TestDisplayName = "Qualifying | Cashers resolves to both flags")]
    [InlineData(5, new[] { "Qualifying", "Match Play" }, TestDisplayName = "Qualifying | MatchPlay resolves to both flags")]
    [InlineData(9, new[] { "Qualifying", "Step Ladder" }, TestDisplayName = "Qualifying | StepLadder resolves to both flags")]
    [InlineData(6, new[] { "Cashers", "Match Play" }, TestDisplayName = "Cashers | MatchPlay resolves to both flags")]
    [InlineData(15, new[] { "Qualifying", "Cashers", "Match Play", "Step Ladder" }, TestDisplayName = "All four flags combined resolves to all four")]
    public void TournamentRound_ShouldResolveCorrectFlags_WhenValueIsCombined(int combinedValue, string[] expectedNames)
    {
        ArgumentNullException.ThrowIfNull(expectedNames);

        var flags = SmartFlagEnum<TournamentRound>.FromValue(combinedValue).ToList();

        flags.Count.ShouldBe(expectedNames.Length);
        foreach (var name in expectedNames)
        {
            flags.ShouldContain(f => f.Name == name);
        }
    }
}