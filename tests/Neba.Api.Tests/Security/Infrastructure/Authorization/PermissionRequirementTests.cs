using Neba.Api.Security.Infrastructure.Authorization;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Infrastructure.Authorization;

[UnitTest]
[Component("Security")]
public sealed class PermissionRequirementTests
{
    [Fact(DisplayName = "Permission should return the value provided to the constructor")]
    public void Permission_ShouldReturnConstructorValue()
    {
        // Arrange
        var requirement = new PermissionRequirement("Read");

        // Act
        var permission = requirement.Permission;

        // Assert
        permission.ShouldBe("Read");
    }
}
