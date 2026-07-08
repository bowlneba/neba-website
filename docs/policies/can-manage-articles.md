# CanManageArticles

**Status: constant defined, policy not yet registered.** `Permissions.CanManageArticlesPolicyName` and `Permissions.ArticleManagementPermissions` exist in `Permission.cs`, but there is currently no `AddPolicy(Permissions.CanManageArticlesPolicyName, ...)` call in `SecurityConfiguration.cs` or `AccountConfiguration.cs`, and nothing references this policy name in an endpoint or `<AuthorizeView>`. `DeleteArticleEndpoint` and the delete UI (`NewsDetail.razor`, `NewsList.razor`) currently gate on `Permissions.DeleteArticle.PolicyName` (the dynamic `Permission:News.DeleteArticle` policy) directly instead.

This file describes what the policy is *designed* to mean once it's wired up (per `docs/plans/CanManageArticlesPolicy.md`), so it's ready to correct in place the moment that lands — see the "Where it's enforced" section below for the current, real state.

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
.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy.RequireAssertion(ctx =>
    ctx.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));
```

## Who would satisfy it once registered

- `Roles.Webmaster` — has `Permissions.DeleteArticle` directly.
- `Roles.Admin` — has every permission via `Permissions.List`.
- `Roles.Member` — would not satisfy this policy.

## Where it's enforced

**Nowhere yet.** `DeleteArticleEndpoint` (`.Policies(Permissions.DeleteArticle.PolicyName)`) and the UI (`<AuthorizeView Policy="@Permissions.DeleteArticle.PolicyName">` in `NewsDetail.razor` / `NewsList.razor`) currently gate directly on the single `News.DeleteArticle` permission via the dynamic per-permission mechanism, not on `CanManageArticles`. Since `ArticleManagementPermissions` today contains only `DeleteArticle`, the two checks are currently equivalent in practice — but they are not the same policy, and a caller could be granted `News.DeleteArticle` without `CanManageArticles` ever being registered or checked.

Once `News.CreateArticle` / `News.UpdateArticle` exist and this policy is actually registered and wired to those endpoints/UI affordances, update this section to reflect the real enforcement points.

## Related

- [ADR-0008](../adr/0008-policy-documentation-structure.md) — why this file exists and its structure.
- `docs/plans/CanManageArticlesPolicy.md` — the phased implementation plan this policy originated from.
