# Permission-Based Security — Design & Implementation Plan

Status: **proposed, not implemented**. This doc is the plan; no code has been changed yet.

## Goal

Today, authorization is role-only: `Roles.Admin`, `Roles.Webmaster`, `Roles.Member` (string
constants in `Neba.Api.Security.Domain.Roles`), and exactly one policy (`AuthenticatedPolicy`,
"is the caller logged in at all"). No endpoint currently gates on role, and there is no role
seeding at startup — roles only exist if created manually via `RoleManager`.

We want: a role (e.g. `Webmaster`) maps to a set of permissions (e.g. `Articles.Create`,
`Articles.Delete`), and FastEndpoints endpoints (and eventually Blazor `AuthorizeView`) gate on
**permission**, not role. Adding a permission to a role should not require touching endpoint code;
adding a new endpoint should declare the permission it needs, not the role.

## Decisions made (confirmed with user)

1. **Permission catalog = compile-time catalog**, not a DB-driven/admin-editable table. A
   `Permission` `SmartEnum` (the codebase already uses `Ardalis.SmartEnum`/`SmartFlagEnum`
   elsewhere, e.g. `HallOfFameCategory`), analogous in spirit to the existing `Roles` class but
   richer. Role→permission mapping is stored in `AspNetRoleClaims` (an existing, currently-unused
   Identity table) — no new schema.
2. **Endpoint authorization = policy-based via a custom `IAuthorizationPolicyProvider`.** Dynamic
   policy names like `"Permission:Articles.Create"`, resolved at runtime rather than pre-registered
   one-by-one. This is the one mechanism that composes with both FastEndpoints (`.Policies(...)`)
   and Blazor (`<AuthorizeView Policy="...">` / `[Authorize(Policy = "...")]`).
3. **Permission claims are baked into the JWT at login/refresh**, same as roles are today. No
   per-request DB lookup. Tradeoff: a permission change takes effect for a user on their next
   login/refresh, not instantly — acceptable, matches how role changes already behave.
4. **Admin gets every permission via `Permission.List`, not a hardcoded bypass.** Seeding `Admin`
   with the SmartEnum's built-in `.List` (all defined instances) means a newly added permission is
   automatically granted to `Admin` with no risk of forgetting to update a role-permission map —
   see §1a and §2.

## Where things live

- **`Neba.Api.Contracts/Security/Permission.cs`** (new, shared assembly) — the `Permission` `SmartEnum`, the permission catalog.
  This must live in `Neba.Api.Contracts`, not `Neba.Api.Security.Domain`, because `Neba.Api.Contracts`
  is the only assembly referenced by both `Neba.Api` and `Neba.Website.Server` (confirmed via
  `Neba.Website.Server.csproj` → `ProjectReference` to `Neba.Api.Contracts`). Website needs the same
  permission name strings to drive `AuthorizeView`/`[Authorize]` on its side.
- Role constants (`Roles.Admin` etc.) stay in `Neba.Api.Security.Domain.Roles` — only the API side
  needs role *names* (for seeding); Website only ever needs permission names.

## 1. Permission catalog

A `Permission` `SmartEnum` — one instance per permission, `Key` = the claim value / policy suffix
(dotted `Feature.Action`, e.g. `"Articles.Create"`), `Name` = a human-readable label (e.g.
`"Create Article"`) for future admin UI. Matches the project's existing `SmartEnum`/`SmartFlagEnum`
convention (e.g. `HallOfFameCategory`).

```csharp
// src/Neba.Api.Contracts/Security/Permission.cs
namespace Neba.Api.Contracts.Security;

public sealed class Permission : SmartEnum<Permission, string>
{
    public static readonly Permission ArticlesCreate = new("Articles.Create", "Create Article");
    public static readonly Permission ArticlesEdit = new("Articles.Edit", "Edit Article");
    public static readonly Permission ArticlesDelete = new("Articles.Delete", "Delete Article");
    public static readonly Permission ArticlesPublish = new("Articles.Publish", "Publish Article");

    public static readonly Permission SponsorsManage = new("Sponsors.Manage", "Manage Sponsors");

    // ... one instance per permission, grown incrementally as endpoints adopt it

    private Permission(string key, string name) : base(name, key) { }
}
```

`Permission.Key` is what gets stored as the claim value in `AspNetRoleClaims` and embedded in the
JWT/cookie claims; it's also what forms the policy-name suffix (`"Permission:" + Permission.ArticlesCreate.Key`).
`Permission.List` (built into `SmartEnum`, no extra code) is the full catalog — this is what lets
`Admin` auto-receive every permission without a role-permission map entry (§2) and without a
special-cased bypass in the authorization handler (§3).

## 2. Role → permission seeding

There is currently **no role seeding at all** — this needs to be added regardless of the
permission work, since `RoleManager` is unused today and roles only exist if someone creates them
by hand.

Add a startup seeder, e.g. `Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`, run once during
app startup (`IHostedService` or a call in `Program.cs` after `app.Build()`, guarded so it's
idempotent):

