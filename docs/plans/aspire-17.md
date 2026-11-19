# Aspire 17 upgrade and `aspire deploy` move

Status: plan only. No code changed yet.
Audience: agents that pick up this work. Read the whole file before you start.

Two parts, two branches:

- **Part A** (branch `aspire17`): move the repo to Aspire 17 so it looks as if we built it on 17 from the start. Ships as one PR.
- **Part B** (new branch, cut from `aspire17` after Part A merges or stacks): replace `azd` with `aspire deploy`. Kept apart so the user can delete the branch if it fails.

Do not start Part B work on the `aspire17` branch.

## Sources

- Reference app: `~/Projects/Aspire17-POC` (outside the repo). A starter app the user made with the 17 templates. Copy its shape.
- Docs: use `aspire docs search "<term>"` then `aspire docs get <slug>`. Key slugs:
  `migrate-to-the-new-dotnetprojectresource`, `compiler-warning-aspire012`, `compiler-error-aspire013`,
  `whats-new-in-aspire-170`, `upgrade-aspire-to-the-latest-version`, `deploy-to-azure`,
  `deploy-to-azure-container-apps`, `use-existing-azd-workflows`, `deployment-state-caching`,
  `azure-container-app-jobs`, `set-up-azure-key-vault-in-the-apphost`, `pipelines-and-app-topology`, `aspire-do-command`.
- The installed CLI is already 17.0.0. Repo packages are still 13.6.5.

## Decisions the user made

1. Enable the `aspire-skills` plugin (as the POC does). Keep repo-specific rules in our agent files.
2. Local URL host becomes `bowlneba.dev.localhost`.
3. CI reads the Aspire version from `Aspire.Hosting.AppHost` in `Directory.Packages.props`.
4. One PR for Part A.
5. The user reviews workflow edits before any push. Never push, dispatch a workflow, or open a PR without asking.
6. Part B goes on its own branch.
7. For Part B, first build a **staging** deploy job in the same Azure subscription that does what production does. Use it to see what `aspire deploy` does before it touches production.

## What the POC does differently

| Area | Repo today | POC (native 17) |
| --- | --- | --- |
| AppHost SDK | `Aspire.AppHost.Sdk/13.6.5` (ASPIRE012 warning on 17) | `Microsoft.NET.Sdk` plus `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` packages |
| Add a project | `AddProject<Projects.Neba_Api>("api")` (obsolete, CS0618) | `AddDotnetProject("api", "../Neba.Api/Neba.Api.csproj")` |
| AppHost `ProjectReference` | Api, MigrationService, Website.Server | none |
| `AspireUseCliBundle` | `true` | absent (bundle is now required) |
| Config files | `aspire.config.json` and legacy `.aspire/settings.json` | `aspire.config.json` only |
| Agent setup | hand-written `aspire` skill | `aspire-skills` plugin in `.claude/settings.json` and `.github/copilot/settings.json` |
| Launch URL | `http://localhost:15107` only | `https` and `http` profiles on `*.dev.localhost` |

---

## Part A: Aspire 17 (branch `aspire17`)

Work in this order. Build after each step.

### A1. Update packages

1. `aspire update --self`, then `aspire update` from the repo root.
2. Confirm `Directory.Packages.props` moved every `Aspire.*` package to 17.x. Check these by hand:
   - `Aspire.Hosting.Browsers` (was `13.6.5-preview.*`): use the 17 version, stable if one exists.
   - `CommunityToolkit.Aspire.Hosting.MailPit` (was 13.6.1, community package): find a version that works with 17. If none exists, stop and ask the user.
   - `Aspire.Hosting.Testing`.
3. Add `PackageVersion` items for `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`. The repo uses central package management, so project files must not carry `Version=` (restore fails with NU1008).

### A2. Convert `src/Neba.AppHost/Neba.AppHost.csproj`

