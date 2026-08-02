# Software Backdoor — Scaffolding

Initial structure for the `/legacy` backdoor described in `docs/api/software-backdoor-plan.md`: the route group, API-key auth, and the packages needed to read from the Software's database (`neba-fwk`). No individual action route (`NewBowler`, `UpdateBowler`, etc.) is part of this plan — those are separate follow-up branches built on top of this scaffolding.

## Decisions locked in during scoping

- **Separate `Neba.Api.Legacy` class library — rejected.** It would need `InternalsVisibleTo`/a project reference to `Neba.Api` (to call `Bowler.Create(...)`, use `AppDbContext`, etc.), but `Neba.Api`'s `Program.cs` needs to call the legacy project's endpoint-registration method — a cycle (`Api → Legacy → Api`) that .NET project references don't allow without falling back to reflection-based discovery, which is real ceremony for code meant to be deleted wholesale at sunset. Keeping it as `src/Neba.Api/Legacy/` (a folder, per the plan doc) gives the same "clean break at sunset" outcome — delete the folder, delete one line, remove the API key — with none of the cycle problem.
- **Auth mechanism**: a Minimal API route-group `IEndpointFilter`, not a full ASP.NET Core `AuthenticationScheme`. The app already has a default JWT bearer scheme (`SecurityConfiguration.cs`); a filter scoped to the `/legacy` group sidesteps any interaction with that default scheme.
- **DB read side**: `Dapper` + `Microsoft.Data.SqlClient` (new to this codebase — the app's own DB is Postgres via Npgsql). `neba-fwk` is Azure SQL Database, reached with a plain ADO.NET connection string, not EF6/`System.Data.EntityClient`'s metadata-wrapped one (see Secrets section below).
- **Secrets**: `LEGACY_API_KEY` and `LEGACY_DB_CONNECTION_STRING` GitHub secrets, seeded into Key Vault by `cd.yml` the same way `JWT_SIGNING_KEY`/`GOOGLE_*` already are.

## Phase 1: API

### New files

`src/Neba.Api/Legacy/LegacySettings.cs`:

```csharp
namespace Neba.Api.Legacy;

/// <summary>
/// Configuration for the temporary `/legacy` backdoor (see docs/api/software-backdoor-plan.md).
/// Deleted along with the rest of Legacy/ at Software sunset.
/// </summary>
internal sealed record LegacySettings
{
    /// <summary>
    /// Shared secret the Software presents via the <c>X-Api-Key</c> header on every `/legacy` request.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Plain ADO.NET connection string to the Software's own database (`neba-fwk`, Azure SQL).
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
```

`src/Neba.Api/Legacy/LegacyApiKeyFilter.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace Neba.Api.Legacy;

/// <summary>
/// Route-group filter for `/legacy` — checks the <c>X-Api-Key</c> header against the configured
/// shared secret. Deliberately a filter, not an ASP.NET Core AuthenticationScheme: the app already
/// has a default JWT bearer scheme (see SecurityConfiguration), and scoping auth to just this group
/// avoids any interaction with that default.
/// </summary>
internal sealed class LegacyApiKeyFilter(IOptions<LegacySettings> settings) : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var providedKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        if (!IsValidKey(providedKey))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private bool IsValidKey(string providedKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        // Fixed-time comparison: a shared API key is a secret worth comparing safely,
        // same reasoning as password/token comparisons elsewhere in the app.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(settings.Value.ApiKey));
    }
}
```

`src/Neba.Api/Legacy/LegacyConfiguration.cs`:

```csharp
using System.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Neba.Api.Legacy;

internal static class LegacyConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddLegacy()
        {
            builder.Services
                .AddOptions<LegacySettings>()
                .Bind(builder.Configuration.GetSection("Legacy"))
                .ValidateOnStart();

            builder.Services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(sp.GetRequiredService<IOptions<LegacySettings>>().Value.ConnectionString));

            return builder;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        public void MapLegacyGroup()
        {
            var group = app.MapGroup("/legacy")
                .AddEndpointFilter<LegacyApiKeyFilter>();

            group.MapLegacyEndpoints();
        }
    }
}
```

`src/Neba.Api/Legacy/LegacyEndpoints.cs` (the aggregator from the plan doc — empty until the first action lands):

```csharp
namespace Neba.Api.Legacy;

internal static class LegacyEndpoints
{
    public static void MapLegacyEndpoints(this IEndpointRouteBuilder app)
    {
        // One line per Legacy/*.cs action file (MapNewBowler, MapUpdateBowler, ...),
        // added by each action's own follow-up branch.
    }
}
```

### Composition root

`Program.cs` — add the `using` and two calls:

```csharp
using Neba.Api.Legacy;
```

```csharp
// alongside the existing builder chain
builder.AddLegacy();
```

```csharp
// after app.UseFastEndpoints(...), alongside app.UseOpenApiDocumentation() etc.
app.MapLegacyGroup();
```

### Packages

`Neba.Api.csproj` — add to the existing `<ItemGroup>` of `PackageReference`s (alphabetical, matching the existing list):

```xml
<PackageReference Include="Dapper" />
```

```xml
<PackageReference Include="Microsoft.Data.SqlClient" />
```

`Directory.Packages.props` — add to the central version list (alphabetical):

```xml
<PackageVersion Include="Dapper" Version="2.1.66" />
```

```xml
<PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.4" />
```

Confirm both versions against NuGet at implementation time — pin whatever the current stable release is rather than trusting these numbers verbatim.

### Config / secrets

`appsettings.json` — add a new top-level section (values empty; real values come from Key Vault/user secrets, same pattern as `EmailSettings:AppPassword`/`JwtSettings:SigningKey`):

```json
"Legacy": {
  "ApiKey": "",
  "ConnectionString": ""
},
```

`appsettings.Development.json` — local dev values (don't commit a real key/connection string; use `dotnet user-secrets set Legacy:ApiKey ...` / `Legacy:ConnectionString` locally instead, same as `JwtSettings:SigningKey`'s dev placeholder does for a non-sensitive dev-only value — a real `neba-fwk` connection string is sensitive even in dev, so leave it out of the checked-in Development file entirely):

```json
"Legacy:ApiKey": "dev-only-legacy-key",
```

GitHub secrets (repo-level, consumed by `cd.yml`): `LEGACY_API_KEY`, `LEGACY_DB_CONNECTION_STRING`.

`.github/workflows/cd.yml` — "Seed Key Vault secrets" step, add to both the `env:` block and the `run:` script, alongside the existing `JWT_SIGNING_KEY`/`GOOGLE_*` entries:

```yaml
# env: block
LEGACY_API_KEY: ${{ secrets.LEGACY_API_KEY }}
LEGACY_DB_CONNECTION_STRING: ${{ secrets.LEGACY_DB_CONNECTION_STRING }}
```

```bash
# run: block
az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "Legacy--ApiKey" \
  --value "$LEGACY_API_KEY"

az keyvault secret set --vault-name "$VAULT_NAME" \
  --name "Legacy--ConnectionString" \
  --value "$LEGACY_DB_CONNECTION_STRING"
```

**`LEGACY_DB_CONNECTION_STRING` value — plain ADO.NET string, not the EF6/`System.Data.EntityClient` wrapper.** The Software's own `App.config` entry is EF6's `EntityClient` format, which wraps a *provider connection string* inside `metadata=...` XML:

```xml
<add name="Entities" connectionString="metadata=res://*/NEBADataModel.csdl|res://*/NEBADataModel.ssdl|res://*/NEBADataModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=bowlneba-eastus.database.windows.net;initial catalog=neba;persist security info=True;user id={0};password={1};MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" />
```

Dapper talks to `Microsoft.Data.SqlClient` directly — it has no EF metadata layer, so only the inner `provider connection string` value is needed, with `&quot;` unescaped and `{0}`/`{1}` filled with the real credentials. The `LEGACY_DB_CONNECTION_STRING` GitHub secret (and the Key Vault secret it seeds) should hold just:

```
data source=bowlneba-eastus.database.windows.net;initial catalog=neba;persist security info=True;user id={0};password={1};MultipleActiveResultSets=True
```

(`App=EntityFramework` dropped — that's EF6 identifying itself to SQL Server for diagnostics, meaningless from a non-EF Dapper connection. Fine to leave it in if there's a reason to keep the two systems showing the same `App` name in SQL Server's connection telemetry, but it's not required.)

### Tests

`tests/Neba.Api.Tests/Legacy/LegacyApiKeyFilterTests.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyApiKeyFilterTests
{
    private const string ValidApiKey = "test-legacy-api-key";
    private const string HeaderName = "X-Api-Key";

    [Fact(DisplayName = "Should return Unauthorized when the header is missing")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenHeaderIsMissing()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(headerValue: null);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "Should return Unauthorized when the key does not match")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenKeyDoesNotMatch()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext("wrong-key");

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "Should call next when the key matches")]
    public async Task InvokeAsync_ShouldCallNext_WhenKeyMatches()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(ValidApiKey);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<Ok>();
    }

    private static ValueTask<object?> NextReturnsOk(EndpointFilterInvocationContext _) =>
        ValueTask.FromResult<object?>(Results.Ok());

    private static LegacyApiKeyFilter CreateFilter() =>
        new(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

    private static EndpointFilterInvocationContext CreateInvocationContext(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();

        if (headerValue is not null)
        {
            httpContext.Request.Headers[HeaderName] = headerValue;
        }

        return EndpointFilterInvocationContext.Create(httpContext);
    }
}
```

No integration test yet — nothing end-to-end to exercise until the first real action (`NewBowler`) lands.

### Explicitly out of scope

- Any actual `/legacy/*` action route.
- Hangfire job classes for individual actions.
- `LegacyId`/`SoftwareId` columns on website aggregates.

## Phase 2: UI

Not applicable — this is a machine-to-machine bridge with no user-facing surface.
