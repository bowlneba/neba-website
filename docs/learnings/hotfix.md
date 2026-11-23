# Hotfix deploys

Use this when a fix must reach production faster than the normal pipeline allows (for example, mid-tournament). Roughly 15 min of tests plus 5–8 min of deploy.

## Try these first

1. **Feature flag or config change.** If the broken path sits behind a flag, or an env var or Key Vault value fixes it, change that. No deploy.
2. **Roll back.** Container Apps run in single-revision mode, so there is no instant traffic switch. To roll back, revert the bad commit on a hotfix branch and deploy it (see "Roll back" below).

## Flow

1. Cut the branch from the latest release tag (the commit production runs). You tag by hand after each deploy, so keep that current:

   ```bash
   git fetch origin --tags
   git switch -c hotfix/2026-10-12-short-slug "$(git tag --list 'v*' --sort=-v:refname | head -1)"
   ```

2. Commit the fix, then run `/hotfix`. It reviews the commits against the tag, checks tests exist and fail without the fix, runs the affected tests, and writes the PR description and bug report. It prints the push, dispatch, and PR commands but runs none of them.
   Then push the branch. Pushing does not start any workflow.
3. Run **Hotfix Workflow** from Actions on that branch (`gh workflow run hotfix.yml --ref hotfix/<name>`). It:
   - checks the branch is `hotfix/*`, descends from the latest `v*` tag (or the `base_tag` input), has `<Version>` in `Directory.Build.props` greater than that tag (bump the patch number in the fix commit), and touches no migrations;
   - runs Website tests (xunit, bunit), API tests (including Testcontainers integration), and Jest;
   - waits for `Production` environment approval, runs a read-only what-if (it stops if the deploy would create or delete Postgres or storage), deploys `api` and `web` with `aspire do`, and smoke tests `/health` on both.
4. Open a PR from the hotfix branch to `main` right away. The full pipeline runs in parallel with the deploy.
5. Tag the hotfix commit by hand once the deploy succeeds, so the next hotfix branches from it.
6. Merge to `main` and delete the branch. The normal deploy ships the same code again and syncs `main` with production.

**Do not skip step 6.** If the fix never reaches `main`, the next `main` deploy reverts it.

## Not covered

- **Migrations.** The workflow refuses a diff that touches them, and never runs migration jobs. Use the normal pipeline.
- **Infra and secret changes.** Make them in the normal pipeline. A hotfix still runs `aspire do` for the two apps, which re-applies the infrastructure they depend on from the hotfix branch (the release's own Bicep, so no change) and rewrites the Key Vault secrets with the same GitHub secret values.
- **Playwright, Sonar, format check.** These run on the PR to `main`, after the deploy.

## Roll back

The guard requires the production tag as an ancestor, so a rollback is a revert commit on top of it, deployed through the same flow:

```bash
git switch -c hotfix/2026-10-12-revert <production-tag>
git revert --no-edit <bad-commit>
git push -u origin HEAD
```

## Known limits

- No 0% traffic canary. That needs multiple-revision mode on the Container Apps, which is an AppHost change plus a provision. Until then the safety nets are the test gate, the readiness probe on the new revision, and the post-deploy smoke test.
- `hotfix.yml`, `cd.yml` and `staging.yml` each hold a copy of the deploy settings (variables and secrets mapped to `Parameters__*`). Change all three together.
- `aspire do` takes one step per call, so the workflow runs it twice (api, then web). The api call also builds and pushes the api image and takes about 4 to 5 minutes; web adds about 1 to 2.
- The hotfix mode of `staging.yml` runs the same two calls against staging. Test a change to the hotfix path there first.
