using Neba.Api.Features.Tournaments.UploadTournamentLogo;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.UploadTournamentLogo;

[UnitTest]
[Component("Tournaments")]
public sealed class UploadTournamentLogoSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new UploadTournamentLogoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new UploadTournamentLogoSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, and 403 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new UploadTournamentLogoSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
    }
}
