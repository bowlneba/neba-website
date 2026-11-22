using Neba.Api.Features.Tournaments.DeleteTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.DeleteTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class DeleteTournamentSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        var summary = new DeleteTournamentSummary();

        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        var summary = new DeleteTournamentSummary();

        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register all documented responses")]
    public void Constructor_ShouldRegisterAllDocumentedResponses()
    {
        var summary = new DeleteTournamentSummary();

        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(409);
    }

    [Fact(DisplayName = "Constructor should say a Series Stage cannot be deleted in the description and the 409 response")]
    public void Constructor_ShouldMentionSeriesStage_WhenDescribingTheConflict()
    {
        // Arrange & Act
        var summary = new DeleteTournamentSummary();

        // Assert
        summary.Description.ShouldContain("Stage in a Series");
        summary.Responses[409].ShouldContain("Stage in a Series");
    }
}