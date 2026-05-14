using Neba.Domain.Tournaments;
using Neba.Api.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed class TournamentRoundValueConverterTests
{
    [Fact(DisplayName = "Should convert rounds to combined bit flag value")]
    public void ConvertToProvider_ShouldReturnCombinedFlagValue_WhenMultipleRoundsProvided()
    {
        // Arrange
        IReadOnlyCollection<TournamentRound> rounds =
        [
            TournamentRound.Qualifying,
            TournamentRound.StepLadder,
        ];
        var converter = new TournamentRoundValueConverter();

        // Act
        var result = (int)converter.ConvertToProvider(rounds)!;

        // Assert
        result.ShouldBe(
            TournamentRound.Qualifying.Value |
            TournamentRound.StepLadder.Value);
    }

    [Fact(DisplayName = "Should convert empty rounds to zero")]
    public void ConvertToProvider_ShouldReturnZero_WhenRoundsAreEmpty()
    {
        // Arrange
        IReadOnlyCollection<TournamentRound> rounds = [];
        var converter = new TournamentRoundValueConverter();

        // Act
        var result = (int)converter.ConvertToProvider(rounds)!;

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "Should convert bit flag value to matching rounds")]
    public void ConvertFromProvider_ShouldReturnMatchingRounds_WhenValueContainsMultipleFlags()
    {
        // Arrange
        var providerValue =
            TournamentRound.Cashers.Value |
            TournamentRound.MatchPlay.Value;
        var converter = new TournamentRoundValueConverter();

        // Act
        var result = (IReadOnlyCollection<TournamentRound>)converter.ConvertFromProvider(providerValue)!;

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(TournamentRound.Cashers);
        result.ShouldContain(TournamentRound.MatchPlay);
    }

    [Fact(DisplayName = "Should preserve rounds when converting to and from provider")]
    public void RoundTrip_ShouldPreserveRounds_WhenConvertingToAndFromProvider()
    {
        // Arrange
        IReadOnlyCollection<TournamentRound> originalRounds =
        [
            TournamentRound.Qualifying,
            TournamentRound.Cashers,
            TournamentRound.MatchPlay,
            TournamentRound.StepLadder,
        ];
        var converter = new TournamentRoundValueConverter();

        // Act
        var providerValue = (int)converter.ConvertToProvider(originalRounds)!;
        var roundTripRounds = (IReadOnlyCollection<TournamentRound>)converter.ConvertFromProvider(providerValue)!;

        // Assert
        roundTripRounds.Count.ShouldBe(originalRounds.Count);
        foreach (var round in originalRounds)
        {
            roundTripRounds.ShouldContain(round);
        }
    }
}