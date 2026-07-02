using Neba.Api.Security.Login;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Login;

[UnitTest]
[Component("Security")]
public sealed class LoginSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new LoginSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new LoginSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 401, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new LoginSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(422);
    }
}