using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Security;

[UnitTest]
[Component("Api.Contracts")]
public sealed class PermissionsTests
{
    [Fact(DisplayName = "PolicyName should return Permission prefix followed by the permission value")]
    public void PolicyName_ShouldReturnPermissionPrefixedValue()
    {
        // Arrange
        var permission = Permissions.Read;

        // Act
        var policyName = permission.PolicyName;

        // Assert
        policyName.ShouldBe($"Permission:{permission.Value}");
    }
}
