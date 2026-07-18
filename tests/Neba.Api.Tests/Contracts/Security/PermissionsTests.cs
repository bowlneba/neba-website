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
        var permission = Permissions.CreateArticle;

        // Act
        var policyName = permission.PolicyName;

        // Assert
        policyName.ShouldBe($"Permission:{permission.Value}");
    }

    [Fact(DisplayName = "ArticleManagementPermissions should contain CreateArticle")]
    public void ArticleManagementPermissions_ShouldContainCreateArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.CreateArticle);
    }

    [Fact(DisplayName = "ArticleManagementPermissions should contain EditArticle")]
    public void ArticleManagementPermissions_ShouldContainEditArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.EditArticle);
    }

    [Fact(DisplayName = "ArticleManagementPermissions should contain DeleteArticle")]
    public void ArticleManagementPermissions_ShouldContainDeleteArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.DeleteArticle);
    }

    [Fact(DisplayName = "ArticleManagementPermissions should only contain CreateArticle, EditArticle, and DeleteArticle")]
    public void ArticleManagementPermissions_ShouldOnlyContainCreateArticleEditArticleAndDeleteArticle()
    {
        // Arrange & Act
        var permissions = Permissions.ArticleManagementPermissions;

        // Assert
        permissions.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "CanManageArticlesPolicyName should be CanManageArticles")]
    public void CanManageArticlesPolicyName_ShouldBeCanManageArticles()
    {
        // Arrange & Act
        const string policyName = Permissions.CanManageArticlesPolicyName;

        // Assert
        policyName.ShouldBe("CanManageArticles");
    }

    [Fact(DisplayName = "SponsorManagementPermissions should contain CreateSponsor")]
    public void SponsorManagementPermissions_ShouldContainCreateSponsor()
    {
        // Arrange & Act
        var permissions = Permissions.SponsorManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.CreateSponsor);
    }

    [Fact(DisplayName = "SponsorManagementPermissions should only contain CreateSponsor")]
    public void SponsorManagementPermissions_ShouldOnlyContainCreateSponsor()
    {
        // Arrange & Act
        var permissions = Permissions.SponsorManagementPermissions;

        // Assert
        permissions.Count.ShouldBe(1);
    }
}