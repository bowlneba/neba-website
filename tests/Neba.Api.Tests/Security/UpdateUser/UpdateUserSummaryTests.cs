using Neba.Api.Security.UpdateUser;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.UpdateUser;

[UnitTest]
[Component("Security")]
public sealed class UpdateUserSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary, description and 204, 400, 401, 403, 404, 409 and 422 responses")]
    public void Constructor_ShouldSetSummaryDescriptionAndResponses()
    {
        // Arrange & Act
        var summary = new UpdateUserSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }
}