- `Sdk="Aspire.AppHost.Sdk/13.6.5"` becomes `Sdk="Microsoft.NET.Sdk"`.
- Add `<TargetFramework>` (the old SDK likely set it; match the other projects, net10.0). Keep `OutputType` and `UserSecretsId`.
- Remove `AspireUseCliBundle` (ASPIRE013 if it is anything but `true`).
- Add `PackageReference` for `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`.
- Remove the three `ProjectReference` items (Api, MigrationService, Website.Server). The path-based API does not need them.
- Keep `MessagePack` and the other existing package references.
- Check that no other repo property relied on the old SDK (`Directory.Build.props`, analyzers, `LangVersion`).

### A3. Convert `src/Neba.AppHost/AppHost.cs`

- Replace the four `AddProject<Projects.X>` calls with `AddDotnetProject(name, path)`:
  - `api-migrations` and `api-security-migrations` both point at `../Neba.MigrationService/Neba.MigrationService.csproj`.
  - `api` points at `../Neba.Api/Neba.Api.csproj`.
  - `web` points at `../Neba.Website.Server/Neba.Website.Server.csproj`.
  - Keep every resource name unchanged. Names feed Azure resource names and the CD workflow.
- Add `using Aspire.Hosting.Dotnet;` if the compiler asks for it.
- Re-check each chained call on `IResourceBuilder<DotnetProjectResource>`:
  `WithReference`, `WaitFor`, `WaitForCompletion`, `WithHttpHealthCheck`, `WithExternalHttpEndpoints`, `WithRoleAssignments`, `WithUrlForEndpoint`, `WithUrls`, `WithBrowserLogs`, `WithArgs`, `PublishAsAzureContainerApp`, `PublishAsAzureContainerAppJob`.
  If one no longer compiles, read the docs. Do not drop the behavior.
- Launch profile: the old call used the default profile. Omit the options callback to keep that.
- Remove pragmas for diagnostics 17 made stable. Keep `ASPIREBROWSERLOGS001` (`WithBrowserLogs` is still experimental). Check `ASPIREACADOMAINS001` after the build.
- Rebuilding is not enough to prove publish works. See A9.

### A4. Test projects

- `tests/Neba.TestFactory/Infrastructure/AspireFixture.cs` uses `DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>()`. The docs say keep the AppHost reference for this. Confirm `Projects.Neba_AppHost` still generates.
- Search the tests for any `Projects.Neba_Api` or similar use. If found, mark the AppHost reference per the migration guide
  (`IsAspireProjectResource="true" ReferenceOutputAssembly="false"`) or change the test.
- Update the sample in `docs/architecture/backend.md` (around line 2197) only if the code changes.

### A5. Launch settings (`src/Neba.AppHost/Properties/launchSettings.json`)

- Add an `https` profile and keep `http`, like the POC.
- `applicationUrl` host: `bowlneba.dev.localhost`. Keep `15107` for http; pick a free port for https (POC pattern is `https://aspire17.dev.localhost:17091;http://aspire17.dev.localhost:15294`).
- Keep `ASPIRE_ALLOW_UNSECURED_TRANSPORT` on the `http` profile.
- Before the change, search the repo for hardcoded `localhost:15107`. At the time of writing only `launchSettings.json` has it. Check e2e config and docs again.

### A6. CI: read the version from `Directory.Packages.props`

Three workflows grep `Aspire.AppHost.Sdk/<version>` out of the AppHost csproj. After A2 that line is gone and the step fails:

- `.github/workflows/cd.yml` (line ~62)
- `.github/workflows/hotfix.yml` (line ~211)
- `.github/workflows/infrastructure_preview.yml` (line ~56)

