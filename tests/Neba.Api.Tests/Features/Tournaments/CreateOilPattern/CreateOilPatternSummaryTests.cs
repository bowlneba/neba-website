using Neba.Api.Features.Tournaments.CreateOilPattern;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CreateOilPattern;

[UnitTest]
[Component("Tournaments")]
public sealed class CreateOilPatternSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        var summary = new CreateOilPatternSummary();

        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        var summary = new CreateOilPatternSummary();

        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register all documented responses")]
    public void Constructor_ShouldRegisterAllDocumentedResponses()
    {
        var summary = new CreateOilPatternSummary();

        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}