using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Domain;

[UnitTest]
[Component("Security")]
public sealed class ApplicationRoleTests
{
    [Fact(DisplayName = "Parameterless constructor should assign a new Ulid as Id")]
    public void Constructor_ShouldAssignNewUlid_WhenCalledWithoutArguments()
    {
        // Arrange & Act
        var role = new ApplicationRole();

        // Assert
        role.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Constructor with role name should assign the role name and a new Ulid as Id")]
    public void Constructor_ShouldAssignRoleNameAndNewUlid_WhenCalledWithRoleName()
    {
        // Arrange & Act
        var role = new ApplicationRole("Admin");

        // Assert
        role.Name.ShouldBe("Admin");
        role.Id.ShouldNotBe(default);
    }
}
