using Neba.Api.Security.ListRoles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.ListRoles;

[UnitTest]
[Component("Security")]
public sealed class ListRolesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the summary, description and 200, 401 and 403 responses")]
    public void Constructor_ShouldSetSummaryDescriptionAndResponses()
    {
        // Arrange & Act
        var summary = new ListRolesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
    }
}