```csharp
internal static class SecurityRoleSeeder
{
    // role name -> permissions granted to that role
    private static readonly Dictionary<string, IReadOnlyCollection<Permission>> RolePermissions = new()
    {
        // Permission.List = every defined Permission instance. Admin always gets the full
        // current catalog, so a newly added permission needs no update here to reach Admin.
        [Roles.Admin] = Permission.List,
        [Roles.Webmaster] = [Permission.ArticlesCreate, Permission.ArticlesEdit,
                              Permission.ArticlesDelete, Permission.ArticlesPublish],
        [Roles.Member] = [],
    };

    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var (roleName, permissions) in RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingPermissionKeys = existingClaims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in permissions.Where(p => !existingPermissionKeys.Contains(p.Key)))
                await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission.Key));

            // Optional: remove claims for permissions no longer in RolePermissions[roleName],
            // so the seeder is also the source of truth for *removal*, not just addition. Because
            // Admin is seeded from Permission.List, this matters more for Webmaster/Member if a
            // permission is intentionally revoked from their list in code.
        }
    }

    public const string PermissionClaimType = "permission";
}
```

## 3. Authorization plumbing (`Neba.Api`)

New requirement + handler, e.g. `Security/Infrastructure/Authorization/PermissionRequirement.cs` and
`PermissionAuthorizationHandler.cs`:

```csharp
internal sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // No role-based bypass here — Admin's claims already include every Permission.List
        // entry (§2), so a plain claim check is sufficient and there's no special case to
        // remember to keep in sync with the permission catalog.
        if (context.User.HasClaim(SecurityRoleSeeder.PermissionClaimType, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

Custom policy provider so `"Permission:X"` policies don't need pre-registration:

```csharp
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var permission = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
```

Register in `SecurityConfiguration.AddSecurity()`:

```csharp
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Small helper so endpoints don't hand-write the `"Permission:"` prefix (typo risk) — can live as a
member on `Permission` itself:

```csharp
// on Permission (src/Neba.Api.Contracts/Security/Permission.cs)
public string PolicyName => $"Permission:{Key}";
```

Endpoint usage:

```csharp
Policies(Permission.ArticlesCreate.PolicyName);
```

## 4. JWT changes

`IJwtTokenService.CreateTokenPair` currently takes `IReadOnlyCollection<string> roles` and emits
one `ClaimTypes.Role` claim per role. It needs to also emit one `"permission"` claim per resolved
permission.

Resolving permissions from roles requires `RoleManager<ApplicationRole>` (to read each role's
claims) at the three call sites that currently call `userManager.GetRolesAsync(user)`:

- `LoginCommandHandler.HandleAsync`
- `RefreshTokenCommandHandler.HandleAsync`
- (`GetCurrentUserQueryHandler` doesn't issue a token, but see §6 below — it should also expose
  permissions in its DTO.)

Plan: add a small resolver used by both handlers instead of duplicating the RoleManager-claims
lookup:

```csharp
// Neba.Api/Security/Infrastructure/PermissionResolver.cs
internal static class PermissionResolver
{
    // Returns Permission.Key strings (claim values), not Permission instances — the caller
    // only needs to embed them as JWT claims.
    public static async Task<IReadOnlyCollection<string>> ResolveAsync(
        RoleManager<ApplicationRole> roleManager, IEnumerable<string> roleNames)
    {
        var permissionKeys = new HashSet<string>();
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var claims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(c => c.Type == SecurityRoleSeeder.PermissionClaimType))
                permissionKeys.Add(claim.Value);
        }
        return permissionKeys;
    }
}
```

Then `LoginCommandHandler`/`RefreshTokenCommandHandler` gain a `RoleManager<ApplicationRole>`
constructor dependency, resolve permissions after `GetRolesAsync`, and pass them into
`jwtTokenService.CreateTokenPair(user, roles, permissions)`.

`JwtTokenService.CreateTokenPair` signature changes to accept the extra collection and adds:

```csharp
foreach (var permission in permissions)
    claims.Add(new Claim("permission", permission));
```

This is a breaking signature change to `IJwtTokenService` — update the interface, the
implementation, and both call sites together, plus their unit tests (mocks currently set up with
`MockBehavior.Strict` will need the new parameter in `Setup`).

## 5. `GetCurrentUser` / `me` endpoint

`UserDto` (and the `MeResponse` contract) currently expose `Roles`. Add `Permissions` alongside so
the Blazor client can build its `ClaimsPrincipal` with permission claims without re-parsing the
JWT differently than it already does (see §6) — mostly this is "for symmetry / debugging /
possible future direct API consumers," since the JWT is the actual source of truth the Blazor
server reads from.

