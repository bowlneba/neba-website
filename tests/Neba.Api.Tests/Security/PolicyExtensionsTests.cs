using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class PolicyExtensionsTests
{
    [Fact(DisplayName = "AddNebaPolicies should succeed when caller has an article management permission")]
    public async Task AddNebaPolicies_ShouldSucceed_WhenCallerHasArticleManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.DeleteArticle.Value)]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageArticlesPolicyName);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddNebaPolicies should fail when caller has no article management permission")]
    public async Task AddNebaPolicies_ShouldFail_WhenCallerHasNoArticleManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, "News.UnrelatedPermission")]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageArticlesPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddNebaPolicies should succeed when caller has a sponsor management permission")]
    public async Task AddNebaPolicies_ShouldSucceed_WhenCallerHasSponsorManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.EditSponsor.Value)]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageSponsorsPolicyName);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddNebaPolicies should fail when caller has no sponsor management permission")]
    public async Task AddNebaPolicies_ShouldFail_WhenCallerHasNoSponsorManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, "Sponsors.UnrelatedPermission")]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageSponsorsPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Theory(DisplayName = "AddNebaPolicies should succeed for CanEditTournamentLogo when caller has EditTournament or EditTournamentLogo")]
    [InlineData("Tournaments.EditTournament")]
    [InlineData("Tournaments.EditTournamentLogo")]
    public async Task AddNebaPolicies_ShouldSucceedForCanEditTournamentLogo_WhenCallerHasEditPermission(string permission)
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Permissions.ClaimType, permission)]));

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanEditTournamentLogoPolicyName);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddNebaPolicies should fail for CanEditTournamentLogo when caller has only CreateTournament")]
    public async Task AddNebaPolicies_ShouldFailForCanEditTournamentLogo_WhenCallerHasOnlyCreateTournament()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.CreateTournament.Value)]));

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanEditTournamentLogoPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Theory(DisplayName = "AddNebaPolicies should succeed for CanUploadTournamentLogo when caller has CreateTournament, EditTournament, or EditTournamentLogo")]
    [InlineData("Tournaments.CreateTournament")]
    [InlineData("Tournaments.EditTournament")]
    [InlineData("Tournaments.EditTournamentLogo")]
    public async Task AddNebaPolicies_ShouldSucceedForCanUploadTournamentLogo_WhenCallerHasUploadPermission(string permission)
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Permissions.ClaimType, permission)]));

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanUploadTournamentLogoPolicyName);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddNebaPolicies should fail for CanUploadTournamentLogo when caller has no tournament logo permission")]
    public async Task AddNebaPolicies_ShouldFailForCanUploadTournamentLogo_WhenCallerHasNoLogoPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.ManageTournamentSponsors.Value)]));

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanUploadTournamentLogoPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddNebaPolicies should not satisfy CanManageTournaments with only EditTournamentLogo")]
    public async Task AddNebaPolicies_ShouldFailForCanManageTournaments_WhenCallerHasOnlyEditTournamentLogo()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.EditTournamentLogo.Value)]));

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageTournamentsPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    private static ServiceProvider BuildAuthorizationServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder().AddNebaPolicies();
        return services.BuildServiceProvider();
    }
}