Change each to read the `Aspire.Hosting.AppHost` `PackageVersion` from `Directory.Packages.props`. Use one small shared script (for example `.github/scripts/aspire-version.sh`) so the three copies do not drift. Do not hardcode the version in YAML (the user's standing rule). The user reviews these edits before any push.

`azd` stays in these workflows in Part A. Part B changes that.

### A7. Config and tooling cleanup

- Delete `.aspire/settings.json` (legacy `appHostPath`) if nothing reads it. `aspire.config.json` covers it.
- Add `.claude/settings.json` and `.github/copilot/settings.json` entries from the POC (`aspire-skills` marketplace, `aspire@aspire-skills` enabled). Merge, do not overwrite, existing settings.
- Run `aspire agent init` or copy the POC layout. Then decide what to do with `.claude/skills/aspire/SKILL.md`: the plugin replaces it, so remove it and keep `.claude/agents/aspire.md` (repo-specific rules).
- Fix stale text: `.claude/agents/aspire.md` (lines ~52, ~67) and `.claude/agents/devops.md` (~38) say to match the `Aspire.AppHost.Sdk` version. Say `Aspire.Hosting.AppHost` in `Directory.Packages.props`.
- Update `CLAUDE.md` or `docs/` if they mention the SDK. Keep `CLAUDE.md` short.
- Update the memory note `feedback_aspire_cli_version_hardcode.md` to name the new source.

### A8. Version bump

PRs to `main` must raise `<Version>` in `Directory.Build.props` (now 1.3.0) above `main`'s. PR title gets the `[vX.Y.Z]` prefix. The user picks the number.

### A9. Verify

1. Solution builds with no CS0618, ASPIRE012, or ASPIRE013 warnings.
2. `aspire start`, then `aspire describe`: postgres, api-migrations, api-security-migrations, storage, api, web, mailpit all healthy. Postgres image moves 18.3 to 18.6 with the persistent `bowlneba-pgdata` volume: confirm it starts.
3. `dotnet test --filter-trait "Category=Unit"` and `Category=Integration` pass (see CLAUDE.md for the MTP exit-code note).
4. Publish check: `aspire publish` (or `aspire do` with `--list-steps`) generates Bicep for publish mode without errors. Set the required `AZURE_*` env vars first (see `AppHost.cs`). Compare generated Bicep with a build from `main` and report differences.
5. E2E: `npm run test:e2e`.

### Part A risks

- `aspire run --watch` does not work for the new resource model. We use `aspire start`, so this should not matter.
- MailPit and Browsers package versions (A1).
- Docs claim `aspire update` never rewrites `AddProject` calls or the SDK, so all of A2 and A3 is manual.

---

## Part B: `aspire deploy` instead of `azd` (separate branch)

Goal: deploy with the Aspire CLI, as natively as possible. `azd` is still supported but no longer the recommended path.

### B0. First, learn the steps

On the Part B branch, before design work, run in the AppHost folder (with publish-mode env vars set):
`aspire deploy --list-steps` and `aspire do <step> --list-steps`. Record the real step names in this file. Everything below that mentions steps depends on them.

### B1. Staging deploy (do this before touching production)

- Add a **staging** Aspire environment and a workflow (or a manual-dispatch job) that deploys to the **same subscription**, in its own resource group, doing everything production does: provision, Key Vault, migration jobs, api, web.
- Use `aspire deploy --environment staging`. Resource group via `Azure__ResourceGroup`; also set `Azure__SubscriptionId`, `Azure__TenantId`, `Azure__Location`.
- Staging needs its own custom domains or none (leave `AZURE_*_CUSTOM_DOMAIN` unset or use test names). Postgres SKU can be small.
- Read what it creates. Compare resource names and types with production. Only after staging looks right do we plan the production cutover.
- Production safety check: compare staging's generated resource names with production's current names. Names come from the generated Bicep, so a match proves a production deploy would update in place instead of creating copies.

### B2. Map each piece

| Today (azd) | With `aspire deploy` |
| --- | --- |
| `azd provision`, then `azd deploy <svc>` | `aspire deploy --environment <env>` |
| `azd provision --preview` (infra preview job) | No Aspire equivalent found. **Recommendation:** the staging deploy is the preview. Keep the existing `azd provision --preview` job until Part B reaches production, then drop `infrastructure_preview.yml` and the preview gate in `cd.yml`. Do not build an `az what-if` replacement unless the user asks. |
| azd env state (`azd env new/select`, `.azure/`) | Pass everything as env vars. Do not rely on `~/.aspire/deployments` cache on CI. If a run would prompt, use `--non-interactive` so it fails loudly. |
| Resource group by tag `azd-env-name` | Explicit `Azure__ResourceGroup` |
| Resource names | `AddAzureContainerAppEnvironment("env").WithAzdResourceNaming()` so existing resources are reused, not duplicated. Critical for production. |
| `Environment.GetEnvironmentVariable("AZURE_*")` in `AppHost.cs` | `AddParameter(...)` fed by `Parameters__*` env vars. SKU, replicas, concurrency, domains, certificate names, principal id all become parameters. Required parameters now reject empty values: use `.AsOptional()` for the www domain and certificate names. |
| Key Vault seeding (10 `az keyvault secret set` calls in `cd.yml`) | `keyVault.AddSecret("<name>", secretParameter)` where `secretParameter = builder.AddParameter("<name>", secret: true)`. Secrets still come from **GitHub secrets**: the workflow maps each GitHub secret to a `Parameters__<name>` env var on the deploy step. Same source, new path. Secret names stay as today (`Google--Credentials--PrivateKey`, `Legacy--ConnectionString`, and so on). Side effect: rotating a secret now needs a deploy run. The deploy-principal Key Vault role workaround in `AppHost.cs` may no longer be needed; verify before removing it. |
| Migration jobs: `azd deploy`, `az containerapp job start`, poll | Keep `PublishAsAzureContainerAppJob`. **Recommendation:** a custom pipeline step in the AppHost (`builder.Pipeline.AddStep`) that starts each job, polls to success, and fails the deploy otherwise. It must depend on the jobs' deploy steps and block the api and web deploy steps. This keeps it inside Aspire. Needs unit-level care: it runs production migrations. If the step API blocks this, fall back to the existing shell loop in the workflow. |
| Hotfix (api and web only, no migrations) | `aspire do <step>` for just those steps. Needs B0. Hotfixes must still never run migrations. |
| `azure/login` OIDC with re-login steps | Keep. Long runs expire the OIDC assertion (~5 min); keep re-login before long steps, or confirm one `aspire deploy` run finishes fast enough. |
| `azure.yaml`, `.azure/` | Delete after production runs on `aspire deploy`. |

### B3. Keep as-is

- `dataprotectionkey.bicep` and `maps.bicep`: no typed Key Vault key resource exists, so these stay as hand-written Bicep templates.
- `Neba.MigrationService` (two contexts, managed-identity auth). `AddEFMigrations` with a bundle is a possible later change, not part of this work.

### B4. Order of work on the Part B branch

1. B0 step discovery.
2. Parameters: move `GetEnvironmentVariable` reads to `AddParameter`. Add `.WithAzdResourceNaming()`.
3. Key Vault secrets via `AddSecret`.
4. Migration pipeline step.
5. Staging workflow (B1). Run it. Review the result with the user.
6. Compare staging names with production. Decide production cutover with the user.
7. Rewrite `cd.yml`, `hotfix.yml`. Remove `azd` steps, `azure.yaml`, `.azure/`, the preview job (per B2).
8. Update `.claude/agents/devops.md`, `docs/learnings/hotfix.md`, `CLAUDE.md` hotfix lines (they say `hotfix.yml` and `cd.yml` hold copies of the deploy steps).

### Part B risks

- **Production resource churn.** A naming mismatch could recreate Postgres. Never run `aspire deploy` against production before staging proves the names match.
- **No infra preview.** See B2.
- **Fresh runners.** No cached state: every value must come from env vars.
- **Custom migration step** is new code on the production path.
- **Experimental APIs.** Check each pipeline API's stability in 17 before relying on it.

## Open items

- Part A: the right `CommunityToolkit.Aspire.Hosting.MailPit` version for 17 (A1).
- Part B: real pipeline step names (B0), and whether `aspire deploy` can run non-interactively with only env vars.
