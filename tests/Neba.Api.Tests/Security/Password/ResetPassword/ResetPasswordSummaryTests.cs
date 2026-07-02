using Neba.Api.Security.Password.ResetPassword;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Password.ResetPassword;

[UnitTest]
[Component("Security")]
public sealed class ResetPasswordSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new ResetPasswordSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new ResetPasswordSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 401, 403, 404, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ResetPasswordSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(422);
    }
}