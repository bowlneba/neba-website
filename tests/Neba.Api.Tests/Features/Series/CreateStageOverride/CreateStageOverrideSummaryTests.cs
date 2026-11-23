using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.CreateStageOverride;

[UnitTest]
[Component("Series")]
public sealed class CreateStageOverrideSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new CreateStageOverrideSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.ManageAdvancement");
    }

    [Fact(DisplayName = "Constructor should register 201, 400, 401, 403, 404, 409 and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new CreateStageOverrideSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([201, 400, 401, 403, 404, 409, 422]);
    }

    [Fact(DisplayName = "Constructor should give the request an Add example")]
    public void Constructor_ShouldGiveTheRequestAnExample()
    {
        // Arrange & Act
        var summary = new CreateStageOverrideSummary();

        // Assert
        var example = summary.RequestExamples.Single().Value.ShouldBeOfType<CreateStageOverrideRequest>();
        example.Id.Length.ShouldBe(26);
        example.StageId.Length.ShouldBe(26);
        example.StageOverride.Kind.ShouldBe(OverrideKindValues.Add);
        example.StageOverride.BowlerId.Length.ShouldBe(26);
        example.StageOverride.Reason.ShouldNotBeNullOrWhiteSpace();
        example.StageOverride.Version.ShouldBe(3);
    }

    [Fact(DisplayName = "Constructor should give the 201 response an example with an override id and the Version after the write")]
    public void Constructor_ShouldGiveTheCreatedResponseAnExample()
    {
        // Arrange & Act
        var summary = new CreateStageOverrideSummary();

        // Assert
        var example = SummaryExamples.For(summary, 201).ShouldBeOfType<CreatedStageOverrideResponse>();
        example.OverrideId.Length.ShouldBe(26);
        example.Version.ShouldBe(4);
    }
}