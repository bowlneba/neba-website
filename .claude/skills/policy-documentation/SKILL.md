---
name: policy-documentation
description: Generate or refresh the docs/policies/README.md row (and dedicated file, if warranted) for an authorization policy, given its policy name or a Command/Endpoint file that references it. Usage: /policy-documentation <PolicyName-or-File>
---

Document (or, on re-run, validate and update) a single authorization policy per [ADR-0008](../../../docs/adr/0008-policy-documentation-structure.md): a row in `docs/policies/README.md`, plus a dedicated `docs/policies/{policy-name-kebab}.md` file only when the policy has real nuance.

## Arguments

- **PolicyName-or-File** — either a bare policy name (e.g. `CanManageArticles`), or a Command/Endpoint file that references one (e.g. `DeleteArticleEndpoint.cs`). If omitted, ask the user which policy to document.

## Step 1 — Resolve the policy name

- If given a bare name, use it directly.
- If given a file, find its `Configure()` and read the `.Policies(...)` (or `Roles(...)`/`AllowAnonymous()`) call. If it's `Policies(Permissions.X.PolicyName)`, the "policy" is the generic dynamic `Permission:{value}` mechanism, not `X` itself — stop and tell the user this is already covered by the single `Permission:{value}` row in `docs/policies/README.md`; there is nothing new to document unless they specifically want the dynamic mechanism's row refreshed.
- If it's `Policies(Permissions.SomePolicyNameConstant)` referencing a named policy (not a per-permission `.PolicyName`), that constant's value (e.g. `"CanManageArticles"`) is the policy name to document.
- If it's `Roles(...)`, treat the policy as "requires role(s) X" — there may not be a formal `AddPolicy(...)` for this at all (ASP.NET Core role checks don't require one); document it as a role-based entry rather than a registered policy.

## Step 2 — Find out whether it's actually registered and where it's enforced

**Do not assume a plausibly-named constant is wired up.** Grep for the ground truth:

1. **Registration**: `grep -rn "AddPolicy(" src/Neba.Api/Security/SecurityConfiguration.cs src/Neba.Website.Server/Account/AccountConfiguration.cs` — look for `AddPolicy({resolved-name}, ...)`. Read the lambda to determine the check kind:
   - `RequireAuthenticatedUser()` → any signed-in user.
   - `RequireAssertion(ctx => ctx.User.HasAnyPermission(X))` (or similar OR-of-many helper) → OR-of-many permissions — this is "real nuance," gets a dedicated file.
   - `RequireClaim(...)` / `RequireRole(...)` / a single straightforward check → trivial, README row only.
   - **Not found at all** → the policy is either role-based (Step 1's `Roles(...)` case) or is a defined-but-unregistered constant. State this explicitly; do not describe it as active.
2. **Enforcement sites**: `grep -rn "Policies(.*{resolved-name}\|AuthorizeView Policy=\"@.*{resolved-name}" src/Neba.Api/Features src/Neba.Website.Server src/Neba.Website.Client` (adjust the pattern to the actual constant/string). List every endpoint and `<AuthorizeView>` block found. **If registration exists (Step 2.1) but this search finds zero call sites, or vice versa, say so explicitly** — a registered-but-unused policy and a referenced-but-unregistered policy are both real, reportable states, not something to paper over.
3. **Who satisfies it**: for a permission-based check, grep `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs` for which roles are granted the relevant permission(s) (per `src/Neba.Api/Security/Domain/Roles.cs`). For a role-based check, the roles are given directly in the `Roles(...)`/`RequireRole(...)` call.

## Step 3 — Update `docs/policies/README.md`

Read the file. If a row for this policy already exists, update its columns to match Step 2's findings (don't just append a duplicate). If not, add a new row:

| Policy | Kind | Satisfied by | Enforced at | Details |

- **Kind**: `Static`, `Static, OR-of-many`, `Role-based`, or similar — plus a `(not yet registered)` qualifier if Step 2.1 found no `AddPolicy(...)` call.
- **Enforced at**: list every call site from Step 2.2, or state plainly that there are none yet.
- **Details**: link to the dedicated file (Step 4) if one exists/is warranted, otherwise `—`.

Do not touch other rows.

## Step 4 — Dedicated file, only if warranted

Create/update `docs/policies/{policy-name-kebab}.md` **only** if Step 2 found OR/AND-of-many semantics, an exception worth explaining, or reasoning a reviewer would otherwise have to reverse-engineer from the security config. A single-permission or single-role check does not need one — a README row is sufficient.

If warranted, use `docs/policies/can-manage-articles.md` as the structural model:

1. Title + a status line up front stating plainly whether it's currently registered/enforced (per Step 2) — lead with this if the policy is defined but not wired up, so nobody mistakes documentation-of-intent for documentation-of-fact.
2. `## What it means` — the OR/AND logic and what it currently evaluates over.
3. `## Why {this shape}` — only if there's a real design reason (e.g. why a static policy instead of the dynamic per-permission mechanism) worth recording.
4. `## Who satisfies it` (or `Who would satisfy it once registered`, if unregistered) — per Step 2.3.
5. `## Where it's enforced` — per Step 2.2, stated as plainly as "nowhere yet" if that's the truth.
6. `## Related` — link back to [ADR-0008](../../../docs/adr/0008-policy-documentation-structure.md) and any implementation plan doc that originated the policy.

If the file already exists, reconcile rather than rewrite: update the status line and any section whose facts changed (registration, enforcement sites, satisfying roles); leave prose that's still accurate.

## Step 5 — Report

Summarize:

- Whether the README row was added or updated, and what changed.
- Whether a dedicated file was created, updated, or correctly skipped (state which, and why, for the "skipped" case).
- Whether the policy turned out to be registered/enforced differently than its name would suggest — call this out clearly, since it's the mistake this skill exists to prevent.

## Rules

- Ground every claim in a grep result from `SecurityConfiguration.cs`, `AccountConfiguration.cs`, or an actual `.Policies(...)`/`AuthorizeView` call site — never infer enforcement from a constant's existence or name alone.
- A policy that's defined but unregistered, or registered but unused, is a normal, reportable state — document it as such rather than assuming it must be wired up somewhere you haven't found yet.
- Don't create a dedicated file for a trivial single-permission/single-role policy just because one was requested — a README row is the correct output for those; explain why in the report if you decline to create a file.
