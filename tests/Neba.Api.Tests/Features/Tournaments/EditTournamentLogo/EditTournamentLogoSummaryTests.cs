using Neba.Api.Features.Tournaments.EditTournamentLogo;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentLogo;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentLogoSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new EditTournamentLogoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new EditTournamentLogoSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new EditTournamentLogoSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(422);
    }

    [Fact(DisplayName = "Constructor should set an example request with a logo")]
    public void Constructor_ShouldSetExampleRequest()
    {
        // Arrange & Act
        var summary = new EditTournamentLogoSummary();

        // Assert
        var example = summary.ExampleRequest.ShouldBeOfType<Neba.Api.Contracts.Tournaments.EditTournamentLogo.EditTournamentLogoRequest>();
        example.Id.Length.ShouldBe(26);
        example.Logo.ShouldNotBeNull();
    }
}
