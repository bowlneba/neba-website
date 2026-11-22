using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class DefendingTeamMappingTests
{
    [Fact(DisplayName = "ToDto should carry the consecutive titles of the team")]
    public void ToDto_ShouldCarryConsecutiveTitles()
    {
        // Arrange
        var team = new DefendingTeam(
            TournamentId.New(),
            "Joe's Pizza Open",
            "2025 Season",
            SourceTruncated: false,
            Undefended: false,
            [new TournamentWinner(BowlerId.New(), NameFactory.Create(firstName: "Dave", lastName: "Smith"))],
            ConsecutiveTitles: 3);

        // Act
        var dto = team.ToDto();

        // Assert
        dto.ConsecutiveTitles.ShouldBe(3);
        dto.TournamentName.ShouldBe("Joe's Pizza Open");
    }

    [Fact(DisplayName = "ToResponse should carry the consecutive titles of the team")]
    public void ToResponse_ShouldCarryConsecutiveTitles()
    {
        // Arrange
        var dto = DefendingTeamDtoFactory.Create(consecutiveTitles: 2);

        // Act
        var response = dto.ToResponse();

        // Assert
        response.ConsecutiveTitles.ShouldBe(2);
        response.TournamentName.ShouldBe(dto.TournamentName);
    }
}