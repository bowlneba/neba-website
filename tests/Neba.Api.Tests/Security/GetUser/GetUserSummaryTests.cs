using Neba.Api.Security.GetUser;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.GetUser;

[UnitTest]
[Component("Security")]
public sealed class GetUserSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary, description and 200, 400, 401, 403 and 404 responses")]
    public void Constructor_ShouldSetSummaryDescriptionAndResponses()
    {
        // Arrange & Act
        var summary = new GetUserSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
    }
}