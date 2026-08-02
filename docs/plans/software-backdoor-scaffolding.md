# Software Backdoor — Scaffolding

Initial structure for the `/legacy` backdoor described in `docs/api/software-backdoor-plan.md`: the route group, API-key auth, and the packages needed to read from the Software's database (`neba-fwk`). No individual action route (`NewBowler`, `UpdateBowler`, etc.) is part of this plan — those are separate follow-up branches built on top of this scaffolding.

## Decisions locked in during scoping

- **Separate `Neba.Api.Legacy` class library — rejected.** It would need `InternalsVisibleTo`/a project reference to `Neba.Api` (to call `Bowler.Create(...)`, use `AppDbContext`, etc.), but `Neba.Api`'s `Program.cs` needs to call the legacy project's endpoint-registration method — a cycle (`Api → Legacy → Api`) that .NET project references don't allow without falling back to reflection-based discovery, which is real ceremony for code meant to be deleted wholesale at sunset. Keeping it as `src/Neba.Api/Legacy/` (a folder, per the plan doc) gives the same "clean break at sunset" outcome — delete the folder, delete one line, remove the API key — with none of the cycle problem.
- **Auth mechanism**: a Minimal API route-group `IEndpointFilter`, not a full ASP.NET Core `AuthenticationScheme`. The app already has a default JWT bearer scheme (`SecurityConfiguration.cs`); a filter scoped to the `/legacy` group sidesteps any interaction with that default scheme.
- **DB read side**: `Dapper` + `Microsoft.Data.SqlClient` (new to this codebase — the app's own DB is Postgres via Npgsql). `neba-fwk` is Azure SQL Database, reached with a plain ADO.NET connection string, not EF6/`System.Data.EntityClient`'s metadata-wrapped one (see Secrets section below).
- **Secrets**: `LEGACY_API_KEY` and `LEGACY_DB_CONNECTION_STRING` GitHub secrets, seeded into Key Vault by `cd.yml` the same way `JWT_SIGNING_KEY`/`GOOGLE_*` already are.

## Phase 1: API

### New files

- `src/Neba.Api/Legacy/LegacySettings.cs` — options record bound from config: `ApiKey`, `ConnectionString` (for `neba-fwk`).
- `src/Neba.Api/Legacy/LegacyApiKeyFilter.cs` — `IEndpointFilter` checking a request header (`X-Api-Key`) against `LegacySettings.ApiKey`; `Results.Unauthorized()` on mismatch/missing.
- `src/Neba.Api/Legacy/LegacyConfiguration.cs`:
  - `AddLegacy()` (`WebApplicationBuilder` extension) — binds `LegacySettings`, registers a scoped `IDbConnection` factory (`SqlConnection` against `LegacySettings.ConnectionString`) for Dapper reads.
  - `MapLegacyGroup()` (`WebApplication`/`IEndpointRouteBuilder` extension) — creates the `/legacy` `RouteGroupBuilder` with `.AddEndpointFilter<LegacyApiKeyFilter>()`, calls `MapLegacyEndpoints()` on it.
- `src/Neba.Api/Legacy/LegacyEndpoints.cs` — the aggregator from the plan doc; empty body for now, individual actions add their `app.MapXxx()` line in their own PR.

### Composition root

- `Program.cs` — `builder.AddLegacy()` alongside the existing `AddInfrastructure()`/`AddSecurity()` chain; `app.MapLegacyGroup()` after `app.UseFastEndpoints()`.

### Packages (`Neba.Api.csproj`)

- `Dapper`
- `Microsoft.Data.SqlClient`

### Config / secrets

- **App config keys**: `Legacy:ApiKey`, `Legacy:ConnectionString` (bound into `LegacySettings`). Local/dev value via user secrets.
- **GitHub secrets** (repo-level, consumed by `cd.yml`): `LEGACY_API_KEY`, `LEGACY_DB_CONNECTION_STRING`.
- **`cd.yml` "Seed Key Vault secrets" step** — add alongside the existing `JWT_SIGNING_KEY`/`GOOGLE_*` secrets:

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

- **`LEGACY_DB_CONNECTION_STRING` value — plain ADO.NET string, not the EF6/`System.Data.EntityClient` wrapper.** The Software's own `App.config` entry is EF6's `EntityClient` format, which wraps a *provider connection string* inside `metadata=...` XML:

  ```xml
  <add name="Entities" connectionString="metadata=res://*/NEBADataModel.csdl|res://*/NEBADataModel.ssdl|res://*/NEBADataModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=bowlneba-eastus.database.windows.net;initial catalog=neba;persist security info=True;user id={0};password={1};MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" />
  ```

  Dapper talks to `Microsoft.Data.SqlClient` directly — it has no EF metadata layer, so only the inner `provider connection string` value is needed, with `&quot;` unescaped and `{0}`/`{1}` filled with the real credentials. The `LEGACY_DB_CONNECTION_STRING` GitHub secret (and the Key Vault secret it seeds) should hold just:

  ```
  data source=bowlneba-eastus.database.windows.net;initial catalog=neba;persist security info=True;user id={0};password={1};MultipleActiveResultSets=True
  ```

  (`App=EntityFramework` dropped — that's EF6 identifying itself to SQL Server for diagnostics, meaningless from a non-EF Dapper connection. Fine to leave it in if there's a reason to keep the two systems showing the same `App` name in SQL Server's connection telemetry, but it's not required.)

### Tests

- `LegacyApiKeyFilterTests` (unit) — constructs the filter directly, asserts 401 on missing/wrong key, passthrough on correct key.
- No integration test yet — nothing end-to-end to exercise until the first real action (`NewBowler`) lands.

### Explicitly out of scope

- Any actual `/legacy/*` action route.
- Hangfire job classes for individual actions.
- `LegacyId`/`SoftwareId` columns on website aggregates.

## Phase 2: UI

Not applicable — this is a machine-to-machine bridge with no user-facing surface.
