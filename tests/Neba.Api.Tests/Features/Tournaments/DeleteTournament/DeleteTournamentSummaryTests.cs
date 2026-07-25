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
}