# Issue #74: Application Auditing via Audit.NET — Implementation Plan

GitHub issue: [#74](https://github.com/bowlneba/neba-website/issues/74)

> **Note on code below**: this is a planning document, not shipped code. The `Audit.NET` / `Audit.EntityFramework` / `Audit.NET.AzureStorageTables` fluent APIs shown are representative of the library's real surface but should be checked against the exact installed version's docs when Phase 1 starts (method names on `Audit.Core.Configuration.Setup()` in particular have shifted across major versions).

## Decisions (resolved during planning)

| Question | Decision |
|---|---|
| Storage backend | **Azure Table Storage** (issue open question #1) |
| EF audit mechanism | `Audit.EntityFramework`'s `AuditSaveChangesInterceptor`, added via `.AddInterceptors(...)` alongside the existing `SlowQueryInterceptor`/`QueryTagEnrichmentInterceptor`/`DomainEventDispatcherInterceptor` — **not** `AuditDbContext` inheritance (issue open question #6) |
| Actor identity | New `ICurrentUserService` abstraction (wraps `IHttpContextAccessor`), shared by the EF interceptor, API audit middleware, and Hangfire filter |
| PII scrubbing | Extend the existing Compliance taxonomy (`[PublicData]`/`[PersonalData]`/`[PrivateData]`) to also support `AttributeTargets.Property`, reused for audit payload scrubbing — no separate `[AuditIgnore]` convention |
| Local dev storage | Azurite table storage via Aspire `storage.AddTables("tables")` — the emulator's `WithTablePort(19634)` is already reserved but currently unused |
| Delivery | Phased (mirrors the Documents feature's 5-phase delivery) |

Still open (per issue, non-blocking — track as follow-up issues, not part of this implementation):
- Retention policy (issue open question #2)
- Who can read audit records / admin UI (issue open question #3)
- Blazor frontend audit scope (issue open question #5) — out of scope; today's Blazor app is read-only and all writes route through the API, so API-layer audit covers it automatically
- Canonical audit event envelope vs. per-provider schemas (issue open question #7) — defer; start with Audit.NET's native per-provider event shapes written to distinct Table Storage tables, revisit a unifying envelope if querying across layers becomes painful

---

## Phase 1 — Infrastructure plumbing + EF Core audit (highest value per issue guideline #2)

### 1a. AppHost — Table Storage resource

`src/Neba.AppHost/AppHost.cs` (full file, changes marked):

```csharp
var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DashboardApplicationName = "NEBA Website",
});

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(container => container
        .WithContainerName("bowlneba-postgres")
        .WithPgAdmin(pgAdmin => pgAdmin
            .WithContainerName("bowlneba-pgadmin")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithHostPort(19631))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHostPort(19630)
        .WithDataVolume("bowlneba-pgdata"));

var database = postgres.AddDatabase("bowlneba");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithContainerName("bowlneba-storage")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("bowlneba-storage-data")
        .WithBlobPort(19632)
        .WithQueuePort(19633)
        .WithTablePort(19634));

var blobs = storage.AddBlobs("blob");
var tables = storage.AddTables("tables"); // new — audit event storage

var api = builder.AddProject<Projects.Neba_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(tables) // new
    .WaitFor(tables) // new
    .WithUrlForEndpoint("http", callback =>
    {
        callback.DisplayText = "Scalar API";
        callback.Url = "/scalar";
    })
    .WithUrls(context =>
    {
        var endpoint = context.GetEndpoint("http")
            ?? throw new InvalidOperationException("HTTP endpoint not found.");

        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/background-jobs",
            DisplayText = "Hangfire Dashboard"
        });

#if DEBUG
        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/debug/cache",
            DisplayText = "Clear Cache"
        });
#endif
    });

#pragma warning disable ASPIREBROWSERLOGS001
var web = builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api)
    .WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

if (builder.ExecutionContext.IsPublishMode)
{
    var workspace = builder.AddAzureLogAnalyticsWorkspace("logs");
    var appInsights = builder.AddAzureApplicationInsights("appinsights")
        .WithLogAnalyticsWorkspace(workspace);

    var keyVault = builder.AddAzureKeyVault("keyvault");

    api
        .WithReference(appInsights)
        .WithReference(keyVault);

    web
        .WithReference(appInsights);
}
else
{
    var mailpit = builder.AddMailPit("mailpit");

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}

await builder.Build().RunAsync();
```

No changes needed to the `IsPublishMode` branch — `AddAzureStorage` already provisions a real Storage Account in Azure; Table Storage is just another service on the same account, exposed automatically once `AddTables` is called on the same `storage` resource.

### 1b. Packages

`Directory.Packages.props` additions:

```xml
<ItemGroup>
  <PackageVersion Include="Audit.NET" Version="27.2.1" />
  <PackageVersion Include="Audit.EntityFramework" Version="27.2.1" />
  <PackageVersion Include="Audit.NET.AzureStorageTables" Version="27.2.1" />
  <PackageVersion Include="Aspire.Azure.Data.Tables" Version="9.5.1" />
</ItemGroup>
```

`src/Neba.Api/Neba.Api.csproj` additions:

```xml
<ItemGroup>
  <PackageReference Include="Audit.NET" />
  <PackageReference Include="Audit.EntityFramework" />
  <PackageReference Include="Audit.NET.AzureStorageTables" />
  <PackageReference Include="Aspire.Azure.Data.Tables" />
</ItemGroup>
```

> Package versions above are placeholders — pin to whatever is current on NuGet.org at implementation time; match the major version across the three `Audit.*` packages.

### 1c. `ICurrentUserService`

New folder `src/Neba.Api/Identity/` (alongside existing identity code — align with wherever `SecurityClaimsBuilder` lives if that turns out to be a better home).

`src/Neba.Api/Identity/ICurrentUserService.cs`:

```csharp
namespace Neba.Api.Identity;

internal interface ICurrentUserService
{
    /// <summary>The authenticated user's NameIdentifier claim, or "anonymous" if unauthenticated.</summary>
    string ActorId { get; }

    bool IsAuthenticated { get; }
}
```

`src/Neba.Api/Identity/CurrentUserService.cs`:

```csharp
using System.Security.Claims;

namespace Neba.Api.Identity;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private const string AnonymousActorId = "anonymous";

    public string ActorId
        => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? AnonymousActorId;

    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
```

Registered scoped (per-request) in `AddAuditing()` — see 1f.

### 1d. Compliance taxonomy — extend attribute targets

`src/Neba.Api/Compliance/PublicDataAttribute.cs`:

```csharp
using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class PublicDataAttribute
    : DataClassificationAttribute
{
    public PublicDataAttribute()
        : base(DataTaxonomy.Public)
    { }
}
```

`src/Neba.Api/Compliance/PersonalDataAttribute.cs`:

```csharp
using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class PersonalDataAttribute
    : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    { }
}
```

`src/Neba.Api/Compliance/PrivateDataAttribute.cs`:

```csharp
using Microsoft.Extensions.Compliance.Classification;

namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class PrivateDataAttribute
    : DataClassificationAttribute
{
    public PrivateDataAttribute()
        : base(DataTaxonomy.Private)
    { }
}
```

`DataTaxonomy.cs` and `RedactionConfiguration.cs` are unchanged — only the attribute usage targets widen.

New: `src/Neba.Api/Compliance/AuditPayloadScrubber.cs`:

```csharp
using System.Collections.Concurrent;
using System.Reflection;

namespace Neba.Api.Compliance;

/// <summary>
/// Applies the [PublicData]/[PersonalData]/[PrivateData] property-level classification to an
/// arbitrary object graph before it is written to an audit store: Private properties are
/// omitted, Personal properties are star-masked, Public/unclassified properties pass through.
/// </summary>
internal static class AuditPayloadScrubber
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static IReadOnlyDictionary<string, object?> Scrub(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var type = source.GetType();
        var properties = PropertyCache.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var result = new Dictionary<string, object?>(properties.Length);

        foreach (var property in properties)
        {
            var value = property.GetValue(source);

            if (property.GetCustomAttribute<PrivateDataAttribute>() is not null)
            {
                continue; // Fully redacted — omit the key entirely.
            }

            if (property.GetCustomAttribute<PersonalDataAttribute>() is not null && value is string stringValue)
            {
                result[property.Name] = Mask(stringValue);
                continue;
            }

            result[property.Name] = value;
        }

        return result;
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value[0] + new string('*', value.Length - 1);
    }
}
```

> This duplicates `StarMaskingRedactor`'s masking rule (first char + stars) rather than depending on `Microsoft.Extensions.Compliance.Redaction.Redactor`, which is designed for `ReadOnlySpan<char>`/logging pipelines, not arbitrary object graphs. If the two ever drift, extract a shared `static string MaskValue(string)` helper both call.

### 1e + 1f. `AddAuditing()` extension and EF interceptor wiring

New file `src/Neba.Api/Auditing/AuditingConfiguration.cs`:

```csharp
using Audit.Core;
using Audit.EntityFramework;

using Neba.Api.Database;
using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal static class AuditingConfiguration
{
    private static readonly string[] AuditedTableNames =
    [
        "bowlers",
        "seasons",
        "tournaments",
        "hall_of_fame_inductions",
        "high_average_awards",
        "high_block_awards",
        "bowler_of_the_year_awards",
        "bowling_centers",
        "sponsors",
    ];

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAuditing()
        {
            builder.AddAzureTableServiceClient("tables");

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<AuditSaveChangesInterceptor>(sp =>
                new AuditSaveChangesInterceptor(new AuditSaveChangesInterceptorOptions
                {
                    AuditEventType = "EF:{context}",
                    IncludeEntityObjects = false, // scrubbed snapshots are attached manually below
                }));

            Configuration.Setup()
                .UseAzureTableStorage(config => config
                    .ConnectionString(builder.Configuration.GetConnectionString("tables"))
                    .TableName(_ => "EFAuditEvents")
                    .EntityBuilder(entity => entity
                        .PartitionKey(ev => ev.EventType ?? "unknown")
                        .RowKey(ev => $"{DateTimeOffset.UtcNow:O}_{Guid.NewGuid()}")
                        .Timestamp(_ => DateTimeOffset.UtcNow)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<AppDbContext>(auditConfig => auditConfig
                    .ForEntity<object>() // baseline; narrowed by IncludeFilter below
                    .IncludeEntityObjects(false))
                .UseOptIn(); // only entities matching IncludeFilter are audited

            return builder;
        }

        internal static bool IsAuditedTable(string? tableName)
            => tableName is not null && AuditedTableNames.Contains(tableName, StringComparer.Ordinal);
    }
}
```

`src/Neba.Api/Database/DatabaseConfiguration.cs` (diff against current file — new interceptor added to the resolution + `.AddInterceptors(...)` call):

```csharp
using Audit.EntityFramework;

using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;

using Npgsql;

namespace Neba.Api.Database;

internal static class DatabaseConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDatabase()
        {
            const string connectionStringName = "bowlneba";

            builder.AddAzureNpgsqlDataSource(connectionStringName, settings =>
            {
                if (!(HasExplicitSslMode(settings.ConnectionString)
                    || IsLocalConnectionString(settings.ConnectionString)))
                {
                    settings.ConnectionString += ";Ssl Mode=Require";
                }
            });

            builder.Services.AddDbContextPool<AppDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                var slowQuery = sp.GetRequiredService<SlowQueryInterceptor>();
                var queryTag = sp.GetRequiredService<QueryTagEnrichmentInterceptor>();
                var domainEvents = sp.GetRequiredService<DomainEventDispatcherInterceptor>();
                var audit = sp.GetRequiredService<AuditSaveChangesInterceptor>(); // new

                options
                    .UseNpgsql(dataSource, npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema))
                    .UseExceptionProcessor()
                    .UseSnakeCaseNamingConvention()
                    .EnableDetailedErrors()
                    .AddInterceptors(slowQuery, queryTag, domainEvents, audit); // audit appended

#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });

            builder.Services.Configure<SlowQueryOptions>(builder.Configuration.GetSection(SlowQueryOptions.SectionName));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SlowQueryOptions>>().Value);

            builder.Services.AddSingleton<SlowQueryInterceptor>();
            builder.Services.AddSingleton<QueryTagEnrichmentInterceptor>();
            builder.Services.AddSingleton<DomainEventDispatcherInterceptor>();
            // AuditSaveChangesInterceptor is registered by AddAuditing() (Neba.Api.Auditing),
            // which must run before AddDatabase() in AddInfrastructure().

            return builder;
        }

        // ... HasExplicitSslMode / IsLocalConnectionString / IsLocalHost unchanged
    }
}
```

> **Ordering constraint**: `AddAuditing()` must run **before** `AddDatabase()` in `AddInfrastructure()` since `AddDatabase()`'s `AddDbContextPool` callback resolves `AuditSaveChangesInterceptor` from `sp`, which `AddAuditing()` registers. See 1f's `InfrastructureConfiguration.cs` diff below.

Custom-field enrichment (actor + correlation ID) and payload scrubbing happen via an `Audit.Core` global filter rather than inside the interceptor itself, since `AuditSaveChangesInterceptor` doesn't expose a per-event customization hook directly — `Audit.Core.Configuration` does, via `AddCustomAction`:

```csharp
// Inside AddAuditing(), after the UseAzureTableStorage(...) call:
Configuration.AddCustomAction(ActionType.OnEventSaving, (scope, _) =>
{
    var httpContextAccessor = builder.Services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>();
    var currentUser = new CurrentUserService(httpContextAccessor);

    scope.Event.CustomFields["ActorId"] = currentUser.ActorId;
    scope.Event.CustomFields["CorrelationId"] =
        System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? httpContextAccessor.HttpContext?.TraceIdentifier
        ?? "none";

    if (scope.Event is AuditEventEntityFramework efEvent)
    {
        foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
        {
            if (!AuditingConfiguration.IsAuditedTable(entry.Table))
            {
                continue; // guideline: only the 7 listed tables produce audit rows
            }

            entry.ColumnValues = entry.Entity is not null
                ? AuditPayloadScrubber.Scrub(entry.Entity)
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                : entry.ColumnValues;
        }
    }
});
```

> **Caveat**: building a fresh `ServiceProvider` inside a custom action is a smell — `IHttpContextAccessor` should be captured once as a field via a small wrapper class registered as a singleton (constructed with `IHttpContextAccessor` injected normally), not rebuilt per save. Restructure this as a dedicated `EfAuditEnrichmentAction` class resolved from DI once at `AddAuditing()` time, rather than the inline lambda sketched above — flag this for cleanup during actual implementation, not left as-is.

`src/Neba.Api/InfrastructureConfiguration.cs` (diff — `AddAuditing()` inserted before `AddDatabase()`):

```csharp
using Neba.Api.Auditing;
using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Clock;
using Neba.Api.Compliance;
using Neba.Api.Database;
using Neba.Api.Documents;
using Neba.Api.Email;
using Neba.Api.RateLimiting;
using Neba.Api.Storage;
using Neba.Api.Telemetry.Tracing;

namespace Neba.Api;

#pragma warning disable CA1708

public static class InfrastructureConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddInfrastructure()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddTracing();
            builder.Services.AddRateLimiting(builder.Configuration);

            builder.Services.DecorateCachedQueryHandlers();

            builder
                .AddAuditing()   // new — must precede AddDatabase()
                .AddDatabase()
                .AddKeyVault()
                .AddStorage()
                .AddEmail()
                .AddRedaction();

            builder.Services.AddCaching(builder.Configuration);
            builder.Services.AddBackgroundJobs(builder.Configuration);
            builder.Services.AddGoogleDrive(builder.Configuration);

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();

            return builder;
        }

        private WebApplicationBuilder AddKeyVault()
        {
            var keyVaultConnectionString = builder.Configuration.GetConnectionString("keyvault");

            if (string.IsNullOrWhiteSpace(keyVaultConnectionString))
            {
                return builder;
            }

            builder.Configuration.AddAzureKeyVaultSecrets(keyVaultConnectionString);

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseInfrastructure()
        {
            app.UseBackgroundJobsDashboard();
            app.UseDocumentSyncJobs();
            app.UseApiAuditMiddleware(); // new — see Phase 2

            return app;
        }
    }
}
```

### 1g. Failure isolation (guideline #7)

```csharp
// Inside AddAuditing(), wrap the data provider so a Table Storage outage
// degrades to a logged warning instead of failing the interceptor's SaveChanges call.
Configuration.DataProviderAs<Audit.Core.Providers.DeferredDataProvider>(); // pseudocode placeholder —
```

Rather than inventing a custom wrapper provider, the simplest robust approach: register `Configuration.SetOnAuditScopeError` / an `AuditScopeFactory` decorator around `Audit.NET.AzureStorageTables`' provider that logs via `ILogger<AuditingConfiguration>` on failure and swallows the exception rather than rethrowing, since `Audit.Core` by default lets data provider exceptions propagate to the caller. Verify against `Audit.NET`'s actual exception-handling hook (`Configuration.ResultFilters` / provider try/catch subclass) during implementation — this is the one piece of the plan most likely to need adjusting once the real package API is in front of you.

### 1h. Tests

`tests/Neba.Api.Tests/Compliance/AuditPayloadScrubberTests.cs`:

```csharp
using Neba.Api.Compliance;

using Shouldly;

namespace Neba.Api.Tests.Compliance;

[UnitTest]
[Component("Compliance")]
public sealed class AuditPayloadScrubberTests
{
    private sealed class SamplePayload
    {
        public string Name { get; init; } = string.Empty;

        [PersonalData]
        public string Email { get; init; } = string.Empty;

        [PrivateData]
        public string Ssn { get; init; } = string.Empty;
    }

    [Fact(DisplayName = "Scrub should omit properties marked PrivateData")]
    public void Scrub_ShouldOmitPrivateDataProperties()
    {
        // Arrange
        var payload = new SamplePayload { Name = "Pat", Email = "pat@example.com", Ssn = "123-45-6789" };

        // Act
        var result = AuditPayloadScrubber.Scrub(payload);

        // Assert
        result.ShouldNotContainKey(nameof(SamplePayload.Ssn));
    }

    [Fact(DisplayName = "Scrub should mask properties marked PersonalData")]
    public void Scrub_ShouldMaskPersonalDataProperties()
    {
        // Arrange
        var payload = new SamplePayload { Name = "Pat", Email = "pat@example.com", Ssn = "123-45-6789" };

        // Act
        var result = AuditPayloadScrubber.Scrub(payload);

        // Assert
        result[nameof(SamplePayload.Email)].ShouldBe("p*************");
    }

    [Fact(DisplayName = "Scrub should pass through unclassified properties unchanged")]
    public void Scrub_ShouldPassThroughUnclassifiedProperties()
    {
        // Arrange
        var payload = new SamplePayload { Name = "Pat", Email = "pat@example.com", Ssn = "123-45-6789" };

        // Act
        var result = AuditPayloadScrubber.Scrub(payload);

        // Assert
        result[nameof(SamplePayload.Name)].ShouldBe("Pat");
    }
}
```

`tests/Neba.Api.Tests/Identity/CurrentUserServiceTests.cs`:

```csharp
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using Neba.Api.Identity;

using Shouldly;

namespace Neba.Api.Tests.Identity;

[UnitTest]
[Component("Identity")]
public sealed class CurrentUserServiceTests
{
    [Fact(DisplayName = "ActorId should return the NameIdentifier claim when authenticated")]
    public void ActorId_ShouldReturnNameIdentifierClaim_WhenAuthenticated()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-123")], "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new CurrentUserService(accessor);

        // Act
        var actorId = sut.ActorId;

        // Assert
        actorId.ShouldBe("user-123");
    }

    [Fact(DisplayName = "ActorId should return anonymous when there is no HttpContext")]
    public void ActorId_ShouldReturnAnonymous_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new CurrentUserService(accessor);

        // Act
        var actorId = sut.ActorId;

        // Assert
        actorId.ShouldBe("anonymous");
    }

    [Fact(DisplayName = "IsAuthenticated should be false when there is no HttpContext")]
    public void IsAuthenticated_ShouldBeFalse_WhenHttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new CurrentUserService(accessor);

        // Act
        var isAuthenticated = sut.IsAuthenticated;

        // Assert
        isAuthenticated.ShouldBeFalse();
    }
}
```

Integration test sketch (`tests/Neba.Api.Tests/Auditing/EfAuditIntegrationTests.cs`), following the `AzuriteFixture` pattern already used for blob storage — extend the fixture (or add a sibling `AzuriteTableFixture`) to also expose a `TableServiceClient`:

```csharp
using Azure.Data.Tables;

using Neba.TestFactory.Infrastructure;

using Shouldly;

namespace Neba.Api.Tests.Auditing;

[IntegrationTest]
[Component("Auditing")]
public sealed class EfAuditIntegrationTests(AzuriteFixture azurite) : IClassFixture<AzuriteFixture>
{
    [Fact(DisplayName = "Saving a change to an audited table should write an EF audit event")]
    public async Task SaveChanges_ShouldWriteAuditEvent_WhenTableIsAudited()
    {
        // Arrange
        var tableClient = azurite.TableServiceClient.GetTableClient("EFAuditEvents");
        await tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);

        // ... build a DbContext wired to AuditSaveChangesInterceptor pointed at azurite,
        // save a new Bowler entity ...

        // Act
        // await dbContext.SaveChangesAsync(...);

        // Assert
        var events = tableClient.QueryAsync<TableEntity>(cancellationToken: TestContext.Current.CancellationToken);
        (await events.CountAsync()).ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Saving a change to a non-audited table should not write an audit event")]
    public async Task SaveChanges_ShouldNotWriteAuditEvent_WhenTableIsNotAudited()
    {
        // Arrange / Act / Assert — mirror above against a table not in AuditedTableNames
    }
}
```

---

## Phase 2 — API command endpoint audit

No existing FastEndpoints pre/post-processor pipeline exists in the codebase — this is the first usage. Rather than depending on `Audit.WebApi`/`Audit.WebApi.Core`'s exact wiring (built primarily around MVC filters, which this codebase doesn't use), implement a small first-party ASP.NET Core middleware built directly on `Audit.Core.AuditScope`, registered in the standard middleware pipeline (works identically under FastEndpoints since FastEndpoints is just terminal middleware).

`src/Neba.Api/Auditing/ApiAuditMiddleware.cs`:

```csharp
using Audit.Core;

using Neba.Api.Compliance;
using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal sealed class ApiAuditMiddleware(RequestDelegate next, ICurrentUserService currentUser, ILogger<ApiAuditMiddleware> logger)
{
    private static readonly string[] ExcludedPathPrefixes = ["/health", "/scalar", "/background-jobs", "/debug"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await next(context);
            return;
        }

        var startedAt = DateTimeOffset.UtcNow;

        AuditScope? scope = null;

        try
        {
            scope = await AuditScope.CreateAsync(options => options
                .EventType("Api:{verb}:{url}")
                .ExtraFields(new
                {
                    Route = context.Request.Path.Value,
                    Method = context.Request.Method,
                    ActorId = currentUser.ActorId,
                    CorrelationId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                    StartedAt = startedAt,
                }));
        }
        catch (Exception ex)
        {
            // Guideline #7 — audit failures must never fail the request.
            LogAuditScopeCreationFailed(logger, ex);
        }

        await next(context);

        if (scope is null)
        {
            return;
        }

        try
        {
            scope.Event.CustomFields["StatusCode"] = context.Response.StatusCode;
            scope.Event.CustomFields["ElapsedMs"] = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

            await scope.DisposeAsync();
        }
        catch (Exception ex)
        {
            LogAuditScopeCompletionFailed(logger, ex);
        }
    }

    private static bool ShouldSkip(HttpContext context)
        => HttpMethods.IsGet(context.Request.Method)
        || ExcludedPathPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix));

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to create API audit scope; continuing request without audit.")]
    private static partial void LogAuditScopeCreationFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to complete API audit scope.")]
    private static partial void LogAuditScopeCompletionFailed(ILogger logger, Exception exception);
}
```

> `ApiAuditMiddleware` needs `partial class` + the file needs `partial` on the class declaration for `[LoggerMessage]` source generation — omitted above for brevity, add `partial` when implementing.

Request payload capture (route + method + status are enough for the AC's minimum bar; if the full scrubbed request body is wanted per the issue's "Request payload (with field-level scrubbing)" bullet, add it via a `Stream`-buffering step before FastEndpoints binds the request, then run `AuditPayloadScrubber.Scrub(...)` over the bound `Request` object from within the FastEndpoints handler's `Send.OkAsync` pipeline — this needs its own design pass since it means either buffering the request body twice or hooking FastEndpoints' `PreProcessor` extension point once one exists. Defer to a Phase 2b task rather than blocking Phase 2's route/status/actor baseline.

`src/Neba.Api/Auditing/AuditingConfiguration.cs` addition (`UseApiAuditMiddleware()`):

```csharp
extension(WebApplication app)
{
    public WebApplication UseApiAuditMiddleware()
    {
        app.UseMiddleware<ApiAuditMiddleware>();
        return app;
    }
}
```

Wired from `InfrastructureConfiguration.UseInfrastructure()` — see Phase 1's diff of that file above (`app.UseApiAuditMiddleware();` already inserted there).

Separate Azure Table (`ApiAuditEvents`) registered in `AddAuditing()` alongside `EFAuditEvents`, partition key = route, row key = `{timestamp}_{eventId}` per the issue's suggested scheme.

### Phase 2 Tests

`tests/Neba.Api.Tests/Auditing/ApiAuditMiddlewareIntegrationTests.cs`:

```csharp
[IntegrationTest]
[Component("Auditing")]
public sealed class ApiAuditMiddlewareIntegrationTests(NebaApiFactory factory) : IClassFixture<NebaApiFactory>
{
    [Fact(DisplayName = "A command endpoint should produce an API audit event")]
    public async Task Post_ShouldProduceAuditEvent_WhenEndpointIsACommand()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        // var response = await client.PostAsJsonAsync("/bowlers/.../assign-high-average-award", request, ct);

        // Assert
        // query ApiAuditEvents table, assert a row with matching route + 200 status + actor
    }

    [Fact(DisplayName = "A GET endpoint should not produce an API audit event")]
    public async Task Get_ShouldNotProduceAuditEvent()
    {
        // Arrange / Act / Assert — mirror above, assert zero matching rows
    }
}
```

(`NebaApiFactory` above is a placeholder for whatever `WebApplicationFactory<Program>` fixture the integration test suite already uses — confirm actual fixture name during implementation.)

---

## Phase 3 — Hangfire background job audit

`src/Neba.Api/BackgroundJobs/AuditJobFilterAttribute.cs`, mirroring `HangfireJobExpirationFilterAttribute`'s shape:

```csharp
using System.Diagnostics;

using Audit.Core;

using Hangfire.Common;
using Hangfire.Server;

namespace Neba.Api.BackgroundJobs;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class AuditJobFilterAttribute : JobFilterAttribute, IServerFilter
{
    private const string StartedAtKey = "AuditJobFilter.StartedAt";

    public void OnPerforming(PerformingContext context)
    {
        context.Items[StartedAtKey] = DateTimeOffset.UtcNow;
    }

    public void OnPerformed(PerformedContext context)
    {
        var startedAt = context.Items.TryGetValue(StartedAtKey, out var value) && value is DateTimeOffset started
            ? started
            : DateTimeOffset.UtcNow;

        var scope = AuditScope.Create(options => options
            .EventType("Job:{jobType}")
            .ExtraFields(new
            {
                JobType = context.BackgroundJob.Job.Type.Name,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Succeeded = context.Exception is null,
                // guideline #9 — outcome only, never the serialized job arguments
                ExceptionSummary = context.Exception?.Message,
            }));

        scope.Dispose();
    }
}
```

`src/Neba.Api/BackgroundJobs/BackgroundJobsConfiguration.cs` diff (add the filter alongside the existing one):

```csharp
options
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseFilter(new AutomaticRetryAttribute { Attempts = settings.AutomaticRetryAttempts })
    .UseFilter(new HangfireJobExpirationFilterAttribute(settings))
    .UseFilter(new AuditJobFilterAttribute()) // new
    .UsePostgreSqlStorage(...);
```

Separate Azure Table (`JobAuditEvents`) registered in `AddAuditing()`.

### Phase 3 Tests

`tests/Neba.Api.Tests/BackgroundJobs/AuditJobFilterAttributeTests.cs` — unit tests around `OnPerforming`/`OnPerformed` using Hangfire's testable filter contexts (construct `PerformingContext`/`PerformedContext` directly, matching however `HangfireJobExpirationFilterAttributeTests` — if one exists — builds `ApplyStateContext`). Integration test: run `SyncDocumentToStorageJobHandler` end-to-end via the real Hangfire pipeline, assert a `JobAuditEvents` row for both a successful run and a forced-failure run.

---

## Phase 4 — Docs, hardening, cleanup

- Update `docs/architecture/backend.md` with the finalized auditing guidelines (issue's 9 guidelines, adjusted for the decisions above — e.g. guideline #4 names "Azure Table Storage" concretely, guideline #6 references the extended Compliance taxonomy instead of a hypothetical `[AuditIgnore]`).
- Confirm managed identity / RBAC for the audit tables is write-only (no delete) in production — coordinate with whatever provisions the Storage Account's role assignments (append-only enforcement per guideline #4). This is an infra/RBAC change outside the app code — flag it as a deployment checklist item, not a code change.
- Resolve the two flagged implementation smells before merging: (1) the inline `ServiceProvider` rebuild in the `OnEventSaving` custom action (1e/1f) should become a proper singleton-resolved enrichment class; (2) the failure-isolation wrapper around the Table Storage data provider (1g) should be validated against the real `Audit.NET` exception-handling API rather than the placeholder shown.
- Full regression pass: `dotnet test --filter "Category=Unit"` and `dotnet test --filter "Category=Integration"`.
- Revisit whether `AuditPayloadScrubber` and `ICurrentUserService` should move to `Neba.Application`/a shared layer if reused beyond `Neba.Api` (currently no such consumer, so keep in `Neba.Api` per YAGNI).

## Acceptance criteria mapping

All 8 acceptance criteria in the issue are covered: Phase 1 → criteria 1 ("EF Core mutations..."), 4 & 5 (fire-and-forget, PII scrubbing) for the EF path; Phase 2 → criterion 2; Phase 3 → criterion 3; Phase 1e/1f → criterion 6 (`AddAuditing()`); Phase 4 → criteria 7 & 8 (docs, tests already threaded through each phase but the full-suite pass closes this out).
