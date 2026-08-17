using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.SquadScore")]
public sealed class SquadScoreTests
{
    private static readonly SquadId ValidSquadId = SquadId.New();
    private static readonly BowlerId ValidBowlerId = BowlerId.New();
    private const short ValidGameNumber = 1;
    private const int ValidScore = 200;

    [Fact(DisplayName = "Create returns a SquadScore with a new Id")]
    public void Create_ShouldReturnSquadScore_WithNewId()
    {
        // Arrange & Act
        var result = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, ValidScore);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create returns a SquadScore with the correct SquadId, BowlerId, GameNumber, and Value")]
    public void Create_ShouldReturnSquadScore_WithCorrectValues()
    {
        // Arrange & Act
        var result = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, ValidScore);

        // Assert
        result.IsError.ShouldBeFalse();

        var squadScore = result.Value;
        squadScore.SquadId.ShouldBe(ValidSquadId);
        squadScore.BowlerId.ShouldBe(ValidBowlerId);
        squadScore.GameNumber.ShouldBe(ValidGameNumber);
        squadScore.Value.ShouldBe(ValidScore);
    }

    [Theory(DisplayName = "Create returns a SquadScore when the score is at a valid boundary")]
    [InlineData(0, TestDisplayName = "Score of 0 should be valid")]
    [InlineData(300, TestDisplayName = "Score of 300 should be valid")]
    public void Create_ShouldReturnSquadScore_WhenScoreIsAtValidBoundary(int score)
    {
        // Arrange & Act
        var result = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, score);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(score);
    }

    [Theory(DisplayName = "Create returns SquadScore.Value.Invalid when the score is out of range")]
    [InlineData(-1, TestDisplayName = "Score of -1 should be invalid")]
    [InlineData(301, TestDisplayName = "Score of 301 should be invalid")]
    public void Create_ShouldReturnError_WhenScoreIsOutOfRange(int score)
    {
        // Arrange & Act
        var result = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, score);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SquadScore.Value.Invalid");
    }

    [Fact(DisplayName = "Create error metadata contains Value when the score is invalid")]
    public void Create_ShouldIncludeValueInMetadata_WhenScoreIsInvalid()
    {
        // Arrange
        const int score = -1;

        // Act
        var result = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, score);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("Value");
        result.FirstError.Metadata["Value"].ShouldBe(score);
    }

    [Fact(DisplayName = "UpdateValue sets the Value when the score is valid")]
    public void UpdateValue_ShouldSetValue_WhenScoreIsValid()
    {
        // Arrange
        var squadScore = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, ValidScore).Value;
        const int updatedScore = 250;

        // Act
        var result = squadScore.UpdateValue(updatedScore);

        // Assert
        result.IsError.ShouldBeFalse();
        squadScore.Value.ShouldBe(updatedScore);
    }

    [Theory(DisplayName = "UpdateValue returns SquadScore.Value.Invalid when the score is out of range")]
    [InlineData(-1, TestDisplayName = "Score of -1 should be invalid")]
    [InlineData(301, TestDisplayName = "Score of 301 should be invalid")]
    public void UpdateValue_ShouldReturnError_WhenScoreIsOutOfRange(int score)
    {
        // Arrange
        var squadScore = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, ValidScore).Value;

        // Act
        var result = squadScore.UpdateValue(score);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SquadScore.Value.Invalid");
    }

    [Fact(DisplayName = "UpdateValue does not change Value when the score is invalid")]
    public void UpdateValue_ShouldNotChangeValue_WhenScoreIsInvalid()
    {
        // Arrange
        var squadScore = SquadScore.Create(ValidSquadId, ValidBowlerId, ValidGameNumber, ValidScore).Value;

        // Act
        squadScore.UpdateValue(-1);

        // Assert
        squadScore.Value.ShouldBe(ValidScore);
    }
}