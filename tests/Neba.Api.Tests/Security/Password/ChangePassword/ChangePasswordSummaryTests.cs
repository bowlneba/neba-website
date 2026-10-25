using Neba.Api.Security.Password.ChangePassword;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Password.ChangePassword;

[UnitTest]
[Component("Security")]
public sealed class ChangePasswordSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary and Description")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new ChangePasswordSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, 409, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new ChangePasswordSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}