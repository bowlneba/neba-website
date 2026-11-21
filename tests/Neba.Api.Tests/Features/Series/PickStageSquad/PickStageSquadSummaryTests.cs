using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Features.Series.PickStageSquad;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.PickStageSquad;

[UnitTest]
[Component("Series")]
public sealed class PickStageSquadSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new PickStageSquadSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.EditSeries");
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404, 409 and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new PickStageSquadSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 409, 422]);
    }

    [Fact(DisplayName = "Constructor should give the request an example with a squad and a Version")]
    public void Constructor_ShouldGiveTheRequestAnExample()
    {
        // Arrange & Act
        var summary = new PickStageSquadSummary();

        // Assert
        var example = summary.RequestExamples.Single().Value.ShouldBeOfType<PickStageSquadRequest>();
        example.Id.Length.ShouldBe(26);
        example.StageId.Length.ShouldBe(26);
        example.Squad.SquadId.Length.ShouldBe(26);
        example.Squad.Version.ShouldBe(3);
    }
}