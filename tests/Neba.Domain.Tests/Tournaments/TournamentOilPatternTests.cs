using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.TournamentOilPattern")]
public sealed class TournamentOilPatternTests
{
    [Fact(DisplayName = "Create returns TournamentOilPattern with correct OilPatternId")]
    public void Create_ShouldReturnOilPattern_WithCorrectOilPatternId()
    {
        var id = OilPatternId.New();

        var result = TournamentOilPattern.Create(id, [TournamentRound.Qualifying]);

        result.IsError.ShouldBeFalse();
        result.Value.OilPatternId.ShouldBe(id);
    }

    [Fact(DisplayName = "Create returns TournamentOilPattern with all specified rounds")]
    public void Create_ShouldReturnOilPattern_WithSpecifiedRounds()
    {
        var result = TournamentOilPattern.Create(OilPatternId.New(), [TournamentRound.Qualifying, TournamentRound.MatchPlay]);

        result.IsError.ShouldBeFalse();
        result.Value.TournamentRounds.Count.ShouldBe(2);
        result.Value.TournamentRounds.ShouldContain(TournamentRound.Qualifying);
        result.Value.TournamentRounds.ShouldContain(TournamentRound.MatchPlay);
    }

    [Fact(DisplayName = "Create returns TournamentOilPattern.NoRoundsSpecified when rounds collection is empty")]
    public void Create_ShouldReturnError_WhenRoundsIsEmpty()
    {
        var result = TournamentOilPattern.Create(OilPatternId.New(), []);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentOilPattern.NoRoundsSpecified");
    }

    [Fact(DisplayName = "Create throws ArgumentException when duplicate rounds are provided")]
    public void Create_ShouldThrow_WhenDuplicateRoundsProvided()
    {
        Action act = () => TournamentOilPattern.Create(OilPatternId.New(), [TournamentRound.Qualifying, TournamentRound.Qualifying]);

        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("tournamentRounds");
    }

#nullable disable
    [Fact(DisplayName = "Create throws ArgumentNullException when tournamentRounds is null")]
    public void Create_ShouldThrow_WhenTournamentRoundsIsNull()
    {
        Action act = () => TournamentOilPattern.Create(OilPatternId.New(), null);

        act.ShouldThrow<ArgumentNullException>();
    }
#nullable enable

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

    [Fact(DisplayName = "AddTournamentRound returns TournamentOilPattern.RoundAlreadyAssociated when round is already associated")]
    public void AddTournamentRound_ShouldReturnError_WhenRoundAlreadyAssociated()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create(tournamentRounds: [TournamentRound.Qualifying]);

        // Act
        var result = oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentOilPattern.RoundAlreadyAssociated");
    }

    [Fact(DisplayName = "AddTournamentRound error metadata contains TournamentRoundName when round is already associated")]
    public void AddTournamentRound_ShouldIncludeTournamentRoundNameInMetadata_WhenRoundAlreadyAssociated()
    {
        // Arrange
        var oilPattern = TournamentOilPatternFactory.Create(tournamentRounds: [TournamentRound.Qualifying]);

        // Act
        var result = oilPattern.AddTournamentRound(TournamentRound.Qualifying);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
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