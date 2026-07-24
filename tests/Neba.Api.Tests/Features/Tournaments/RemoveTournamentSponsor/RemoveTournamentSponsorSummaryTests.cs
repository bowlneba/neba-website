using Neba.Api.Features.Tournaments.RemoveTournamentSponsor;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.RemoveTournamentSponsor;

[UnitTest]
[Component("Tournaments")]
public sealed class RemoveTournamentSponsorSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        var summary = new RemoveTournamentSponsorSummary();

        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        var summary = new RemoveTournamentSponsorSummary();

        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register all documented responses")]
    public void Constructor_ShouldRegisterAllDocumentedResponses()
    {
        var summary = new RemoveTournamentSponsorSummary();

        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(409);
    }
}