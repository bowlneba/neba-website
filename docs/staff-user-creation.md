@ -1,125 +0,0 @@
# Staff User Creation — Plan

## Overview

Today, the only way to create an `ApplicationUser` is `RegisterCommandHandler`, which is really an
admin-created-account shortcut: it takes an email + password directly from the caller and force-sets
`EmailConfirmed = true`, bypassing real email verification. This is a stopgap noted in the handler itself.

This plan replaces that shortcut with a proper flow for staff/internal accounts — people doing the
business of NEBA (webmasters, tournament directors, journalists, etc.) — created by an Admin or
Webmaster, who never see or set the new user's password. It also builds a piece of shared
infrastructure (a token-based password-set/reset endpoint) that closes out issue #84 as a side effect
and lays groundwork for issue #88.

There is no database-level distinction between a "staff" user and a "regular" user — both are rows in
the same `ApplicationUser` table. What differs is only which roles are assigned.

## Naming — Internal vs. Public (for future reference)

Two distinct flows, two distinct names, so they're never confused in code or UI copy:

| | Internal (this feature) | Public self-registration (future — issue #88) |
|---|---|---|
| Concept | Admin/Webmaster creates an account for someone else | Anonymous visitor creates their own account |
| API route | `POST /security/users` | `POST /security/register` |
| UI route | `/account/create-user` (hidden, no nav link) | `/sign-up` (public-facing) |
| Command | `CreateUserCommand` | `RegisterCommand` (reclaims the name once today's admin-shortcut `Register` retires) |

"Register"/"Sign Up" is reserved for the real, anonymous, double-opt-in public flow — not used for this
feature, since the account isn't being created by its own owner.

## Scope — This Feature

### `CreateUserCommand` (new: `Security/Users/CreateUser/`)

- **Authorization**: `Roles.Admin` or `Roles.Webmaster` only. Standard 401/403 on the API side — no
  special hiding behavior needed there (see UI section for the 404 requirement).
- **Input**:
    - `Email` (required)
    - `Roles` (required, one or more, validated against the known `Roles` set **excluding `Roles.Admin`**
      — the Admin role can never be granted through this endpoint, regardless of caller)
    - `UsbcId` (optional)
    - `PhoneNumber` (optional)
    - `Claims` (optional list of type/value pairs — endpoint supports arbitrary claims for
      extensibility; the UI will not expose this yet)
- **Behavior**:
    1. Create `ApplicationUser` with `EmailConfirmed = false` and no usable password.
    2. Call `UserManager.AddToRoleAsync` for each requested role (this is the first real caller of that
       API anywhere in the codebase).
    3. Add any supplied claims.
    4. Generate a password-set token (`UserManager.GeneratePasswordResetTokenAsync`).
    5. Email an invite link containing the token + user id, via a new email template (no existing
       invite/welcome template today).
- **Validation rule**: rejecting `Roles.Admin` from the allowed set is a structural/business rule that
  never depends on state, so it's `Error.Validation` (422), not `Error.Conflict`.

### Shared token-consumption endpoint (build as general infra, not invite-specific)

- New: `Security/Password/SetPasswordFromToken` (or similarly named — exact name TBD at
  implementation time; this is the "Phase 3" `ResetPasswordFromToken` endpoint referenced in the prior
  identity implementation plan, never built).
- **Authorization**: anonymous + token (`userId` + token).
- **Behavior**: validate the token, `UserManager.ResetPasswordAsync(user, token, newPassword)`, and set
  `EmailConfirmed = true` in the same operation — clicking the emailed link and successfully setting a
  password *is* the proof of email ownership.
- **Reused by** (over time, not all built now):
    1. This staff-invite flow (now).
    2. Issue **#84** — admin-triggered password reset upgrades to point at this same endpoint instead of
       emailing a plaintext temporary password. Trivial once this exists.
    3. Issue **#88** — public forgot-password flow, eventually.

### UI (Blazor)

- `/account/create-user` — standalone page, same pattern as `Login.razor` (no nav link, reached only by
  direct navigation). Role picker excludes Admin. No claims UI.
- **404, not 403, for unauthorized visitors** — enforced at the UI layer only (the page renders a
  NotFound view for non-Admin/non-Webmaster users). The API endpoint itself uses standard 401/403; it's
  not browsed directly, so hiding it there isn't worth a custom FastEndpoints authorization-failure
  handler.
- `/account/set-password?userId=&token=` — anonymous landing page for the invited user to set their
  password. On success they're confirmed and can log in.

## Explicitly Out of Scope / Separate

- **Issue #88** (public self-registration + USBC linking) is untouched by this work — different trust
  boundary (anonymous), different endpoint (`register`), different UI (`/sign-up`), real double-opt-in
  email confirmation. Planned separately, later.
- Today's `RegisterCommandHandler`/`RegisterEndpoint` (the current admin-shortcut with forced
  `EmailConfirmed = true`) is retired once this feature ships — its use case is fully superseded by
  `CreateUserCommand`.

## Related Issues

- **#84** — Upgrade admin password reset to token-based flow. Folded into this feature (see Delivery
  Plan below) since it's a direct consumer of the shared token-consumption endpoint built here — no
  reason to leave it as a separate later effort once that endpoint exists.
- **#88** — Member self-registration + USBC ID linking. Explicitly deferred; naming reserved above so it
  doesn't collide with this feature's routes/commands when it's eventually built.

## Delivery Plan

Feature branch: `feature/staff-users`. Work is broken into sequential sub-branches, each PR'd back into
the feature branch (not `main`) — same create/edit/merge pattern used for Tournaments. `feature/staff-users`
PRs into `main` once all sub-branches have landed. Each sub-branch is small enough to run through
`/feature-plan` on its own once work on it starts; this document only records the ordering and why.

1. **`set-password-from-token`** — Shared token-consumption endpoint (command/endpoint/validator +
   tests). No UI. Foundational: nothing downstream is end-to-end testable without it, and it's what #84
   repoints to.
2. **`create-user`** — `CreateUserCommand`/endpoint/validator + tests, invite email template. Depends
   on (1) merged so the invite link points at something real.
3. **`create-user-ui`** — Blazor `/account/create-user` and `/account/set-password` pages. Depends on
   (1) and (2); first point the full flow is testable in a browser.
4. **`admin-reset-upgrade` (#84)** — Repoint the existing admin-triggered password reset handler from
   emailing a plaintext temporary password to generating a token and calling (1)'s endpoint. Remove the
   old `GenerateTempPassword` helper and the plaintext-password email template. Depends on (1) only —
   can land any time after it, but sequenced last here since it's the smallest, lowest-risk change.
5. **`retire-register`** — Remove the old `RegisterCommandHandler`/`RegisterEndpoint` admin-shortcut and
   its feature flag, now fully superseded by (2). Cleanup-only, done last.

## Next Steps

1. Start with sub-branch (1), `set-password-from-token`, off `feature/staff-users`.
2. Run `/feature-plan` at the start of each sub-branch for its functional/code-level breakdown
   (endpoint checklist, validators, tests, etc.) — not detailed in this document.