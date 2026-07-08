# CanManageArticles

Static ASP.NET Core policy (`RequireAssertion`), registered in `SecurityConfiguration.cs` and `AccountConfiguration.cs`.

> This describes the policy's current, shipped behavior. It is not an implementation plan — for the phased rollout of the article-management feature (badges, draft preview, etc.), see `docs/plans/CanManageArticlesPolicy.md`. Update this file as later phases land and the policy's real-world behavior changes.

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

## Who satisfies it today

- `Roles.Webmaster` — has `Permissions.DeleteArticle` directly.
- `Roles.Admin` — has every permission via `Permissions.List`.
- `Roles.Member` — does not satisfy this policy.

## Where it's enforced

- **API**: `DeleteArticleEndpoint` (`.Policies(Permissions.CanManageArticlesPolicyName)`).
- **UI**: `<AuthorizeView Policy="@Permissions.CanManageArticlesPolicyName">` gates management-only affordances (e.g. the delete button on `NewsDetail.razor`; a publication-status badge is planned per the implementation plan above).

## Related

- [ADR-0008](../adr/0008-policy-documentation-structure.md) — why this file exists and its structure.
- `docs/plans/CanManageArticlesPolicy.md` — the phased implementation plan this policy originated from.
