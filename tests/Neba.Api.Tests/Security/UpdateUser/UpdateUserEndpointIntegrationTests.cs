using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.UpdateUser;

// A real host with the production handlers: the override rules, the permission policy, the token
// resolution and the database are all real. Only the HTTP client and the seeded users are test code.
[IntegrationTest]
[Component("Security")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "The host is stopped, never disposed - see SecurityApiHost.StopAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class UpdateUserEndpointIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string TargetEmail = "target@bowlneba.com";

    private SecurityApiHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _host = await SecurityApiHost.StartAsync(fixture.ConnectionString);
    }

    public async ValueTask DisposeAsync()
        => await _host.StopAsync();

    private async Task<ApplicationUser> SeedUserAsync(string role, string email = TargetEmail)
    {
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = ApplicationUserFactory.Create(userName: email, email: email, emailConfirmed: true);
        (await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword)).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();

        return user;
    }

    private async Task<string> CallerTokenAsync(string role, Ulid? callerId = null)
    {
        var permissions = await _host.SeededRolePermissionsAsync(role);
        return TestAccessTokenFactory.Create(roles: [role], permissions: permissions, userId: callerId);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 401 when no access token is provided")]
    public async Task Put_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/security/users/{Ulid.NewUlid()}")
        {
            Content = JsonContent.Create(new
            {
                userId = Ulid.NewUlid().ToString(),
                user = new { addedPermissions = Array.Empty<string>(), removedPermissions = Array.Empty<string>() }
            })
        };

        // Act
        using var response = await _host.Client.SendAsync(request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 403 when the caller lacks the System.EditUser permission")]
    public async Task Put_ShouldReturn403_WhenCallerLacksEditUserPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedUserAsync(Roles.Journalist);
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.GetUsers]);

        // Act
        using var response = await _host.PutOverridesAsync(token, target.Id, [], [Permissions.CreateArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 204 for a Webmaster who sets overrides on a Journalist")]
    public async Task Put_ShouldReturn204_WhenWebmasterEditsJournalist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedUserAsync(Roles.Journalist);
        var token = await CallerTokenAsync(Roles.Webmaster);

        // Act
        using var response = await _host.PutOverridesAsync(
            token, target.Id, [Permissions.CreateSponsor.Value], [Permissions.DeleteArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 403 when a Webmaster adds a permission the Webmaster role lacks")]
    public async Task Put_ShouldReturn403_WhenWebmasterAddsPermissionTheRoleLacks()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedUserAsync(Roles.Journalist);
        var token = await CallerTokenAsync(Roles.Webmaster);
        (await _host.SeededRolePermissionsAsync(Roles.Webmaster)).ShouldNotContain(Permissions.ClearCache);

        // Act
        using var response = await _host.PutOverridesAsync(token, target.Id, [Permissions.ClearCache.Value], [], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 409 when a Webmaster edits an Admin")]
    public async Task Put_ShouldReturn409_WhenTargetIsAdmin()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var admin = await SeedUserAsync(Roles.Admin);
        var token = await CallerTokenAsync(Roles.Webmaster);

        // Act
        using var response = await _host.PutOverridesAsync(token, admin.Id, [], [Permissions.CreateArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 403 when the caller edits their own account")]
    public async Task Put_ShouldReturn403_WhenCallerEditsOwnAccount()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var self = await SeedUserAsync(Roles.Webmaster);
        var token = await CallerTokenAsync(Roles.Webmaster, callerId: self.Id);

        // Act
        using var response = await _host.PutOverridesAsync(token, self.Id, [], [Permissions.CreateArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 404 when the target user does not exist")]
    public async Task Put_ShouldReturn404_WhenTargetDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = await CallerTokenAsync(Roles.Webmaster);

        // Act
        using var response = await _host.PutOverridesAsync(token, Ulid.NewUlid(), [], [], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /security/users/{id} returns 400 when a permission is in both sets")]
    public async Task Put_ShouldReturn400_WhenPermissionIsInBothSets()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedUserAsync(Roles.Journalist);
        var token = await CallerTokenAsync(Roles.Webmaster);

        // Act
        using var response = await _host.PutOverridesAsync(
            token, target.Id, [Permissions.CreateArticle.Value], [Permissions.CreateArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Login returns role permissions plus Added minus Removed after PUT /security/users/{id}")]
    public async Task Login_ShouldReflectOverrides_AfterUserIsUpdated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedUserAsync(Roles.TournamentDirector);
        var token = await CallerTokenAsync(Roles.Webmaster);

        var before = await _host.LoginPermissionsAsync(TargetEmail, LoginRequestFactory.ValidPassword, ct);
        before.ShouldContain(Permissions.CreateTournament.Value);
        before.ShouldNotContain(Permissions.CreateSponsor.Value);

        // Act
        using var put = await _host.PutOverridesAsync(
            token, target.Id, [Permissions.CreateSponsor.Value], [Permissions.CreateTournament.Value], ct);
        var after = await _host.LoginPermissionsAsync(TargetEmail, LoginRequestFactory.ValidPassword, ct);

        // Assert
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        after.ShouldContain(Permissions.CreateSponsor.Value);
        after.ShouldNotContain(Permissions.CreateTournament.Value);
        after.ShouldContain(Permissions.EditTournament.Value);
    }
}