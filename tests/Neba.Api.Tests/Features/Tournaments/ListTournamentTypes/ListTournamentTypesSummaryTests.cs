using Neba.Api.Features.Tournaments.ListTournamentTypes;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentTypes;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentTypesSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        var summary = new ListTournamentTypesSummary();

        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        var summary = new ListTournamentTypesSummary();

        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register a 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        var summary = new ListTournamentTypesSummary();

        summary.Responses.ShouldContainKey(200);
    }
}