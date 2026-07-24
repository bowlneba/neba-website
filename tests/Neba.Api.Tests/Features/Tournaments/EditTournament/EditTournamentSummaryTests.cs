using Neba.Api.Features.Tournaments.EditTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.EditTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new EditTournamentSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new EditTournamentSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404, 409, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new EditTournamentSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}
