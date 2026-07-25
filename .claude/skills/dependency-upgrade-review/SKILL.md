---
name: dependency-upgrade-review
description: Review new major versions of dependencies for breaking changes and worthwhile new features before upgrading. Usage /dependency-upgrade-review [package names]
---

The user wants to know, for one or more dependencies with a new major version available, whether upgrading would break anything in this repo and whether any new features are worth adopting. This is a research-and-report skill — it does **not** bump package versions or edit code unless the user explicitly asks for that as a follow-up.

## What to do

1. **Determine the package list.**
   - If the user named packages in the arguments, use those.
   - Otherwise, discover outdated packages yourself:
     - .NET: `dotnet outdated` (installed as a global tool — `~/.dotnet/tools/dotnet-outdated`). Run it from the repo root; it reads `Directory.Packages.props` (central package management).
     - JS: `npm outdated` from the repo root (`package.json` exists at the root).
   - From either tool's output, keep only packages with a **major** version bump available (e.g. `13.1.0` → `14.0.0`). Ignore minor/patch-only bumps — this skill is specifically for major-version risk review, not routine updates.
   - Confirm the filtered list with the user before doing deep research if it's long (more than ~4 packages) — each package requires real investigation, not a rubber stamp.

2. **For each package, find the current and latest version.**
   - Current: `grep <PackageName> Directory.Packages.props` (or the relevant `package.json`).
   - Latest: query the registry directly rather than trusting search-engine summaries, which are often stale —
     - NuGet: `curl -s "https://api.nuget.org/v3-flatcontainer/<package-id-lowercase>/index.json"` and take the last entry in `versions` (skip prerelease/beta suffixes unless the user wants preview versions).
     - npm: `npm view <package> versions --json` (take the last stable entry).

3. **Research the release notes for every major version between current and latest** (not just the newest — a jump from 6→7 might skip an intermediate breaking change if 6.x had multiple majors released). Prefer, in order:
   - The project's GitHub releases: `gh api repos/<owner>/<repo>/releases --jq '.[] | select(.tag_name | test("<version-pattern>")) | {tag: .tag_name, body: .body}'`
   - A dedicated breaking-changes/migration doc if the project has one (check `docs/breaking-changes.md`, `MIGRATING.md`, `CHANGELOG.md` at the repo root via `gh api repos/<owner>/<repo>/contents/<path>` or `curl` against the raw GitHub URL pinned to the target tag).
   - Only fall back to `WebSearch`/`WebFetch` against arbitrary blog posts if GitHub has nothing usable — those sources are lower-confidence and should be flagged as such in the report.

4. **Cross-reference every breaking change against actual usage in this repo**, not against the package's general capabilities. For each breaking change found:
   - `grep`/`Explore` the codebase for the specific API, attribute, delegate signature, or behavior the change affects.
   - If it's used, read the call site closely enough to state whether the change actually alters behavior here (many breaking changes affect code patterns this repo doesn't use — say so plainly rather than listing every upstream breaking change as a risk).
   - If a determination can only be made by actually building against the new version (e.g. a compiler diagnostic that only fires post-upgrade), say that explicitly rather than guessing — don't present a guess as a finding.

5. **Separately, note new features worth adopting** — but hold them to a real bar:
   - Only surface features that map to something already awkward, hand-rolled, or duplicated in this repo (e.g. a custom implementation of something the library now does natively).
   - Explicitly reject features that don't fit current usage, and say why in one line — this keeps the report honest and short rather than a marketing recap of the changelog.

6. **Report the findings** grouped per package, in this shape:
   - **Breaking risk: low/medium/high** with a one-line justification.
   - Bullet list of breaking changes that matter here, each naming the actual file/call site affected.
   - Bullet list of breaking changes explicitly ruled out as not applicable (brief — one clause each, not a full changelog dump).
   - "Worth bringing in" — 0-3 features max, each tied to a concrete file/pattern in this repo it would replace or improve. Say "nothing worth adopting" if that's the honest answer.
   - Do not recommend the version bump itself as an action item — the user already knows they're deciding whether to upgrade; just give them the risk/reward picture.

## Notes

- This is a review, not an upgrade. Do not edit `Directory.Packages.props`, `package.json`, or any source file unless the user explicitly asks you to apply the upgrade after reading the report.
- Central package management means one version bump in `Directory.Packages.props` affects every project referencing that package — when listing affected call sites, search the whole repo (`src/` and `tests/`), not just the most obvious project.
- If a package ships several related packages together (e.g. `Refit` + `Refit.HttpClientFactory` + `Refit.Testing`), always check all of them even if only one was named — a major bump is usually synchronized across the family and each can have its own breaking changes (source generator changes affect `Refit`, DI wiring affects `Refit.HttpClientFactory`, test helpers affect `Refit.Testing`).
- Keep the report tight — this should read like a pre-upgrade risk memo, not a changelog reprint. If a package's major version has no relevant breaking changes and nothing worth adopting, one line is enough: "Breaking risk: none found. Nothing worth adopting."
