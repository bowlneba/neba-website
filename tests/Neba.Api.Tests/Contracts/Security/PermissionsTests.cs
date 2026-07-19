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

    [Fact(DisplayName = "SponsorManagementPermissions should contain EditSponsor")]
    public void SponsorManagementPermissions_ShouldContainEditSponsor()
    {
        // Arrange & Act
        var permissions = Permissions.SponsorManagementPermissions;

        // Assert
        permissions.ShouldContain(Permissions.EditSponsor);
    }

    [Fact(DisplayName = "SponsorManagementPermissions should only contain CreateSponsor and EditSponsor")]
    public void SponsorManagementPermissions_ShouldOnlyContainCreateSponsorAndEditSponsor()
    {
        // Arrange & Act
        var permissions = Permissions.SponsorManagementPermissions;

        // Assert
        permissions.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "CanManageSponsorsPolicyName should be CanManageSponsors")]
    public void CanManageSponsorsPolicyName_ShouldBeCanManageSponsors()
    {
        // Arrange & Act
        const string policyName = Permissions.CanManageSponsorsPolicyName;

        // Assert
        policyName.ShouldBe("CanManageSponsors");
    }
}