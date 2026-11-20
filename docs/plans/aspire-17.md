# Aspire 17 upgrade and `aspire deploy` move

Status: Part A merged (#264). Part B in progress on branch `aspire-deploy`: B0 to B4 and the staging run are done (see the result sections). Production cutover not started.
Audience: agents that pick up this work. Read the whole file before you start.

Two parts, two branches:

- **Part A** (branch `aspire17`): move the repo to Aspire 17 so it looks as if we built it on 17 from the start. Ships as one PR.
- **Part B** (new branch, cut from `aspire17` after Part A merges or stacks): replace `azd` with `aspire deploy`. Kept apart so the user can delete the branch if it fails.

Do not start Part B work on the `aspire17` branch. Cut the Part B branch from `aspire17`.

## Sources

- Reference app: `~/Projects/Aspire17-POC` (outside the repo). A starter app the user made with the 17 templates. Copy its shape.
- Docs: use `aspire docs search "<term>"` then `aspire docs get <slug>`. Key slugs:
  `migrate-to-the-new-dotnetprojectresource`, `compiler-warning-aspire012`, `compiler-error-aspire013`,
  `whats-new-in-aspire-170`, `upgrade-aspire-to-the-latest-version`, `deploy-to-azure`,
  `deploy-to-azure-container-apps`, `use-existing-azd-workflows`, `deployment-state-caching`,
  `azure-container-app-jobs`, `set-up-azure-key-vault-in-the-apphost`, `pipelines-and-app-topology`, `aspire-do-command`.
- The installed CLI is 17.0.0. Repo packages are now 17.x (Part A).

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

### Part A result (done)

Commits on `aspire17`:

1. Move the AppHost to Aspire 17 with path-based `AddDotnetProject`.
2. Add an `https` launch profile on `bowlneba.dev.localhost` (https 17107, http 15107).
3. CI reads the Aspire CLI version through `.github/scripts/aspire-version.sh`, which takes `Aspire.Hosting.AppHost` from `Directory.Packages.props`. `cd.yml`, `hotfix.yml` and `infrastructure_preview.yml` call it. It has not yet run in CI.
4. Enable the `aspire-skills` plugin. Removed `.claude/skills/aspire`, `.github/skills/aspire` and `.aspire/settings.json`.

Differences from the plan above:

- `Aspire.Hosting.Browsers` is `17.0.0-preview.1.26567.5` (the only 17 build). `CommunityToolkit.Aspire.Hosting.MailPit` is `17.0.0-beta.1078` (only betas exist for 17).
- `aspire update` added `sdk.version` to `aspire.config.json`. It was removed, because the AppHost no longer uses the SDK.
- Both `ASPIREBROWSERLOGS001` and `ASPIREACADOMAINS001` pragmas were removed: the build is clean without them on 17.
- `<TargetFramework>` is not in the AppHost csproj. `Directory.Build.props` already sets it.
- `.claude/agents/aspire.md` no longer preloads the `aspire` skill. It holds the repo rules itself (never `aspire run`, `--isolated` in worktrees, no early persistent containers) and loads the plugin skill for the command reference.
- `<Version>` stays 1.3.0 (user decision). Before the PR, confirm `main` is below 1.3.0. The PR title needs the `[v1.3.0]` prefix.

Verification: solution builds with 0 warnings. `aspire start` brought up every resource healthy. 5,769 unit and 1,315 integration tests pass. The user confirmed a normal local start on the real `bowlneba-pgdata` volume. E2E: 1,223 passed; 4 mobile-safari password-page specs failed once and passed on re-run (flaky, not tied to Aspire).

Publish check (`aspire publish -o <dir> --non-interactive`) works. Compared with `main`, resource names are identical. Differences: storage Bicep API version `2024-01-01` to `2026-06-01` (properties reordered), and a new `env` output `AZURE_CONTAINER_APPS_ENVIRONMENT_LOCATION`. Env vars needed for it: `AZURE_POSTGRES_SKU_NAME`, `AZURE_POSTGRES_SKU_TIER`, `AZURE_PRINCIPAL_ID` (any GUID), `AZURE_CONTAINERAPP_MIN_REPLICAS`, `AZURE_CONTAINERAPP_MAX_REPLICAS`, `AZURE_CONTAINERAPP_CONCURRENT_REQUESTS`, `AZURE_WEB_CUSTOM_DOMAIN`, `AZURE_API_CUSTOM_DOMAIN`, optionally `AZURE_WEB_WWW_CUSTOM_DOMAIN`.

Pipeline step names seen in the publish output (a start for B0; `aspire deploy --list-steps` is still needed for the deploy steps): `process-parameters`, `publish-prereq`, `publish-azure-environment`, `publish`, `azure-prepare-resources`, `validate-azure-container-apps`, `prepare-azure-container-apps-env`, `before-start`, `pipeline-execution`, `validate-build-only-container-references`.

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

### B0 result (done, 2026-11-19)

`aspire deploy` has no preview or dry-run flag. Use `aspire publish` plus `az deployment group what-if` instead (see B1a).

Deploy steps (`aspire deploy --list-steps`, 22 in all): `process-parameters`, `build-prereq`, `validate-build-only-container-references`, `deploy-prereq`, `build-api`, `build-api-migrations`, `build-api-security-migrations`, `build-web`, `validate-azure-login`, `create-provisioning-context`, `provision-logs`, `provision-appinsights`, `provision-keyvault`, `provision-dataprotectionkey`, `provision-env-acr`, `provision-env`, `provision-maps`, `provision-postgres`, `provision-storage`, `provision-azure-bicep-resources`, `print-dashboard-url-env`, `deploy`.

`provision-<resource>` steps exist per resource, so a single resource can be provisioned alone. The `deploy` step depends on all builds and on `provision-azure-bicep-resources`. Container pushes and app updates happen inside `deploy`. Open: the per-app deploy step names (for hotfix) and whether the migration jobs can be hooked between provision and deploy.

### B1a. What-if against production (done for storage and Postgres)

Prod is in subscription `bowlneba`, resource group `rg-bowlneba-prod`. Method: `aspire publish -o <dir>` with the repo's GitHub variables as env vars, then `az deployment group what-if -g rg-bowlneba-prod -f <module>.bicep`. Read-only.

- Resource names already match prod (`storagej7jlg5nmk67ym`, `postgres-j7jlg5nmk67ym`). Both use `uniqueString(resourceGroup().id)`, so `WithAzdResourceNaming()` is likely not needed. Confirm on the env module before dropping it from B2.
- Storage: **NoChange**.
- Postgres: same server, modified in place. Only real change: `authConfig.passwordAuth` is `Enabled` live and `Disabled` in the template. The other "Delete" lines are what-if noise (server-set defaults absent from the template). Data is not at risk (no recreate), but the live setting differs from the template, and the current azd flow would apply the same change. Find out who enabled it before any prod deploy.
- User decision: the Postgres `passwordAuth` was enabled by hand for LINQPad scripts. A deploy turning it off is fine. The user re-enables it by hand when needed.

Full-stack result (`az deployment sub what-if` on the generated `main.bicep`, all infra modules): **no resource is created or deleted.** Every resource keeps its prod name (Postgres, storage, Key Vault, registry, environment, identities, Maps, Log Analytics, App Insights). Changes found:

- Postgres `passwordAuth` `Enabled` to `Disabled` (accepted above).
- App Insights gains `Flow_Type` and `Request_Source` (harmless).
- Resource group loses the `azd-env-name` tag (the template declares the group with no tags). Matters only while the azd workflows still look the group up by that tag. Part B uses an explicit group name.
- Everything else is what-if noise: server-set defaults (retention, isolation scope, mTLS, encryption flags) and role assignments that what-if cannot evaluate. Role assignment names are fixed GUIDs, so they update in place.
- Not checked: the api, web and migration job modules (they need image names). Their resource names are fixed (`api`, `web`, `api-migrations`, `api-security-migrations`) and match prod.

Why the names match: the template names use `uniqueString(resourceGroup().id)`. `aspire deploy` deploys into the group named by `Azure__ResourceGroup`. With `rg-bowlneba-prod` the hash is the same.

**Go/no-go (2026-11-19):** GO to implement B4 steps 1 to 5 on the Part B branch (parameters, secrets, migration step, staging). NO-GO to run `aspire deploy` against prod until staging proves the pipeline. The data resources are safe. What-if cannot test the deploy pipeline itself, the migration step, secret seeding, or the OIDC login window.

### B4 steps 1 and 2 result (done, uncommitted on branch `aspire-deploy`)

`AppHost.cs` no longer reads `AZURE_*` environment variables. Publish mode now uses `AddParameter`, fed by `Parameters__<name>` variables:

| Parameter | Old variable | Notes |
| --- | --- | --- |
| `postgresSkuName`, `postgresSkuTier` | `AZURE_POSTGRES_SKU_NAME`, `_TIER` | Bicep params, no longer baked into the template |
| `deployPrincipalId` | `AZURE_PRINCIPAL_ID` | Keeps the Key Vault Secrets Officer role for the deploy principal |
| `minReplicas`, `maxReplicas`, `concurrentRequests` | `AZURE_CONTAINERAPP_*` | Strings in Aspire; Bicep converts with `int()` |
| `webCustomDomain`, `apiCustomDomain` | `AZURE_WEB_CUSTOM_DOMAIN`, `AZURE_API_CUSTOM_DOMAIN` | Required |
| `webCertificateName`, `apiCertificateName` | `AZURE_*_CERTIFICATE_NAME` | Optional |
| `webWwwCustomDomain`, `webWwwCertificateName` | `AZURE_WEB_WWW_*` | Declared only if `Parameters__webWwwCustomDomain` is set; certificate optional |

Checks: `aspire publish` with the new variables gives the same Bicep as before, except that the values are now Bicep parameters. The role assignment name hashes to the same value. With the optional values unset, publish succeeds and the certificate binding falls back to `Disabled`.

Not tested: a deploy with a required value missing. `aspire publish` does not fail on a missing value (Bicep takes it at deploy time), so check `aspire deploy --non-interactive` in staging.

GitHub changes: none now. The old `AZURE_*` repo variables stay as they are (release/v1.3.0 still uses them). The new workflows map each old variable to its `Parameters__<name>` on the deploy step (step 7). Staging will need its own values (domains, SKU, certificate names), so it needs its own GitHub environment.

### B4 step 3 result (done)

The ten secrets from the `cd.yml` seed step are now `keyVault.AddSecret(...)` calls in `AppHost.cs`, each fed by a `secret: true` parameter. Secret names are unchanged. Aspire resource names forbid `--`, so the code uses the `AddSecret(name, secretName, parameter)` overload.

| Key Vault secret | Parameter | GitHub secret |
| --- | --- | --- |
| `Google--Credentials--PrivateKey` | `googlePrivateKey` | `GOOGLE_PRIVATE_KEY` |
| `Google--Credentials--PrivateKeyId` | `googlePrivateKeyId` | `GOOGLE_PRIVATE_KEY_ID` |
| `Google--Credentials--ClientEmail` | `googleClientEmail` | `GOOGLE_CLIENT_EMAIL` |
| `EmailSettings--AppPassword` | `emailAppPassword` | `GOOGLE_PASSWORD` |
| `JwtSettings--SigningKey` | `jwtSigningKey` | `JWT_SIGNING_KEY` |
| `Legacy--ApiKey` | `legacyApiKey` | `LEGACY_API_KEY` |
| `Legacy--ConnectionString` | `legacyConnectionString` | `LEGACY_DB_CONNECTION_STRING` |
| `Discord--ApiLogsWebhookUrl` | `discordApiLogsWebhookUrl` | `DISCORD_API_LOGS_WEBHOOK_URL` |
| `Discord--ManualInterventionWebhookUrl` | `discordManualInterventionWebhookUrl` | `DISCORD_MANUAL_INTERVENTION_WEBHOOK_URL` |
| `Discord--NewsWebhookUrl` | `discordNewsWebhookUrl` | `DISCORD_NEWS_WEBHOOK_URL` |

Checks: the generated `keyvault.bicep` takes each value as a `@secure()` parameter and holds no secret value. A what-if with dummy values against prod found all ten names already in the vault, so a deploy writes a new version of each in place. The required-parameter rule replaces the shell check for blank Discord URLs.

Notes:

- No GitHub secret changes. The new workflow maps each GitHub secret to `Parameters__<parameter>` on the deploy step. The `cd.yml` seed step stays until release/v1.3.0 reaches main.
- Staging needs a value for all ten (dummy values work), because every one is required.
- The vault also holds `Discord--WebhookUrl` and `AzureMaps--SubscriptionKey`. The first is stale and unmanaged (delete by hand when convenient). The second comes from `maps.bicep` and is untouched.
- The `deploy_pipeline` Key Vault role stays for now. Remove it only after staging shows `aspire deploy` needs no data-plane access (the old azd flow does).

### B4 step 4 result (code done, not yet run)

New file `src/Neba.AppHost/MigrationPipelineStep.cs` and a `run-migrations` step in `AppHost.cs`. It is a port of the shell loop in `cd.yml`: it runs `az containerapp job start` for `api-migrations` and `api-security-migrations`, polls every 10 seconds for up to 10 minutes, and fails the deploy unless each ends `Succeeded`.

- **Order:** a pipeline configuration callback makes `run-migrations` depend on the two jobs' `deploy-compute` steps, and makes the api and web `deploy-compute` steps depend on it. Wiring is by resource and tag, not by step name, because those steps are created late and `--list-steps` does not show them. The same step also feeds `deploy` (it shows in `aspire deploy --list-steps`).
- **Opt-in:** the step does nothing unless `Migrations__Run=true`. Any run that deploys api or web passes through it (hotfix, a retry of one app), and only the full CD run may touch the schema. The new CD workflow sets the flag. The hotfix workflow never does.
- **Needs from CI:** `Azure__ResourceGroup` (required, the step fails without it), optional `Azure__SubscriptionId`, an `az` login that stays valid for the run, and `az` on the PATH.
- **Verified:** it builds, `aspire publish` still works, the step is in the deploy list.
- **Not verified:** the by-tag wiring and the job runs. List mode skips the configuration callback. First real check is the staging run: confirm in its output that `run-migrations` ran after the job deploys and before api and web.
- **Open:** whether the OIDC login lasts through a full `aspire deploy` plus up to 10 minutes of polling.

### B1 staging setup (workflow written, not run)

Workflow: `.github/workflows/staging.yml` (manual dispatch, GitHub environment `Staging`, job group `staging-deploy`). It checks settings, logs in, installs the CLI, runs `aspire deploy --environment staging --non-interactive`, then lists the resources it created.

Defaults:

- Resource group `rg-bowlneba-staging`, same subscription as production, created by the deploy. The workflow refuses any group name without `staging` in it.
- No custom domains. The AppHost now declares a domain only when its parameter has a value, so staging binds no hostnames. With all domain parameters set (production) the generated Bicep is byte-identical to before.
- Replicas 0 to 1, concurrency 50. Postgres `Standard_B1ms` / `Burstable` (same as prod).
- Test secrets in the Staging environment (done by the user, all 11 present).

Setup the user must do before the first run:

1. **OIDC.** Add a federated credential on the deploy app registration (`AZURE_CLIENT_ID`) with subject `repo:bowlneba/neba-website:environment:Staging`. Without it `azure/login` fails.
2. **Staging environment variables** (the Staging environment has none today):
   - New: `AZURE_RESOURCE_GROUP` = `rg-bowlneba-staging`.
   - Needed because a missing one falls back to the repo-level or Production value, or fails the check step: `AZURE_PRINCIPAL_ID` (same value as production), `AZURE_CONTAINERAPP_MIN_REPLICAS` = `0`, `AZURE_CONTAINERAPP_MAX_REPLICAS` = `1`.
   - Inherited from repo level if set there (confirm each resolves): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_LOCATION`, `AZURE_POSTGRES_SKU_NAME`, `AZURE_POSTGRES_SKU_TIER`, `AZURE_CONTAINERAPP_CONCURRENT_REQUESTS`.
3. Production's role for the deploy principal must allow creating a resource group and role assignments at subscription scope (it has Contributor and User Access Administrator today).

Local note: `aspire publish` caches parameter values per environment in `~/.aspire/deployments`. To test a no-domain publish on a machine that has run production values, pass `--environment staging`. Fresh CI runners have no cache.

### Staging run result (2026-11-19, run 44609141202, success)

Third run, `westus2`, empty database: **18 min 14 s** from start to finish. The first two runs failed for fixable reasons:

1. Azure login: the federated credential used the ID-based subject (`repo:bowlneba@<id>/neba-website@<id>:environment:Staging`). The workflow presents the name-based subject `repo:bowlneba/neba-website:environment:Staging`. Use the name-based form.
2. `run-migrations` started at second zero, with no dependencies (the by-tag wiring found nothing). Fixed by explicit step names, registered only when `Migrations__Run=true`.
3. Postgres B1ms returned `SkuNotAvailable` in `eastus2` for a new server, although the CLI lists it as available. Staging moved to `westus2`. Prod's server already exists in `eastus2`, so a deploy only updates it. A recreate of prod's Postgres in `eastus2` could hit the same limit.

What the third run proved:

- **Step names.** The names `provision-<name>-containerapp` are right for the two jobs and for api and web. Full order: provision infra, push images, `provision-env`, the two job `provision-*-containerapp` steps, `run-migrations` (2 min 3 s), then `provision-api-containerapp` and `provision-web-containerapp` in parallel.
- **Migration step.** Both jobs ran to `Succeeded` before api and web deployed. The step needs only `az` and `Azure__ResourceGroup`.
- **Secrets.** All ten secrets and `AzureMaps--SubscriptionKey` are in the vault (listed through ARM). Seeding through `AddSecret` works.
- **Postgres.** `Standard_B1ms`, password auth disabled, state Ready.
- **Login lifetime.** One `az` login covered an 18-minute run, with `az` calls 16 minutes in. No re-login steps are needed inside one `aspire deploy`.
- **Parameters.** All parameters resolved from `Parameters__*` variables. Empty custom domains were skipped.
- **Names.** Same resource types as prod. Staging has no managed certificates, no DNS zone, and no Smart Detection action group, which prod holds from manual or automatic setup.
- **Timing.** Postgres took 9 min 49 s and the container environment 12 min 4 s, in parallel. These two set the run time.

Hotfix test (run 44613719760, success, 5 min 53 s): `aspire do provision-api-containerapp`, then `aspire do provision-web-containerapp`, `Migrations__Run` empty, on top of the existing staging deploy.

- No `run-migrations` step and no migration job step or image appeared. The workflow checks the log for this.
- Both apps updated in place. Api took 4 min 37 s (it also ran its build and push, and the dependency steps), web about 1 min 20 s.
- Each `aspire do` pulls in the infra provisioning steps the app depends on (storage, Key Vault, Postgres, environment, role assignments) and applies their Bicep again. They are idempotent and quick when nothing changed, but a hotfix therefore re-applies the infra template on its branch, which azd's per-service deploy did not. Safe, because a hotfix branch is cut from the release tag and carries that release's Bicep.
- `aspire do` takes one step per call, so a hotfix runs it twice (api, then web).

Open after staging:

- A deploy with a required parameter missing: not tested.
- The `deploy_pipeline` Key Vault role is not needed: secrets go in through ARM, not the data plane. Remove it, and the `deployPrincipalId` parameter, at cutover. Prod keeps its existing role assignment (Bicep does not delete it).
- Staging still exists and costs a little. Keep it until the hotfix path is tested, then delete `rg-bowlneba-staging` and purge its Key Vault.

### B1. Staging deploy (do this before touching production)

Lower priority now. Production's data (Postgres, storage) is covered by B1a. Staging is for the pipeline, migration step, and Key Vault secrets, which what-if cannot show.

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

- Part A: none. Push and PR are the user's call.
- Part B: real pipeline step names (B0), and whether `aspire deploy` can run non-interactively with only env vars.
