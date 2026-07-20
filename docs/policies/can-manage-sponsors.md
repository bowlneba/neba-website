# CanManageSponsors

**Status: registered and enforced.** `Permissions.CanManageSponsorsPolicyName` and `Permissions.SponsorManagementPermissions` are defined in `Permission.cs`, and `AddNebaPolicies()` (`PolicyExtensions.cs`) registers `AddPolicy(Permissions.CanManageSponsorsPolicyName, ...)`. This is called from both `SecurityConfiguration.cs` (API) and `AccountConfiguration.cs` (Website), so the policy is available to both `[Authorize]`-style endpoint gating and Blazor `<AuthorizeView>`.

## What it means

Succeeds when the caller holds **any** permission in `Permissions.SponsorManagementPermissions` — an OR check across a small, growing set of permissions, not a single fixed claim.

```csharp
public static readonly IReadOnlyCollection<Permissions> SponsorManagementPermissions =
[
    CreateSponsor,
    EditSponsor,
];
```

That set includes `Sponsors.CreateSponsor` and `Sponsors.EditSponsor` — adding a permission to this list is the only change needed to extend it further; no additional policy-wiring changes are required.

## Why a dedicated policy instead of the dynamic per-permission mechanism

The existing `PermissionPolicyProvider` / `"Permission:{value}"` mechanism resolves one permission per policy by design (see `PermissionRequirement` / `PermissionAuthorizationHandler`). Generalizing it to OR-of-many wasn't worth it for one call site, so `CanManageSponsors` is registered as a plain static policy instead, following the same shape as `CanManageArticles`:

```csharp
builder.AddPolicy(Permissions.CanManageSponsorsPolicyName, policy => policy
    .RequireAssertion(context => context.User.HasAnyPermission(Permissions.SponsorManagementPermissions)));
```

## Who satisfies it

- `Roles.Admin` — has every permission via `Permissions.List`, including `CreateSponsor` and `EditSponsor`.
- `Roles.Webmaster` — does not currently hold `Sponsors.CreateSponsor` or `Sponsors.EditSponsor` (see `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`), so it does not satisfy this policy today.
- `Roles.Member` — does not satisfy this policy.

## Where it's enforced

`CanManageSponsors` has one job: **visibility of staff-only presentation on the sponsors pages**, for any caller who can manage sponsors in some capacity. It does not gate any actual action or the Create/Edit Sponsor pages:

- `<AuthorizeView Policy="@Permissions.CanManageSponsorsPolicyName">` in `Sponsors.razor` gates the "Inactive Sponsors" section and the Active/Inactive status badge shown on each sponsor's tile.
- `<AuthorizeView Policy="@Permissions.CanManageSponsorsPolicyName">` in `SponsorDetail.razor` gates the same Active/Inactive status badge on the sponsor's detail page.

Every page and action that actually *does* something gates directly on the single permission its own API endpoint requires, not on this broader policy — a page's `<AuthorizeView>` should always match its command's `.Policies(...)` call, not the badge-visibility policy:

- `CreateSponsorEndpoint` — `.Policies(Permissions.CreateSponsor.PolicyName)`; matched by `<AuthorizeView Policy="@Permissions.CreateSponsor.PolicyName">` gating the entire `CreateSponsor.razor` page (`/sponsors/new`) and the "Add Sponsor" floating action button in `Sponsors.razor`.
- `EditSponsorEndpoint` — `.Policies(Permissions.EditSponsor.PolicyName)`; matched by `<AuthorizeView Policy="@Permissions.EditSponsor.PolicyName">` gating the entire `EditSponsor.razor` page (`/sponsors/{slug}/edit`), the staff-only "Live Read Text"/"Promotional Notes"/"Internal Contact" sections and edit buttons in `SponsorDetail.razor`, and the edit buttons on each sponsor tile in `Sponsors.razor`.

## Related

- [ADR-0008](../adr/0008-policy-documentation-structure.md) — why this file exists and its structure.
- [can-manage-articles.md](can-manage-articles.md) — the equivalent policy for News, same OR-of-many shape.
