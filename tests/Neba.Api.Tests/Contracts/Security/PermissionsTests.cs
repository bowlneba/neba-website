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

    [Fact(DisplayName = "ArticleManagementPermissions should contain DeleteArticle")]
    public void ArticleManagementPermissions_ShouldContainDeleteArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.DeleteArticle);
    }

    [Fact(DisplayName = "ArticleManagementPermissions should only contain DeleteArticle")]
    public void ArticleManagementPermissions_ShouldOnlyContainDeleteArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "CanManageArticlesPolicyName should be CanManageArticles")]
    public void CanManageArticlesPolicyName_ShouldBeCanManageArticles()
    {
        // Arrange & Act
        var policyName = Permissions.CanManageArticlesPolicyName;

        // Assert
        policyName.ShouldBe("CanManageArticles");
    }
}