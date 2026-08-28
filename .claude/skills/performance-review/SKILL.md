---
name: performance-review
description: Review a diff (current branch, a PR number, or a path) for performance issues — EF Core query patterns, caching gaps, Blazor render cost, and algorithmic complexity — as a dedicated pass separate from code-review's correctness/simplification focus and security-review's security focus. Usage: /performance-review [target]
---

Review changes for performance, not correctness or security. `code-review` already covers correctness/simplification/efficiency-as-cleanup, and `security-review` covers security — this skill exists for the performance-specific patterns this codebase has been burned by before (EF Core navigation/query traps, caching layer misuse, Blazor Server round-trip cost) that a general-purpose review pass tends to skim past.

## Arguments

- **target** (optional) — a PR number, branch name, or path. Defaults to the current branch's diff against `main`.

## Step 1 — Establish the diff

```
git diff main...HEAD
git log main...HEAD --oneline
git diff main...HEAD --name-only
```

If a PR number or branch was given instead, diff against that. If the diff is large, group the review by slice (Features/{Feature} → Contracts → Blazor/Website).

## Step 2 — Review by category

Only evaluate categories relevant to what actually changed (don't force a Blazor finding out of a pure API diff). For each, check against the patterns below — these are drawn from CLAUDE.md's own "Learnings" (FusionCache, EF Core navigation fixup, Blazor Server round-trips) plus the `dotnet-entity-framework-core`/`dotnet-blazor` skills, not generic advice.

**EF Core / Database**
- A loop that issues a query per iteration (N+1) instead of a single batched query.
- A read-only query missing `.AsNoTracking()`.
- A list endpoint with no pagination or page-size cap on a table that can grow unbounded.
- A new filter/sort column with no supporting index.
- Loading a full aggregate/entity graph via `.Include()` when the caller only needs a few scalar fields — check whether the "Lightweight Collection Projections" convention in CLAUDE.md should apply instead.
- Any new query that materializes a collection navigation property client-side that could be filtered/aggregated in SQL instead.

**Caching**
- A new query handler that re-derives from CLAUDE.md's caching convention it should be an `ICachedQuery` behind `IFusionCache`, but isn't.
- A cache key built from mutable or non-deterministic input (would silently fragment the cache).
- A command that mutates data covered by an existing cached query but doesn't evict/invalidate that cache entry.
- Any use of Microsoft's `HybridCache` — flag as a hard violation; this app standardizes on `IFusionCache` for all query caching/eviction/expiration, and the two cache stacks are not bridged.

**Background / async**
- Blocking calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) on async work.
- Missing `CancellationToken` propagation on a new async method.
- An unbounded loop making external calls (HTTP, storage, email) one at a time where batching is possible.

**Blazor**
- A component fetching its own data directly instead of through a query/service (also a `pull-request-prep` correctness check, but re-flag here if it also implies a per-render fetch cost).
- A large collection rendered with no paging/virtualization on a list page.
- New per-keystroke or high-frequency interactive behavior implemented via C# `@onkeydown`/server round-trips instead of colocated JS — this is the exact race condition documented in CLAUDE.md's "Custom Interactive Blazor Server Inputs" learning, not just a style preference.
- A render-mode choice that causes the same data to be re-fetched on every reconnect/prerender cycle unnecessarily.

**Algorithmic complexity**
- Nested loops over the same or related collections where a lookup (`Dictionary`/`HashSet`) would drop it from O(n²) to O(n).
- A LINQ `.Where()`/`.Count()`/`.Any()` re-evaluated inside a loop instead of computed once outside it.

## Step 3 — Severity and output

Use the same severity levels as `pull-request-review.instructions.md` (🚫 Blocker / ⚠️ Should Fix / 💡 Suggestion), so this review composes cleanly with `pull-request-prep` and `code-review` output. A Blocker here means a pattern with a known, confirmed production incident behind it in this codebase (e.g. `HybridCache` usage, the Blazor keyboard race) or an unbounded query/loop that will visibly degrade under realistic load — not every N+1 is automatically a blocker if the collection is small and bounded.

Present findings grouped by category (only include categories with findings):

```
## Performance Review

### 🚫 Blockers
### ⚠️ Should Fix
### 💡 Suggestions
### ✅ Looks Good
```

For each finding: file/line link, the pattern violated, and the concrete failure scenario (what load/input triggers the cost) — not just "this could be slow."

Do not apply fixes automatically. Show the findings, then ask whether to apply them.
