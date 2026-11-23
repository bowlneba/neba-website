using Neba.Api.Contracts.Series.MakeStageOfficial;
using Neba.Api.Features.Series.MakeStageOfficial;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.MakeStageOfficial;

[UnitTest]
[Component("Series")]
public sealed class MakeStageOfficialSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new MakeStageOfficialSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.MakeStageOfficial");
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404, 409 and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new MakeStageOfficialSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 409, 422]);
    }

    [Fact(DisplayName = "Constructor should give the summary an example request")]
    public void Constructor_ShouldGiveAnExampleRequest()
    {
        // Arrange & Act
        var summary = new MakeStageOfficialSummary();

        // Assert
        var example = summary.ExampleRequest.ShouldBeOfType<MakeStageOfficialRequest>();
        example.Official.Version.ShouldBe(3);
    }
}
