# ADR-0008: Policy Documentation Structure

## Status

Proposed

## Context

[ADR-0007](0007-in-repo-user-help-documentation.md) requires help docs to list the prerequisite role/permission for a command, and suggested linking to a policy doc when one exists (e.g. `CanManageArticlesPolicy.md`). That file, however, is at `docs/plans/CanManageArticlesPolicy.md` — an **implementation plan** (future tense, phased TODOs, an "open items to confirm" section), not a stable reference of what the policy currently means. Plans are disposable once their work lands; a policy reference needs to persist and stay accurate for as long as the policy exists.

The system currently has a small number of authorization policies:

- `Authenticated` (static, `RequireAuthenticatedUser()`)
- `CanManageArticles` (static, `RequireAssertion` — OR-of-many permission semantics)
- One dynamic `Permission:{value}` policy per `Permissions` value (`Read`, `Write`, `News.DeleteArticle`, ...), resolved by `PermissionPolicyProvider`

Most of these are a single permission check with nothing to explain beyond "requires claim X." A small minority (like `CanManageArticles`) have real behavior worth writing down: OR semantics, which permissions currently satisfy it, and how it's expected to grow.

Writing a full markdown file for every trivial single-permission policy would produce mostly-empty files that add maintenance overhead without adding information. Writing nothing risks the opposite problem `CanManageArticles` already ran into: a policy's actual semantics living only in prose inside a phased implementation plan, disconnected from the reference someone would look for later.

## Decision

Policy documentation lives at `docs/policies/`, structured the same way `docs/adr/` separates its index from individual decisions:

- **`docs/policies/README.md`** — one row per policy in a table: policy name, what satisfies it (permission(s)/role(s)), where it's enforced. This is the default and is sufficient for any policy that's a straightforward single-permission or single-role check.
- **`docs/policies/<policy-name>.md`** — a dedicated file only for policies with actual nuance to explain: OR/AND-of-many semantics, exceptions, or reasoning a reviewer would otherwise have to reverse-engineer from `SecurityConfiguration.cs` / `Permission.cs`. Linked from the README row for that policy.

The source of truth remains the code (`Permission.cs`, `Roles.cs`, `SecurityRoleSeeder.cs`, `SecurityConfiguration.cs`). These docs describe it in prose for reviewers and help-doc authors and link back to it — the same relationship an ADR has to the code it justifies.

**`docs/plans/CanManageArticlesPolicy.md` is not repurposed as this reference.** It stays where it is as the implementation plan for the remaining phases of that work. Once the policy reference exists, it documents the policy's *current, shipped* state — which may lag behind the plan until later phases land.

### Enforcement

`pull-request-review.instructions.md` is updated: a PR that adds a new authorization policy (a new `AddPolicy(...)` call, or a new `Permissions` value that changes an existing policy's semantics) must add or update the corresponding row in `docs/policies/README.md`, and add a dedicated file if the policy has OR/AND-of-many or otherwise non-trivial semantics.

## Consequences

### Positive

- Reviewers and help-doc authors have one stable place to look up "what does policy X require," independent of whatever implementation plan originally introduced it.
- Trivial policies stay a single table row — no file-per-policy overhead for the common case.
- Complex policies get room to explain themselves, same as ADRs do for complex decisions.

### Negative

- Judgment call required on "does this policy need its own file" — mitigated by using the same bar already applied to `CanManageArticles` (OR-of-many semantics) as the reference example.
- Another doc location to keep in sync with `Permission.cs`/`SecurityConfiguration.cs`, enforced only by PR review, not automated tests.

## Related Decisions

- [ADR-0007](0007-in-repo-user-help-documentation.md): In-Repo User Help Documentation — help docs link to policy docs here for prerequisites.
