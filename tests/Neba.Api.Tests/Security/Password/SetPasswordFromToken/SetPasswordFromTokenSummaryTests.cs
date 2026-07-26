using Neba.Api.Security.Password.SetPasswordFromToken;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Password.SetPasswordFromToken;

[UnitTest]
[Component("Security")]
public sealed class SetPasswordFromTokenSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new SetPasswordFromTokenSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new SetPasswordFromTokenSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204 and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new SetPasswordFromTokenSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(422);
    }
}