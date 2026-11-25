using Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentSponsorAmount;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentSponsorAmountSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange

        // Act
        var summary = new EditTournamentSponsorAmountSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange

        // Act
        var summary = new EditTournamentSponsorAmountSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register all documented responses")]
    public void Constructor_ShouldRegisterAllDocumentedResponses()
    {
        // Arrange

        // Act
        var summary = new EditTournamentSponsorAmountSummary();

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