using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;

using Microsoft.AspNetCore.Http;

using Neba.Api.Auditing;
using Neba.Api.Database;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Shouldly;

namespace Neba.Api.Tests.Auditing;

[IntegrationTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class SecurityDbContextAuditIntegrationTests(SecurityDbContextFixture securityDbContextFixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private readonly AuditSaveChangesInterceptor _auditInterceptor = new();

    private InMemoryDataProvider _securityProvider = null!;
    private InMemoryDataProvider _defaultProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await securityDbContextFixture.ResetAsync();

        _securityProvider = new InMemoryDataProvider();
        _defaultProvider = new InMemoryDataProvider();

        Audit.Core.Configuration.Setup()
            .Use(new SecurityAuditDataProviderRouter(_securityProvider, _defaultProvider))
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
        Audit.Core.Configuration.ResetCustomActions();

        var enrichmentAction = new AuditEnrichmentAction(new HttpContextAccessor());
        Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, enrichmentAction.OnEventSaving);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<SecurityDbContext>(auditConfig => auditConfig
                .AuditEventType("EF:{context}")
                .IncludeEntityObjects(true))
            .UseOptIn()
            .Include<ApplicationUser>();
    }

    public ValueTask DisposeAsync()
    {
        Audit.Core.Configuration.ResetCustomActions();
        return ValueTask.CompletedTask;
    }

    [Fact(DisplayName = "Saving an ApplicationUser through the real EF pipeline routes the audit event to the security provider")]
    public async Task SaveChanges_ShouldRouteToSecurityProvider_WhenTemplatedEventTypeRendersForSecurityDbContext()
    {
        // Arrange
        await using var dbContext = securityDbContextFixture.CreateDbContext(_auditInterceptor);
        var user = new ApplicationUser { UserName = "pat", Email = "pat@example.com" };
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var securityEvent = _securityProvider.GetAllEvents().ShouldHaveSingleItem();
        securityEvent.EventType.ShouldBe("EF:SecurityDbContext");
        _defaultProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Saving an ApplicationUser through the real EF pipeline scrubs PII before it reaches the audit store")]
    public async Task SaveChanges_ShouldScrubPii_WhenApplicationUserIsSaved()
    {
        // Arrange
        const string email = "pat@example.com";

        await using var dbContext = securityDbContextFixture.CreateDbContext(_auditInterceptor);
        var user = new ApplicationUser { UserName = "pat", Email = email, PasswordHash = "super-secret-hash" };
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);

        // Act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var auditEvent = _securityProvider.GetAllEvents().ShouldHaveSingleItem().ShouldBeOfType<AuditEventEntityFramework>();
        var entry = auditEvent.EntityFrameworkEvent.Entries.ShouldHaveSingleItem();

        entry.ColumnValues.ShouldNotContainKey(nameof(ApplicationUser.PasswordHash));
        entry.ColumnValues[nameof(ApplicationUser.Email)].ShouldBe("p" + new string('*', email.Length - 1));
        entry.Entity.ShouldBeNull();
        auditEvent.ToJson().ShouldNotContain("super-secret-hash");
    }
}