using System.Diagnostics.CodeAnalysis;
using System.Net;

using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Auditing;
using Neba.Api.Contracts.Security;
using Neba.Api.Database.Interceptors;
using Neba.Api.Security.Domain;
using Neba.Api.Tests.Security;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

using Shouldly;

namespace Neba.Api.Tests.Auditing;

// Audit.NET configuration is process-wide static state, so this runs in the same sequential
// collection as the other audit tests. The EF opt-in lists are the production ones
// (AuditingConfiguration.ConfigureSecurityDbContextAuditing), and the DbContext is built by the
// production AddSecurity() - so a missing interceptor or a missing opt-in fails here.
[IntegrationTest]
[Component("Auditing")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "The host is stopped, never disposed - see SecurityApiHost.StopAsync.")]
[Collection("AuditConfigurationSequential")]
public sealed class UserPermissionOverrideAuditIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string TargetEmail = "target@bowlneba.com";

    private InMemoryDataProvider _securityProvider = null!;
    private InMemoryDataProvider _defaultProvider = null!;
    private SecurityApiHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        _securityProvider = new InMemoryDataProvider();
        _defaultProvider = new InMemoryDataProvider();

        Audit.Core.Configuration.Setup()
            .Use(new SecurityAuditDataProviderRouter(_securityProvider, _defaultProvider))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
        Audit.Core.Configuration.ResetCustomActions();

        AuditingConfiguration.ConfigureSecurityDbContextAuditing();

        _host = await SecurityApiHost.StartAsync(fixture.ConnectionString, services =>
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<PooledAuditSaveChangesInterceptor>();
        });

        var enrichment = new AuditEnrichmentAction(_host.App.Services.GetRequiredService<IHttpContextAccessor>());
        Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, enrichment.OnEventSaving);
    }

    public async ValueTask DisposeAsync()
    {
        Audit.Core.Configuration.ResetCustomActions();
        await _host.StopAsync();
    }

    private async Task<ApplicationUser> SeedTargetAsync()
    {
        // Seeded through the fixture's own context, which has no audit interceptor, so the only
        // audit events in the providers are the ones the endpoint under test wrote.
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var role = await roleManager.FindByNameAsync(Roles.Journalist);
        role.ShouldNotBeNull();

        var user = ApplicationUserFactory.Create(userName: TargetEmail, email: TargetEmail, emailConfirmed: true);
        (await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword)).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(user, Roles.Journalist)).Succeeded.ShouldBeTrue();

        return user;
    }

    private async Task<string> WebmasterTokenAsync(Ulid callerId)
        => TestAccessTokenFactory.Create(
            roles: [Roles.Webmaster],
            permissions: await _host.SeededRolePermissionsAsync(Roles.Webmaster),
            userId: callerId);

    private List<(AuditEvent Event, EventEntry Entry)> ClaimEntries()
        => [.. _securityProvider.GetAllEvents()
            .OfType<AuditEventEntityFramework>()
            .SelectMany(auditEvent => auditEvent.EntityFrameworkEvent.Entries
                .Where(entry => entry.ColumnValues.ContainsKey(nameof(IdentityUserClaim<Ulid>.ClaimType)))
                .Select(entry => ((AuditEvent)auditEvent, entry)))];

    [Fact(DisplayName = "PUT /security/users/{id} writes one security audit row per stored override, carrying the caller's actor ID")]
    public async Task Put_ShouldWriteAuditRows_WithCallerActorId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedTargetAsync();
        var callerId = Ulid.NewUlid();
        var token = await WebmasterTokenAsync(callerId);

        // Act
        using var response = await _host.PutOverridesAsync(
            token, target.Id, [Permissions.CreateSponsor.Value], [Permissions.DeleteArticle.Value], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var entries = ClaimEntries();
        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.Entry.Action == "Insert");
        entries.ShouldContain(e =>
            (string)e.Entry.ColumnValues[nameof(IdentityUserClaim<Ulid>.ClaimType)] == Permissions.ClaimType
            && (string)e.Entry.ColumnValues[nameof(IdentityUserClaim<Ulid>.ClaimValue)] == Permissions.CreateSponsor.Value);
        entries.ShouldContain(e =>
            (string)e.Entry.ColumnValues[nameof(IdentityUserClaim<Ulid>.ClaimType)] == Permissions.DeniedClaimType
            && (string)e.Entry.ColumnValues[nameof(IdentityUserClaim<Ulid>.ClaimValue)] == Permissions.DeleteArticle.Value);
        entries.ShouldAllBe(e => (string)e.Event.CustomFields["ActorId"] == callerId.ToString());
        entries.ShouldAllBe(e => e.Event.EventType == "EF:SecurityDbContext");
        _defaultProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "PUT /security/users/{id} writes Delete audit rows when it clears stored overrides")]
    public async Task Put_ShouldWriteDeleteAuditRows_WhenOverridesAreCleared()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedTargetAsync();
        var callerId = Ulid.NewUlid();
        var token = await WebmasterTokenAsync(callerId);

        using var set = await _host.PutOverridesAsync(
            token, target.Id, [Permissions.CreateSponsor.Value], [], ct);
        set.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        _securityProvider.ClearEvents();

        // Act
        using var clear = await _host.PutOverridesAsync(token, target.Id, [], [], ct);

        // Assert
        clear.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var (@event, entry) = ClaimEntries().ShouldHaveSingleItem();
        entry.Action.ShouldBe("Delete");
        entry.ColumnValues[nameof(IdentityUserClaim<Ulid>.ClaimValue)].ShouldBe(Permissions.CreateSponsor.Value);
        @event.CustomFields["ActorId"].ShouldBe(callerId.ToString());
    }

    [Fact(DisplayName = "PUT /security/users/{id} writes no audit rows when the request is rejected")]
    public async Task Put_ShouldWriteNoAuditRows_WhenRequestIsRejected()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var target = await SeedTargetAsync();
        var token = await WebmasterTokenAsync(Ulid.NewUlid());

        // Act
        using var response = await _host.PutOverridesAsync(token, target.Id, [Permissions.ClearCache.Value], [], ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _securityProvider.GetAllEvents().ShouldBeEmpty();
    }
}