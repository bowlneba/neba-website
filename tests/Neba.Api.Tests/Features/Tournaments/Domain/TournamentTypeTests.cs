using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.TournamentType")]
public sealed class TournamentTypeTests
{
    [Fact(DisplayName = "Should have 18 tournament types")]
    public void TournamentType_ShouldHave17Types()
    {
        // Act
        var count = TournamentType.List.Count;

        // Assert
        count.ShouldBe(18);
    }

    [Theory(DisplayName = "Tournament type properties should be correct")]
    [InlineData("Singles", 100, 1, true, TestDisplayName = "Singles should have value 100, team size 1, active")]
    [InlineData("Doubles", 200, 2, true, TestDisplayName = "Doubles should have value 200, team size 2, active")]
    [InlineData("Trios", 300, 3, true, TestDisplayName = "Trios should have value 300, team size 3, active")]
    [InlineData("Baker", 500, 5, true, TestDisplayName = "Baker should have value 500, team size 5, active")]
    [InlineData("Non-Champions", 101, 1, true, TestDisplayName = "Non-Champions should have value 101, team size 1, active")]
    [InlineData("Tournament of Champions", 102, 1, true, TestDisplayName = "Tournament of Champions should have value 102, team size 1, active")]
    [InlineData("Invitational", 103, 1, true, TestDisplayName = "Invitational should have value 103, team size 1, active")]
    [InlineData("Masters", 104, 1, true, TestDisplayName = "Masters should have value 104, team size 1, active")]
    [InlineData("High Roller", 105, 1, false, TestDisplayName = "High Roller should have value 105, team size 1, inactive")]
    [InlineData("Senior", 106, 1, true, TestDisplayName = "Senior should have value 106, team size 1, active")]
    [InlineData("Women", 107, 1, true, TestDisplayName = "Women should have value 107, team size 1, active")]
    [InlineData("Over 40", 108, 1, false, TestDisplayName = "Over 40 should have value 108, team size 1, inactive")]
    [InlineData("40 - 49", 109, 1, false, TestDisplayName = "40 - 49 should have value 109, team size 1, inactive")]
    [InlineData("Youth", 110, 1, true, TestDisplayName = "Youth should have value 110, team size 1, active")]
    [InlineData("Eliminator", 111, 1, false, TestDisplayName = "Eliminator should have value 111, team size 1, inactive")]
    [InlineData("Senior / Women", 112, 1, true, TestDisplayName = "Senior / Women should have value 112, team size 1, active")]
    [InlineData("Over/Under 50 Doubles", 201, 2, true, TestDisplayName = "Over/Under 50 Doubles should have value 201, team size 2, active")]
    [InlineData("Over/Under 40 Doubles", 202, 2, false, TestDisplayName = "Over/Under 40 Doubles should have value 202, team size 2, inactive")]
    public void TournamentType_ShouldHaveCorrectProperties(string expectedName, int expectedValue, int expectedTeamSize, bool expectedActiveFormat)
    {
        // Act
        var type = TournamentType.FromValue(expectedValue);

        // Assert
        type.Name.ShouldBe(expectedName);
        type.Value.ShouldBe(expectedValue);
        type.TeamSize.ShouldBe(expectedTeamSize);
        type.ActiveFormat.ShouldBe(expectedActiveFormat);
    }
}