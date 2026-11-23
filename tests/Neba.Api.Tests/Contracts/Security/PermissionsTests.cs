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

    [Fact(DisplayName = "ManageAdvancement should have the value Series.ManageAdvancement")]
    public void ManageAdvancement_ShouldHaveSeriesManageAdvancementValue()
    {
        // Arrange & Act
        var value = Permissions.ManageAdvancement.Value;

        // Assert
        value.ShouldBe("Series.ManageAdvancement");
    }

    [Fact(DisplayName = "FromValue should return ManageAdvancement for the value Series.ManageAdvancement")]
    public void FromValue_ShouldReturnManageAdvancement_WhenValueIsSeriesManageAdvancement()
    {
        // Arrange
        const string value = "Series.ManageAdvancement";

        // Act
        var permission = Permissions.FromValue(value);

        // Assert
        permission.ShouldBe(Permissions.ManageAdvancement);
    }

    [Fact(DisplayName = "List should contain ManageAdvancement once and give every permission a distinct value")]
    public void List_ShouldContainManageAdvancementOnceAndHaveDistinctValues()
    {
        // Arrange & Act
        var values = Permissions.List.Select(p => p.Value).ToList();

        // Assert
        values.Count(v => v == Permissions.ManageAdvancement.Value).ShouldBe(1);
        values.Distinct(StringComparer.Ordinal).Count().ShouldBe(values.Count);
    }

    [Fact(DisplayName = "MakeStageOfficial should have the value Series.MakeStageOfficial")]
    public void MakeStageOfficial_ShouldHaveSeriesMakeStageOfficialValue()
    {
        // Arrange & Act
        var value = Permissions.MakeStageOfficial.Value;

        // Assert
        value.ShouldBe("Series.MakeStageOfficial");
    }

    [Fact(DisplayName = "FromValue should return MakeStageOfficial for the value Series.MakeStageOfficial")]
    public void FromValue_ShouldReturnMakeStageOfficial_WhenValueIsSeriesMakeStageOfficial()
    {
        // Arrange & Act
        var permission = Permissions.FromValue("Series.MakeStageOfficial");

        // Assert
        permission.ShouldBe(Permissions.MakeStageOfficial);
    }

    [Fact(DisplayName = "SeriesAdvancementViewPermissions should hold EditSeries and MakeStageOfficial")]
    public void SeriesAdvancementViewPermissions_ShouldHoldEditSeriesAndMakeStageOfficial()
    {
        // Arrange & Act
        var permissions = Permissions.SeriesAdvancementViewPermissions;

        // Assert
        permissions.Count.ShouldBe(2);
        permissions.ShouldContain(Permissions.EditSeries);
        permissions.ShouldContain(Permissions.MakeStageOfficial);
    }
}
