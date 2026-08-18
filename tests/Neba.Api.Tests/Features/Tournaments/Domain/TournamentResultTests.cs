using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.TournamentResult")]
public sealed class TournamentResultTests
{
    private static readonly BowlerId ValidBowlerId = BowlerId.New();
    private const int ValidPlace = 1;
    private const decimal ValidPrizeMoney = 100m;
    private const int ValidPoints = 10;

    [Fact(DisplayName = "Create returns a TournamentResult with a new Id")]
    public void Create_ShouldReturnTournamentResult_WithNewId()
    {
        // Arrange & Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, ValidPrizeMoney, ValidPoints);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create returns a TournamentResult with the correct BowlerId, Place, PrizeMoney, and Points")]
    public void Create_ShouldReturnTournamentResult_WithCorrectValues()
    {
        // Arrange & Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, ValidPrizeMoney, ValidPoints);

        // Assert
        result.IsError.ShouldBeFalse();

        var tournamentResult = result.Value;
        tournamentResult.BowlerId.ShouldBe(ValidBowlerId);
        tournamentResult.Place.ShouldBe(ValidPlace);
        tournamentResult.PrizeMoney.ShouldBe(ValidPrizeMoney);
        tournamentResult.Points.ShouldBe(ValidPoints);
    }

    [Fact(DisplayName = "Create returns a TournamentResult when PrizeMoney and Points are zero")]
    public void Create_ShouldReturnTournamentResult_WhenPrizeMoneyAndPointsAreZero()
    {
        // Arrange & Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, prizeMoney: 0m, points: 0);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PrizeMoney.ShouldBe(0m);
        result.Value.Points.ShouldBe(0);
    }

    [Theory(DisplayName = "Create returns TournamentResult.Place.Invalid when the place is not positive")]
    [InlineData(0, TestDisplayName = "Place of 0 should be invalid")]
    [InlineData(-1, TestDisplayName = "Place of -1 should be invalid")]
    public void Create_ShouldReturnError_WhenPlaceIsNotPositive(int place)
    {
        // Arrange & Act
        var result = TournamentResult.Create(ValidBowlerId, place, ValidPrizeMoney, ValidPoints);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentResult.Place.Invalid");
    }

    [Fact(DisplayName = "Create error metadata contains Place when the place is invalid")]
    public void Create_ShouldIncludePlaceInMetadata_WhenPlaceIsInvalid()
    {
        // Arrange
        const int place = 0;

        // Act
        var result = TournamentResult.Create(ValidBowlerId, place, ValidPrizeMoney, ValidPoints);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("Place");
        result.FirstError.Metadata["Place"].ShouldBe(place);
    }

    [Fact(DisplayName = "Create returns TournamentResult.PrizeMoney.Invalid when the prize money is negative")]
    public void Create_ShouldReturnError_WhenPrizeMoneyIsNegative()
    {
        // Arrange
        const decimal prizeMoney = -0.01m;

        // Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, prizeMoney, ValidPoints);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentResult.PrizeMoney.Invalid");
    }

    [Fact(DisplayName = "Create error metadata contains PrizeMoney when the prize money is invalid")]
    public void Create_ShouldIncludePrizeMoneyInMetadata_WhenPrizeMoneyIsInvalid()
    {
        // Arrange
        const decimal prizeMoney = -0.01m;

        // Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, prizeMoney, ValidPoints);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("PrizeMoney");
        result.FirstError.Metadata["PrizeMoney"].ShouldBe(prizeMoney);
    }

    [Fact(DisplayName = "Create returns TournamentResult.Points.Invalid when the points are negative")]
    public void Create_ShouldReturnError_WhenPointsAreNegative()
    {
        // Arrange
        const int points = -1;

        // Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, ValidPrizeMoney, points);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentResult.Points.Invalid");
    }

    [Fact(DisplayName = "Create error metadata contains Points when the points are invalid")]
    public void Create_ShouldIncludePointsInMetadata_WhenPointsAreInvalid()
    {
        // Arrange
        const int points = -1;

        // Act
        var result = TournamentResult.Create(ValidBowlerId, ValidPlace, ValidPrizeMoney, points);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("Points");
        result.FirstError.Metadata["Points"].ShouldBe(points);
    }
}
