using Neba.Api.Security.Password.ForgotPassword;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Password.ForgotPassword;

[UnitTest]
[Component("Security")]
public sealed class ForgotPasswordSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary and Description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ForgotPasswordSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 400, and 429 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ForgotPasswordSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(429);
    }
}
