# Post-merge actions (aspire deploy cutover)

Read this in step 3b, only when `docs/learnings/aspire-deploy-cutover.md` exists on the branch and the base is `main`.

If `docs/learnings/aspire-deploy-cutover.md` exists on the branch (`git ls-files docs/learnings/aspire-deploy-cutover.md`) and base-branch is `main`, this PR carries the move from `azd` to `aspire deploy`. Some of its steps happen in GitHub and Azure settings, outside the diff, so the review and the PR description must show them.

- Read the file. Copy its three checklists ("Before the merge", "First production deploy", "After the merge") and its "Variable cleanup" table into the review as a **📋 Post-merge Actions** section, every box unticked, in the file's order. The table says which repository variables must stay and which Production-environment copies to delete, so keep it whole.
- Check what you can without changing anything, and say which items you checked: for example `git diff <base-branch>...HEAD -- .github/workflows/staging.yml` still holds a `push:` trigger (an open "Before the merge" item), and `<Version>` is above `main`'s.
- Say plainly what you could not check (GitHub variables, Azure credentials). Do not tick those.
- If the file is deleted on the branch, skip this step.
