using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class SeasonTournamentDtoMappingTests
{
    [Fact(DisplayName = "ToResponse should turn the next tournament id into its string")]
    public void ToResponse_ShouldMapNextTournamentId()
    {
        // Arrange
        var nextId = TournamentId.New();
        var dto = SeasonTournamentDtoFactory.Create(nextTournamentId: nextId);

        // Act
        var response = dto.ToResponse();

        // Assert
        response.NextTournamentId.ShouldBe(nextId.ToString());
    }

    [Fact(DisplayName = "ToResponse should leave the next tournament id null when no tournament follows")]
    public void ToResponse_ShouldLeaveNextTournamentIdNull_WhenNoneFollows()
    {
        // Arrange
        var dto = SeasonTournamentDtoFactory.Create();

        // Act
        var response = dto.ToResponse();

        // Assert
        response.NextTournamentId.ShouldBeNull();
    }
}
