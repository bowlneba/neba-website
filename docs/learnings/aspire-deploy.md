# Deploying with `aspire deploy`

Production deploys with the Aspire CLI (`aspire deploy`), not `azd`. The AppHost (`src/Neba.AppHost/AppHost.cs`) holds the whole topology; the workflows only supply values.

## Workflows

| Workflow | What it does |
| --- | --- |
| `cd.yml` | Production. What-if guard, then `aspire deploy`: provision, seed Key Vault, run the migration jobs, deploy api and web, smoke test. |
| `hotfix.yml` | Production, api and web only, no migrations. What-if guard, then `aspire do` for each app. See `docs/learnings/hotfix.md`. |
| `infrastructure_preview.yml` | Read-only what-if of production for a pull request that changes the AppHost. Posts a comment. Runs with no GitHub environment, so it reads repository variables only. |
| `staging.yml` | Deploys the whole stack to a separate resource group, in the production subscription. Use it to try a pipeline change before production. Modes: `full` and `hotfix`. |
| `destroy-staging.yml` | Deletes the staging resource group and purges both its Key Vaults. Manual; you type the group name to confirm. Refuses any group without `staging` in its name. |

`cd.yml`, `hotfix.yml` and `staging.yml` each hold a copy of the deploy settings. Change all three together. Try the change in `staging.yml` first.

## How values reach the AppHost

Every deploy-time value is an Aspire parameter, set by a `Parameters__<name>` environment variable. Workflows map GitHub variables and secrets to those names. Azure targets use `Azure__*` (`SubscriptionId`, `TenantId`, `Location`, `ResourceGroup`, `AllowResourceGroupCreation`).

| Parameter | GitHub source | Notes |
| --- | --- | --- |
| `postgresSkuName`, `postgresSkuTier` | `AZURE_POSTGRES_SKU_NAME`, `_TIER` | |
| `deployPrincipalId` | `AZURE_PRINCIPAL_ID` | Key Vault Secrets Officer role for the deploy principal on both vaults. Not needed by `aspire deploy` itself. |
| `minReplicas`, `maxReplicas`, `concurrentRequests` | `AZURE_CONTAINERAPP_*` | Strings; Bicep converts with `int()`. |
| `webCustomDomain`, `apiCustomDomain`, `webWwwCustomDomain` | `AZURE_WEB_CUSTOM_DOMAIN`, `AZURE_API_CUSTOM_DOMAIN`, `AZURE_WEB_WWW_CUSTOM_DOMAIN` | A domain is declared only when its value is set. Staging sets none. |
| `webCertificateName`, `apiCertificateName`, `webWwwCertificateName` | `AZURE_*_CERTIFICATE_NAME` | Optional. Empty binds the hostname without a certificate. |
| Ten secret parameters | GitHub secrets (Production or Staging environment) | See the table in the AppHost; Key Vault secret names are unchanged. |
| `bowlerPiiHashKey`, `legacySsnPassphrase` | `BOWLER_PII_HASH_KEY`, `LEGACY_SSN_PASSPHRASE` | Written to `piivault`, which only `api` references (ADR-0011). The hash key is 32 random bytes in Base64; staging has its own. Never change it once hashes are stored. |

A required parameter rejects an empty value, so a blank secret stops the deploy before it reaches Azure.

## The migration step

`run-migrations` (`MigrationPipelineStep.cs`) starts the two migration Container App Jobs and waits for both to succeed. It runs after the job images deploy and before api and web deploy. It exists only when `Migrations__Run=true`. `cd.yml` and the `full` mode of `staging.yml` set it. A hotfix never does, so the step is not in its pipeline. It needs `Azure__ResourceGroup`, and `az` logged in on the runner.

## What-if guard

`.github/scripts/whatif-guard.sh` runs `aspire publish` to get the Bicep, then `az deployment sub what-if`. It reads Azure and changes nothing. It fails if the deploy would create or delete Postgres or the storage account. Resource names hash the resource group, so a wrong group or renamed resource would otherwise create a new, empty database. A fresh staging environment passes `ALLOW_CREATE=true`. Property changes on existing resources are mostly values Azure sets itself; only create and delete of the protected resources fail.

## Things that bite

- `piivault` has purge protection in production (the `--environment production` publish), never in staging. Azure cannot turn it off again.
- `aspire do` takes one step per call. A hotfix calls it twice, and each call also runs the steps the app depends on (infrastructure provisioning is idempotent and quick when nothing changed).
- Deploy steps for apps and jobs are named `provision-<name>-containerapp`. They are created late, so `--list-steps` does not show them, and a wrong name fails at the start of a real run, not in list mode.
- `aspire publish` and `aspire deploy` cache parameter values per environment in `~/.aspire/deployments`. To test locally with different values, pass `--environment`. Fresh CI runners have no cache.
- Postgres `Standard_B1ms` was not available for a new server in `eastus2` for this subscription (it returned `SkuNotAvailable`), so staging runs in `westus2`. Production's server already exists in `eastus2`; recreating it there could hit the same limit.
- Deleting a resource group soft-deletes its Key Vaults and keeps the names reserved. `destroy-staging.yml` purges both (`keyvault` and `piivault`). The same names would otherwise block the next staging deploy.
- Postgres password login is off in the template. Turn it on by hand when you need a local tool, for example LINQPad. The next deploy turns it off again.
- GitHub cannot dispatch a workflow that exists only on a feature branch. `staging.yml` has a temporary push trigger for that reason (remove it once the workflow is on `main`).
- One `az` login covered an 18-minute full deploy, so no re-login steps are needed inside one `aspire deploy`.
