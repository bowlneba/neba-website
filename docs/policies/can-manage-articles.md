# CanManageArticles

**Status: registered and enforced.** `Permissions.CanManageArticlesPolicyName` and `Permissions.ArticleManagementPermissions` are defined in `Permission.cs`, and `AddNebaPolicies()` (`PolicyExtensions.cs`) registers `AddPolicy(Permissions.CanManageArticlesPolicyName, ...)`. This is called from both `SecurityConfiguration.cs` (API) and `AccountConfiguration.cs` (Website), so the policy is available to both `[Authorize]`-style endpoint gating and Blazor `<AuthorizeView>`.

## What it means

Succeeds when the caller holds **any** permission in `Permissions.ArticleManagementPermissions` — an OR check across a small, growing set of permissions, not a single fixed claim.

```csharp
public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
[
    CreateArticle,
    DeleteArticle,
];
```

That set now includes both `News.CreateArticle` and `News.DeleteArticle`. `News.UpdateArticle` is expected to join it later — adding a permission to this list is the only change needed; no further policy-wiring changes are required.

## Why a dedicated policy instead of the dynamic per-permission mechanism

The existing `PermissionPolicyProvider` / `"Permission:{value}"` mechanism resolves one permission per policy by design (see `PermissionRequirement` / `PermissionAuthorizationHandler`). Generalizing it to OR-of-many wasn't worth it for one call site, so `CanManageArticles` is registered as a plain static policy instead:

```csharp
builder.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy
    .RequireAssertion(context => context.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));
```

## Who satisfies it

- `Roles.Webmaster` — has `Permissions.DeleteArticle` directly (but **not** `Permissions.CreateArticle` — see `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`).
- `Roles.Admin` — has every permission via `Permissions.List`, including both `CreateArticle` and `DeleteArticle`.
- `Roles.Member` — does not satisfy this policy.

## Where it's enforced

`CanManageArticles` has one job: **visibility of the article status badge** on list/detail views, for any caller who can manage articles in some capacity. It does not gate any actual action or the Create Article page:

- `<AuthorizeView Policy="@Permissions.CanManageArticlesPolicyName">` in `ArticleCard.razor`, `NewsDetail.razor`, and `NewsList.razor` gates whether the publication-status badge is shown to the caller.

Every page and action that actually *does* something gates directly on the single permission its own API endpoint requires, not on this broader policy — a page's `<AuthorizeView>` should always match its command's `.Policies(...)` call, not the list-visibility policy:

- `CreateArticleEndpoint` — `.Policies(Permissions.CreateArticle.PolicyName)`; matched by `<AuthorizeView Policy="@Permissions.CreateArticle.PolicyName">` gating the entire `CreateArticle.razor` page (`/news/new`) and the "Create Article" floating action button in `NewsList.razor`.
- `DeleteArticleEndpoint` — `.Policies(Permissions.DeleteArticle.PolicyName)`; matched by `<AuthorizeView Policy="@Permissions.DeleteArticle.PolicyName">` gating the delete button in `ArticleCard.razor`, `NewsDetail.razor`, `NewsList.razor`.

This used to be inconsistent: `CreateArticle.razor` briefly gated on `CanManageArticles` instead of `Permissions.CreateArticle.PolicyName`, which meant a caller who could only delete articles (not create them) could still open the create form and would only find out they lacked access when the save failed. It's since been corrected to gate on the same permission the endpoint requires — `CanManageArticles` stays scoped to badge visibility only, never to gating an action page.

## Related

- [ADR-0008](../adr/0008-policy-documentation-structure.md) — why this file exists and its structure.
- `docs/plans/CanManageArticlesPolicy.md` — the phased implementation plan this policy originated from.