`GetCurrentUserQueryHandler` needs the same `RoleManager`-based resolution as §4 (it already calls
`userManager.GetRolesAsync`, so it's the same shape of change).

## 6. Blazor side (`Neba.Website.Server`)

`SecurityClaimsBuilder.BuildClaims` currently copies `ClaimTypes.Role` and `usbc_id` claims out of
the decoded JWT payload into the cookie's `ClaimsIdentity`. Add `"permission"` to that allow-list:

```csharp
if (jwtClaim.Type == ClaimTypes.Role || jwtClaim.Type == "usbc_id" || jwtClaim.Type == "permission")
    claims.Add(jwtClaim);
```

For `<AuthorizeView Policy="Permission:Articles.Create">` / `[Authorize(Policy = "...")]` to work
in the Blazor app, **the same `PermissionPolicyProvider`/`PermissionAuthorizationHandler` pair from
§3 needs to be registered in `Neba.Website.Server` too** — it's a separate process/DI container
from `Neba.Api`, so nothing is shared automatically. Since the permission check itself is just
"does the `ClaimsPrincipal` have claim `(\"permission\", X)`", this handler has no dependency on
`RoleManager`/EF — it's pure claims logic, so it can be a small shared class. Two options:

- Duplicate the ~15-line handler/provider in both projects (simplest, no new shared project).
- Move `PermissionRequirement`/`PermissionAuthorizationHandler`/`PermissionPolicyProvider` into
  `Neba.Api.Contracts` (or another already-shared assembly) since they have no `Neba.Api`-only
  dependencies. **Recommended** — avoids drift between two copies of the same logic.

Register in `Neba.Website.Server`'s auth setup (wherever `AccountConfiguration.cs` wires up cookie
auth) the same way as §3.

## 7. Migration / rollout notes

- No EF migration needed — `AspNetRoleClaims` already exists in the `20260610014726_Security_Init`
  migration.
- Because permissions are baked into the JWT at login, **existing logged-in users won't have
  permission claims until they log in again or hit refresh**. Access-token lifetime is short
  (`JwtSettings.AccessTokenExpiryMinutes`), refresh token flow will pick up new claims within one
  refresh cycle — acceptable, no forced logout needed.
- Seeding must run before any endpoint gates on a permission, otherwise `Webmaster`/`Admin` users
  refreshing their token get zero permission claims. Run the seeder at startup, before
  `app.Run()`.
- No endpoint currently has role/permission checks, so this can be rolled out incrementally:
  land the infrastructure (catalog, seeding, policy provider, JWT changes) in one PR with zero
  behavior change (nothing calls `.Policies("Permission:...")` yet), then convert endpoints to
  permission checks feature-by-feature in follow-up PRs.

## Files touched (summary)

**New:**

- `src/Neba.Api.Contracts/Security/Permission.cs` (the `SmartEnum`)
- `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`
- `src/Neba.Api/Security/Infrastructure/PermissionResolver.cs`
- `PermissionRequirement.cs`, `PermissionAuthorizationHandler.cs`, `PermissionPolicyProvider.cs`
  (location per §6 decision — `Neba.Api.Contracts` recommended, else duplicated in both API and
  Website projects)

**Modified:**

- `src/Neba.Api/Security/Domain/Roles.cs` — no change needed, but seeder references it
- `src/Neba.Api/Security/SecurityConfiguration.cs` — register policy provider + handler, call seeder
- `src/Neba.Api/Security/IJwtTokenService.cs`, `JwtTokenService.cs` — add `permissions` parameter
- `src/Neba.Api/Security/Login/LoginCommandHandler.cs` — resolve + pass permissions
- `src/Neba.Api/Security/RefreshToken/RefreshTokenCommandHandler.cs` — resolve + pass permissions
- `src/Neba.Api/Security/GetCurrentUser/GetCurrentUserQueryHandler.cs`, `UserDto` — expose permissions
- `src/Neba.Api.Contracts/Security/...MeResponse` — add `Permissions` field
- `src/Neba.Website.Server/Account/SecurityClaimsBuilder.cs` — copy `permission` claims
- `src/Neba.Website.Server/.../AccountConfiguration.cs` (or wherever auth is wired) — register
  policy provider + handler
- Existing tests for `JwtTokenService`, `LoginCommandHandler`, `RefreshTokenCommandHandler`,
  `GetCurrentUserQueryHandler` — signature changes ripple into `MockBehavior.Strict` setups

## Open questions to resolve before/at implementation time

1. Where do `PermissionRequirement`/`PermissionAuthorizationHandler`/`PermissionPolicyProvider`
   live — shared in `Neba.Api.Contracts`, or duplicated per project? (Doc recommends shared.)
2. Should the seeder also *remove* permission claims no longer listed for a role (keeping
   `RolePermissions` as the single source of truth), or only additive? Additive-only is safer but
   can accumulate stale claims if a permission is renamed/removed from a role in code. (Admin is
   unaffected either way since it's always reseeded from the live `Permission.List`.)
3. Full initial permission catalog per feature area — this doc sketches a handful of `Articles`/
   `Sponsors` permissions as examples; the actual list should be drawn up against real endpoints
   before the first PR.
