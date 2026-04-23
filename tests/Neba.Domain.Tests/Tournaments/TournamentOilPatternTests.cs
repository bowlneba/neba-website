using Neba.Domain.Tournaments;
using Neba.TestFactory.Tournaments;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.TournamentOilPattern")]
public sealed class TournamentOilPatternTests
{
    [Fact(DisplayName = "AddTournamentRound returns Updated when round is new")]
    public void AddTournamentRound_ShouldReturnUpdated_WhenRoundIsNew()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create();

        // Act
        var result = oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddTournamentRound adds round to TournamentRounds when round is new")]
    public void AddTournamentRound_ShouldAddRoundToCollection_WhenRoundIsNew()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create();

        // Act
        oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        oilPattern.TournamentRounds.ShouldContain(TournamentRound.Qualifying);
    }

    [Fact(DisplayName = "AddTournamentRound adds multiple distinct rounds to TournamentRounds")]
    public void AddTournamentRound_ShouldAddMultipleDistinctRounds()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create();

        // Act
        oilPattern.AddTournamentRound(TournamentRound.Qualifying);
        oilPattern.AddTournamentRound(TournamentRound.MatchPlay);

        // Assert
        oilPattern.TournamentRounds.Count.ShouldBe(2);
        oilPattern.TournamentRounds.ShouldContain(TournamentRound.Qualifying);
        oilPattern.TournamentRounds.ShouldContain(TournamentRound.MatchPlay);
    }

    [Fact(DisplayName = "AddTournamentRound returns Tournaments.TournamentOilPattern.TournamentRoundAlreadyAssociated when round is already associated")]
    public void AddTournamentRound_ShouldReturnError_WhenRoundAlreadyAssociated()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create(tournamentRounds: [TournamentRound.Qualifying]);

        // Act
        var result = oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournaments.TournamentOilPattern.TournamentRoundAlreadyAssociated");
    }

    [Fact(DisplayName = "AddTournamentRound error metadata contains TournamentRoundName when round is already associated")]
    public void AddTournamentRound_ShouldIncludeTournamentRoundNameInMetadata_WhenRoundAlreadyAssociated()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create(tournamentRounds: [TournamentRound.Qualifying]);

        // Act
        var result = oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        result.FirstError.Metadata.ShouldContainKey("TournamentRoundName");
        result.FirstError.Metadata["TournamentRoundName"].ShouldBe(TournamentRound.Qualifying.Name);
    }

    [Fact(DisplayName = "AddTournamentRound does not add duplicate round to collection when round is already associated")]
    public void AddTournamentRound_ShouldNotAddDuplicateRound_WhenRoundAlreadyAssociated()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create(tournamentRounds: [TournamentRound.Qualifying]);

        // Act
        oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        oilPattern.TournamentRounds.Count.ShouldBe(1);
    }

#nullable disable
    [Fact(DisplayName = "AddTournamentRound throws ArgumentNullException when tournamentRound is null")]
    public void AddTournamentRound_ShouldThrow_WhenTournamentRoundIsNull()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create();

        // Act
        Action act = () => oilPattern.AddTournamentRound(null);

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }
#nullable enable
}
