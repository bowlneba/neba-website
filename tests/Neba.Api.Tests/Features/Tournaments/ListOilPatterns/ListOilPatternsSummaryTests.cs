using Neba.Api.Features.Tournaments.ListOilPatterns;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListOilPatterns;

[UnitTest]
[Component("Tournaments")]
public sealed class ListOilPatternsSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        var summary = new ListOilPatternsSummary();

        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        var summary = new ListOilPatternsSummary();

        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register a 200 response")]
    public void Constructor_ShouldRegisterOkResponse()
    {
        var summary = new ListOilPatternsSummary();

        summary.Responses.ShouldContainKey(200);
    }
}