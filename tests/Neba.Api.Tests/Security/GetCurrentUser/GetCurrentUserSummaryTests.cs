using Neba.Api.Security.GetCurrentUser;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.GetCurrentUser;

[UnitTest]
[Component("Security")]
public sealed class GetCurrentUserSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new GetCurrentUserSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new GetCurrentUserSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 401, and 404 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new GetCurrentUserSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(404);
    }
}