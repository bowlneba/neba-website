# Aspire deploy cutover checklist

One-time list for the pull request that moves production from `azd` to `aspire deploy` (release/v1.3.0 into `main`). The PR review must show these steps to the author, under a "Post-merge actions" heading, because they happen in GitHub and Azure settings, not in the diff. Delete this file once every box is ticked.

The old workflows (`azd`, `AZURE_*` read by the AppHost) stay valid on branches that do not contain this change. The new workflows need the settings below.

## Before the merge

- [ ] **Repository variable `AZURE_RESOURCE_GROUP` = `rg-bowlneba-prod`.** `infrastructure_preview.yml` runs with no GitHub environment, so it reads repository variables only. The Production environment already has this variable; the repository one is new. The Staging environment keeps its own value (`rg-bowlneba-staging`), which overrides the repository one.
- [ ] **Keep these production values at repository level** (see "Variable cleanup" below). The PR what-if reads them from there.
- [ ] **Staging environment variables** (they override the repository values, so staging never inherits production's): `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION` (`westus2`), `AZURE_PRINCIPAL_ID`, `AZURE_POSTGRES_SKU_NAME`, `AZURE_POSTGRES_SKU_TIER`, `AZURE_CONTAINERAPP_MIN_REPLICAS`, `AZURE_CONTAINERAPP_MAX_REPLICAS`, `AZURE_CONTAINERAPP_CONCURRENT_REQUESTS`. Do not set any domain or certificate variable there. The Staging environment also needs the ten secrets (test values are fine).
- [ ] **Azure federated credentials** on the deploy app registration: subject `repo:bowlneba/neba-website:environment:Production`, `repo:bowlneba/neba-website:environment:Staging`, and the pull-request and branch subjects the preview already uses. Use the name-based subject, not the ID-based form the portal can offer.
- [ ] **Remove the temporary push trigger from `staging.yml`** (the `push:` block and the `MODE` default that exists for push runs). Without it the workflow is manual only.
- [ ] **Version bump.** `<Version>` in `Directory.Build.props` above `main`'s, and the `[vX.Y.Z]` title prefix.
- [ ] Production `<Version>` tag convention is unchanged: tag the deployed commit by hand after the deploy.

## Variable cleanup

`cd.yml` and `hotfix.yml` run in the Production environment, so they read a variable from there first and fall back to the repository one. `infrastructure_preview.yml` has no environment and reads repository variables only. So the repository level is the single source for production values, and the Production environment keeps only what is specific to it.

| Variable | Repository level | Production environment | Staging environment |
| --- | --- | --- | --- |
| `AZURE_RESOURCE_GROUP` | **Add** `rg-bowlneba-prod` | Keep | `rg-bowlneba-staging` |
| `AZURE_CLIENT_ID` | Keep | none | inherits |
| `AZURE_LOCATION` | Keep (`eastus2`) | none | `westus2` |
| `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | organization level, keep | none | inherits |
| `AZURE_PRINCIPAL_ID` | Keep | none | same value, set |
| `AZURE_POSTGRES_SKU_NAME`, `AZURE_POSTGRES_SKU_TIER` | Keep | none | set |
| `AZURE_CONTAINERAPP_MIN_REPLICAS` | Keep (`1`) | **Delete the copy** | `0` |
| `AZURE_CONTAINERAPP_MAX_REPLICAS` | Keep (`10`) | **Delete the copy** | `1` |
| `AZURE_CONTAINERAPP_CONCURRENT_REQUESTS` | Keep (`50`) | **Delete the copy** | `50` |
| `AZURE_API_CUSTOM_DOMAIN` | Keep | **Delete the copy** | never set |
| `AZURE_API_CERTIFICATE_NAME` | Keep | **Delete the copy** | never set |
| `AZURE_WEB_CUSTOM_DOMAIN` | Keep | **Delete the copy** | never set |
| `AZURE_WEB_CERTIFICATE_NAME` | Keep | **Delete the copy** | never set |
| `AZURE_WEB_WWW_CUSTOM_DOMAIN` | Keep | **Delete the copy** | never set |
| `AZURE_WEB_WWW_CERTIFICATE_NAME` | Keep | **Delete the copy** | never set |
| `AZURE_ENV_NAME` | **Delete** (only `azd` read it) | **Delete** | none |

"Keep" at repository level means do not delete it when you clean up the Production environment. The old `azd` workflows on other branches still read these.

## First production deploy

- [ ] The run waits for Production environment approval, then shows a what-if summary in the job summary. Read it: no create or delete of Postgres or storage (the step fails if there is one).
- [ ] Expect Postgres password login to turn off (it was enabled by hand for LINQPad). Turn it back on by hand when needed.
- [ ] Expect the `run-migrations` step between the job deploys and the api and web deploys, and the smoke test at the end.
- [ ] If the run fails partway, fix the cause and run it again. Provisioning and the migrations are safe to repeat.

## After the merge

- [ ] **Variable cleanup:** delete the Production-environment copies and `AZURE_ENV_NAME` as the table below says. First confirm each Production copy has the same value as the repository one. A copy that drifts would make the deploy and the PR what-if disagree.
- [ ] Optional: delete the unused GitHub secret `DISCORD_WEBHOOK_URL` and the stale Key Vault secret `Discord--WebhookUrl`.
- [ ] Delete the local, git-ignored `.azure/` folder and uninstall `azd` if nothing else uses it.
- [ ] Remove the `deploy_pipeline` Key Vault role and the `deployPrincipalId` parameter from the AppHost (secrets now go in through ARM, not the data plane). Then drop `AZURE_PRINCIPAL_ID` from the workflows and variables. The existing role assignment in production stays until you delete it by hand.
- [ ] Run **Destroy Staging** when you no longer need staging (`destroy-staging.yml`, type the group name), or leave it for the next pipeline change. It costs a little while it exists.
- [ ] `docs/adr/0001` and `0002` describe `azd up`. They are history; add a one-line "superseded by Aspire deploy" note if you want them to say so.
