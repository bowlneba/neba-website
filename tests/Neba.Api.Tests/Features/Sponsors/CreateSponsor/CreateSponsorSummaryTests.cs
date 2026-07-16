using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.CreateSponsor;

[UnitTest]
[Component("Sponsors")]
public sealed class CreateSponsorSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new CreateSponsorSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new CreateSponsorSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 201, 401, 403, 409, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new CreateSponsorSummary();

        // Assert
        summary.Responses.ShouldContainKey(201);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}
