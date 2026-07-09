# CanManageArticles

**Status: registered and enforced.** `Permissions.CanManageArticlesPolicyName` and `Permissions.ArticleManagementPermissions` are defined in `Permission.cs`, and `AddNebaPolicies()` (`PolicyExtensions.cs`) registers `AddPolicy(Permissions.CanManageArticlesPolicyName, ...)`. This is called from both `SecurityConfiguration.cs` (API) and `AccountConfiguration.cs` (Website), so the policy is available to both `[Authorize]`-style endpoint gating and Blazor `<AuthorizeView>`.

## What it means

Succeeds when the caller holds **any** permission in `Permissions.ArticleManagementPermissions` — an OR check across a small, growing set of permissions, not a single fixed claim.

```csharp
public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
[
    DeleteArticle,
];
```

Today that set is just `News.DeleteArticle`. `News.CreateArticle` / `News.UpdateArticle` are expected to join it later — adding a permission to this list is the only change needed; no further policy-wiring changes are required.

## Why a dedicated policy instead of the dynamic per-permission mechanism

The existing `PermissionPolicyProvider` / `"Permission:{value}"` mechanism resolves one permission per policy by design (see `PermissionRequirement` / `PermissionAuthorizationHandler`). Generalizing it to OR-of-many wasn't worth it for one call site, so `CanManageArticles` is registered as a plain static policy instead:

```csharp
builder.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy
    .RequireAssertion(context => context.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));
```

## Who satisfies it

- `Roles.Webmaster` — has `Permissions.DeleteArticle` directly.
- `Roles.Admin` — has every permission via `Permissions.List`.
- `Roles.Member` — does not satisfy this policy.

## Where it's enforced

`CanManageArticles` currently drives **visibility of the article status badge**, not the delete action itself:

- `<AuthorizeView Policy="@Permissions.CanManageArticlesPolicyName">` in `ArticleCard.razor`, `NewsDetail.razor`, and `NewsList.razor` gates whether the publication-status badge is shown to the caller.

The delete action is gated separately, directly on the single `News.DeleteArticle` permission via the dynamic per-permission mechanism, not on `CanManageArticles`:

- `DeleteArticleEndpoint` — `.Policies(Permissions.DeleteArticle.PolicyName)`.
- `<AuthorizeView Policy="@Permissions.DeleteArticle.PolicyName">` in `ArticleCard.razor`, `NewsDetail.razor`, `NewsList.razor` gates the delete button itself.

Since `ArticleManagementPermissions` today contains only `DeleteArticle`, the two checks are currently equivalent in practice for who satisfies them — but they are not the same policy, and a future permission added only to `ArticleManagementPermissions` (e.g. `News.CreateArticle`) would grant status-badge visibility without granting delete access.

## Related

- [ADR-0008](../adr/0008-policy-documentation-structure.md) — why this file exists and its structure.
- `docs/plans/CanManageArticlesPolicy.md` — the phased implementation plan this policy originated from.
