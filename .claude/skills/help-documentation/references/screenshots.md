# Screenshots (step 5)

Read this in step 5. Only a use case with a UI gets a spec.

**If a UI exists (Step 3 found call sites):**

Create or update `tests/e2e/docs-screenshots/{use-case-kebab}.spec.ts`, modeled on `tests/e2e/docs-screenshots/delete-article.spec.ts`:

- Log in with the required permission via `page.request.post('/__test/login?permissions={permission}')` if Step 1/2 found a permission requirement (skip if the flow is anonymous).
- Navigate to each page found in Step 3, waiting on a stable selector before each screenshot.
- Capture one screenshot per placeholder inserted in Step 4, writing to `path.join('docs', 'help', 'images', '{use-case-kebab}', '{step-slug}.png')`.
- **Prefer non-mutating capture paths.** If the flow has a natural "cancel"/"back out" step (like the confirm/cancel modal in the delete-article example), end the test there instead of completing the action, so the script is safe to re-run against the same mock data without a reset step. If the action has no natural undo (e.g. a create form), capture the pre-submit state and stop short of submitting; note in a comment why the flow doesn't go further.
- The Blazor server in `playwright.docs.config.ts`'s `webServer` block runs `dotnet run --configuration Release` — Debug builds surface debug-only UI chrome (e.g. a debug toolbar/buttons) that must never appear in a user-facing help screenshot. If this config's `dotnet run` command is ever changed, keep `--configuration Release` in it.
- Regardless of whether the doc or spec already existed, **always run the screenshots this pass** — do not skip regeneration just because files with the right names already exist:
  ```
  npm run docs:screenshots -- tests/e2e/docs-screenshots/{use-case-kebab}.spec.ts
  ```
- Report whether the run passed. If it failed (e.g. selectors no longer match, or the local stack didn't come up), say so plainly and leave the previous screenshots in place rather than deleting them.

**If no UI exists (Step 3 found no call sites):**

- Do not create a spec file, and do not run `docs:screenshots`.
- In the doc's `## Steps` section, still include the same `![...](images/{use-case-kebab}/{step-slug}.png)` placeholder syntax for any UI-shaped steps that might exist once a UI is built (there usually won't be any for a pure API command — in that case, omit the Screenshots-style placeholders and instead describe the command as an API-only operation).
- Add a note directly under the affected heading: *"No screenshots were generated — this command has no UI today. The placeholders above are left in place so a UI, if one is added later, can be documented without restructuring this file."*
