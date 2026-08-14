using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.Squad")]
public sealed class SquadTests
{
    [Fact(DisplayName = "Create returns Squad with correct BowlingDateTime")]
    public void Create_ShouldReturnSquad_WithCorrectBowlingDateTime()
    {
        // Arrange
        var bowlingDateTime = new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);

        // Act
        var result = Squad.Create(bowlingDateTime);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlingDateTime.ShouldBe(bowlingDateTime);
    }

    [Fact(DisplayName = "Create returns Squad with a new Id")]
    public void Create_ShouldReturnSquad_WithNewId()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create returns Squad with null MaxEntries when not specified")]
    public void Create_ShouldReturnSquad_WithNullMaxEntries_WhenNotSpecified()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.MaxEntries.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns Squad with correct MaxEntries when specified")]
    public void Create_ShouldReturnSquad_WithCorrectMaxEntries_WhenSpecified()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow, maxEntries: 32);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.MaxEntries.ShouldBe(32);
    }

    [Fact(DisplayName = "Create returns Squad with null LegacyId when not specified")]
    public void Create_ShouldReturnSquad_WithNullLegacyId_WhenNotSpecified()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LegacyId.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns Squad with correct LegacyId when specified")]
    public void Create_ShouldReturnSquad_WithCorrectLegacyId_WhenSpecified()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow, legacyId: 4521);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LegacyId.ShouldBe(4521);
    }

    [Fact(DisplayName = "Create returns Squad.MaxEntries.Invalid when maxEntries is zero")]
    public void Create_ShouldReturnError_WhenMaxEntriesIsZero()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow, maxEntries: 0);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Squad.MaxEntries.Invalid");
    }

    [Fact(DisplayName = "Create returns Squad.MaxEntries.Invalid when maxEntries is negative")]
    public void Create_ShouldReturnError_WhenMaxEntriesIsNegative()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow, maxEntries: -1);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Squad.MaxEntries.Invalid");
    }

    [Fact(DisplayName = "Create error metadata contains MaxEntries when maxEntries is invalid")]
    public void Create_ShouldIncludeMaxEntriesInMetadata_WhenMaxEntriesIsInvalid()
    {
        // Arrange & Act
        var result = Squad.Create(DateTimeOffset.UtcNow, maxEntries: -1);

        // Assert
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("MaxEntries");
        result.FirstError.Metadata["MaxEntries"].ShouldBe(-1);
    }
}
