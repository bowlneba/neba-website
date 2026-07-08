# CanManageArticles Policy — Implementation Plan

Two phases. Land each as its own PR (or commit set) so it can be reviewed independently.

---

## Background / resolved design decisions

- **`CanManageArticles` is a policy, not a permission.** It succeeds when the caller holds *any* of a small set of article-management permissions (today just `News.DeleteArticle`; `News.CreateArticle`/`News.UpdateArticle` will join the set later without further policy-wiring changes). It is registered as a normal static ASP.NET Core policy (`RequireAssertion`), **not** routed through the existing dynamic `PermissionPolicyProvider`/`"Permission:{value}"` mechanism — that provider is single-permission by design (see `PermissionRequirement`/`PermissionAuthorizationHandler`), and generalizing it to OR-of-many isn't needed for one call site. Keep it simple: one extra `AddPolicy(...)` call in each app.
- **List/Get endpoints stay `AllowAnonymous()`.** No auth requirement is added at the endpoint level.
- **Cache-key vs. "logic belongs in the handler" tension, resolved:** `CachedQueryHandlerDecorator` computes the `ICachedQuery.Cache` key from the `Query` object *before* the handler ever runs, so the cache key can only vary by something already present on the `Query` at construction time. The reconciliation:
  - The **endpoint** does one mechanical thing: read the caller's permission claims into a boolean and put it on the `Query` — exactly like it already copies `Page`/`PageSize` off the request. No branching, no article-filtering decision, just claims → property.
  - The **handler** owns all actual logic: whether to apply the published/date filter, whether attachments/status are shaped differently, etc. It reads the boolean the endpoint provided.
  - The cache key incorporates that same boolean, so a public (anonymous) response and a management (all-articles) response for the same page/slug get **distinct cache entries** — no cross-user leakage.
- **`PublicationStatus` is always returned by the API**, both in `ListArticles` and `GetArticle` responses, regardless of caller permission. The UI is what conditionally hides it (via `<AuthorizeView Policy="...">`, the same pattern already used for the delete button). This avoids needing separate response shapes / extra cache variants for field-presence — only the *article set* (drafts included or not) varies by permission, not the response shape.
- **`PublicationStatus` must not be cached as the domain `SmartEnum` instance** (see `CLAUDE.md` → FusionCache Deserialization Recovery). Project it to `.Name` (`string`) in the EF `Select(...)` projection, same convention used elsewhere for cached query DTOs.
- **`GetArticleQueryHandler` gets the same treatment as `ListArticlesQueryHandler`** — a privileged caller can fetch a single draft/scheduled article by slug (e.g., to preview it before publishing). Confirm this is desired before implementing 1d below; it's a new capability (direct-URL draft preview), not just a list-filtering change.

---

## Phase 1 — API

### 1a. `CanManageArticles` policy

**`src/Neba.Api.Contracts/Security/Permission.cs`** — add a static collection of permissions that satisfy article management, and a policy-name constant:

```csharp
#region News

/// <summary>
/// This is a temporary permission to set us up until real permissions come into the picture
/// </summary>
public static readonly Permissions DeleteArticle = new("News.DeleteArticle", "Delete Article");

/// <summary>
/// Permissions that satisfy the <see cref="CanManageArticlesPolicyName"/> policy (OR semantics).
/// Extend this list as News.CreateArticle / News.UpdateArticle are introduced — no other
/// wiring needs to change when that happens.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
[
    DeleteArticle
];

/// <summary>
/// Policy name satisfied when the caller holds any permission in <see cref="ArticleManagementPermissions"/>.
/// </summary>
public const string CanManageArticlesPolicyName = "CanManageArticles";

#endregion
```

**New file — `src/Neba.Api.Contracts/Security/ClaimsPrincipalPermissionExtensions.cs`** (shared, used by both endpoint code and Razor `@code` blocks that need a raw boolean rather than an `<AuthorizeView>`):

```csharp
using System.Security.Claims;

namespace Neba.Api.Contracts.Security;

public static class ClaimsPrincipalPermissionExtensions
{
    public static bool HasAnyPermission(this ClaimsPrincipal user, IReadOnlyCollection<Permissions> permissions)
        => permissions.Any(p => user.HasClaim(Permissions.ClaimType, p.Value));
}
```

**Register the policy in both apps** (static `AddPolicy`, same shape as the existing `AuthenticatedPolicy` registration — no changes needed to `PermissionPolicyProvider`, since a bare `"CanManageArticles"` policy name doesn't start with the `"Permission:"` prefix and falls through to the default provider, which resolves statically-registered policies):

- `src/Neba.Api/Security/SecurityConfiguration.cs`:
  ```csharp
  builder.Services
      .AddAuthorizationBuilder()
      .AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser())
      .AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy.RequireAssertion(ctx =>
          ctx.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));
  ```
- `src/Neba.Website.Server/Account/AccountConfiguration.cs`: same `AddPolicy(...)` call added to the existing `services.AddAuthorization(...)` (or `AddAuthorizationBuilder()`, whichever the current call shape is) — needed so `<AuthorizeView Policy="@Permissions.CanManageArticlesPolicyName">` resolves in Blazor Server too.

No change needed to `SecurityRoleSeeder.cs` — `Roles.Webmaster` already has `Permissions.DeleteArticle`, and `Roles.Admin` gets everything via `Permissions.List`, so both already satisfy the new policy.

### 1b. Add `PublicationStatus` to internal DTOs (as `string`)

- **`ArticleSummaryDto.cs`** (`src/Neba.Api/Features/News/ListArticles/`) — add `public required string PublicationStatus { get; init; }`.
- **`ArticleDetailDto.cs`** (`src/Neba.Api/Features/News/GetArticle/`) — add `public required string PublicationStatus { get; init; }`.

### 1c. Add `PublicationStatus` to response contracts

- **`ArticleSummaryResponse.cs`** (`src/Neba.Api.Contracts/News/ListArticles/`) — add `public required string PublicationStatus { get; init; }`.
- **`ArticleDetailResponse.cs`** (`src/Neba.Api.Contracts/News/GetArticle/`) — add `public required string PublicationStatus { get; init; }`.
- Update `ArticleSummaryResponseFactory` / `ArticleDetailResponseFactory` (`tests/Neba.TestFactory/News/`) and any Verify snapshots (`*.verified.txt` for both endpoint tests) for the new field.

### 1d. `ListArticlesQuery` / `ListArticlesQueryHandler`

**`ListArticlesQuery.cs`** — add the caller-permission flag and fold it into the cache key:

```csharp
public sealed record ListArticlesQuery : ICachedQuery<PagedResult<ArticleSummaryDto>>, IPaginationQuery
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required bool CallerHasArticleManagementPermission { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.News.ListArticles(Page, PageSize, CallerHasArticleManagementPermission);

    public TimeSpan Expiry => TimeSpan.FromMinutes(45);
}
```

**`CacheDescriptors.cs`** (`src/Neba.Api/Caching/`) — add the scope to the key (tags stay the same so `RemoveByTagAsync("neba:news:articles", ...)` still busts both variants):

```csharp
public static CacheDescriptor ListArticles(int page, int pageSize, bool callerHasArticleManagementPermission)
    => new()
    {
        Key = $"neba:news:articles:list:page:{page}:size:{pageSize}:scope:{(callerHasArticleManagementPermission ? "management" : "public")}",
        Tags = ["neba", "neba:news", "neba:news:articles"]
    };
```

**`ListArticlesEndpoint.cs`** — mechanical translation only (mirrors how `Page`/`PageSize` are already read off the request):

```csharp
var query = new ListArticlesQuery
{
    Page = req.Page,
    PageSize = req.PageSize,
    CallerHasArticleManagementPermission = User.HasAnyPermission(Permissions.ArticleManagementPermissions)
};
```

**`ListArticlesQueryHandler.cs`** — the actual business logic. Skip the published/date filter when the caller has article-management permission; project `PublicationStatus.Name`:

```csharp
public async Task<PagedResult<ArticleSummaryDto>> HandleAsync(ListArticlesQuery query, CancellationToken cancellationToken)
{
    var baseQuery = _articles;

    if (!query.CallerHasArticleManagementPermission)
    {
        baseQuery = baseQuery.Where(article => article.PublicationStatus == PublicationStatus.Published
            && article.PublishDateUtc <= _timeProvider.GetUtcNow());
    }

    var totalItems = await baseQuery.CountAsync(cancellationToken);

    var rows = await baseQuery
        .Select(article => new
        {
            article.Id,
            article.Slug,
            article.Title,
            article.Content,
            HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
            HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null,
            article.PublishDateUtc,
            PublicationStatus = article.PublicationStatus.Name,
        })
        .OrderByDescending(article => article.PublishDateUtc)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToListAsync(cancellationToken);

    var items = rows.ConvertAll(row => new ArticleSummaryDto
    {
        Id = row.Id,
        Slug = row.Slug,
        Title = row.Title,
        Excerpt = BuildExcerpt(row.Content),
        HeaderImageUrl = row.HeaderImageContainer != null && row.HeaderImagePath != null
            ? _fileStorageService.GetBlobUri(row.HeaderImageContainer, row.HeaderImagePath)
            : null,
        PublishDateUtc = row.PublishDateUtc,
        PublicationStatus = row.PublicationStatus,
    });

    return new PagedResult<ArticleSummaryDto>([.. items], totalItems);
}
```

Note: drafts/scheduled articles are included in the **same paginated, `PublishDateUtc`-descending-ordered list** as published ones for privileged callers — no separate "drafts" section.

### 1e. `GetArticleQuery` / `GetArticleQueryHandler`

Same treatment, symmetric with 1d:

**`GetArticleQuery.cs`**:
```csharp
public sealed record GetArticleQuery : ICachedQuery<ErrorOr<ArticleDetailDto>>
{
    public required string Slug { get; init; }
    public required bool CallerHasArticleManagementPermission { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.News.Article(Slug, CallerHasArticleManagementPermission);

    public TimeSpan Expiry => TimeSpan.FromDays(7);
}
```

**`CacheDescriptors.cs`**:
```csharp
public static CacheDescriptor Article(string slug, bool callerHasArticleManagementPermission)
    => new()
    {
        Key = $"neba:news:{slug}:article:scope:{(callerHasArticleManagementPermission ? "management" : "public")}",
        Tags = ["neba", "neba:news", $"neba:news:{slug}"]
    };
```

**`GetArticleEndpoint.cs`**:
```csharp
var query = new GetArticleQuery
{
    Slug = req.Slug,
    CallerHasArticleManagementPermission = User.HasAnyPermission(Permissions.ArticleManagementPermissions)
};
```

**`GetArticleQueryHandler.cs`** — drop the published/date predicate from the `Where(...)` when privileged, project `article.PublicationStatus.Name` into the DTO:

```csharp
var row = await _articles
    .Where(article => article.Slug == query.Slug
        && (query.CallerHasArticleManagementPermission
            || (article.PublicationStatus == PublicationStatus.Published && article.PublishDateUtc <= now)))
    .Select(article => new
    {
        article.Id,
        article.Slug,
        article.Title,
        article.Content,
        HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
        HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null,
        article.PublishDateUtc,
        PublicationStatus = article.PublicationStatus.Name,
        Attachments = article.Attachments
            .Where(attachment => !attachment.IsInline)
            .Select(attachment => new
            {
                attachment.DisplayName,
                attachment.File.Container,
                attachment.File.Path,
                attachment.File.ContentType
            }).ToList(),
        article.TournamentId
    })
    .SingleOrDefaultAsync(cancellationToken);

// ... unchanged, plus PublicationStatus = row.PublicationStatus on the returned ArticleDetailDto
```

### 1f. Tests (Phase 1)

Deferred to the dedicated test-writing pass, but plan should cover:
- `ListArticlesQueryHandlerTests` / `GetArticleQueryHandlerTests`: privileged caller sees drafts/scheduled/future-dated articles; unprivileged caller does not; `PublicationStatus` mapped correctly on both paths.
- Cache descriptor tests (or the existing cache-descriptor test pattern via `/cache-descriptor` skill) — confirm `ListArticles`/`Article` produce distinct keys for the two boolean values, same tags.
- Endpoint tests: `User.HasAnyPermission(...)` is exercised via a `ClaimsPrincipal` with/without the `News.DeleteArticle` claim, asserting the flag lands correctly on the constructed `Query`.
- `ClaimsPrincipalPermissionExtensionsTests`: true when any listed permission claim present, false when none, false for empty collection.
- Update `ArticleSummaryResponseFactory` / `ArticleDetailResponseFactory` and Verify snapshots for the new field (per 1c).

---

## Phase 2 — UI

### 2a. Publication-status badge (list + detail)

Add a small status badge, visible only to callers who satisfy the policy, using the same `<AuthorizeView Policy="...">` pattern already used for the delete button:

- **`ArticleCard.razor`** (`src/Neba.Website.Server/News/`) — in `.card-meta-row`, next to the existing date:
  ```razor
  <AuthorizeView Policy="@Permissions.CanManageArticlesPolicyName">
      <Authorized>
          <span class="article-status-badge article-status-badge--@StatusBadgeClass">@StatusBadgeLabel</span>
      </Authorized>
  </AuthorizeView>

  @code {
      private string StatusBadgeLabel =>
          Article.PublicationStatus != nameof(PublicationStatus.Published) ? "Draft"
          : Article.PublishDateUtc > DateTimeOffset.UtcNow ? "Scheduled"
          : "Published";

      private string StatusBadgeClass => StatusBadgeLabel.ToLowerInvariant();
  }
  ```
  (Avoid referencing the API-only `PublicationStatus` SmartEnum from `Neba.Website.Server`; compare against the response's `string` field directly, or add a small `ArticleSummaryResponse`-side constant if preferred.)
- **`NewsDetail.razor`** — same badge near the title, next to (or reusing) the existing `<AuthorizeView Policy="@Permissions.DeleteArticle.PolicyName">` block already there for the delete button. Swap in `Permissions.CanManageArticlesPolicyName` for a combined "management affordances" block if it reads cleaner to wrap both the badge and the delete button in one `<AuthorizeView>` — confirm preference when implementing, since `DeleteArticle`-specific policy and the new broader policy are both satisfied by the same claim today, but will diverge once `CreateArticle`/`UpdateArticle` exist and someone has, say, only `UpdateArticle` (no delete permission) — that caller should see the status badge but not the delete button.
- **`ArticleCard.razor.css`** / detail page CSS — add `.article-status-badge` styles (draft/scheduled/published variants); small pill, consistent with existing card meta styling.

### 2b. List page — no structural changes needed

`NewsList.razor`'s hero-card-plus-grid layout and pagination already just render whatever `ListArticlesAsync` returns — since privileged callers now get drafts/scheduled articles mixed into the same paginated, `PublishDateUtc`-ordered response (per 1d), no new branching is needed here beyond the badge from 2a. Confirm during implementation that a draft with no meaningful `PublishDateUtc` (or a default/min value) doesn't look wrong sorted to the very end/start of the list — if `PublishDateUtc` is required non-null today, this is moot.

### 2c. Detail page — drafts become directly viewable by URL for privileged callers

Since `GetArticleQueryHandler` (1e) now returns a draft/scheduled article to a privileged caller instead of `ArticleErrors.ArticleNotFound`, `NewsDetail.razor` needs no new code path to *fetch* it — `NewsApi.GetArticleAsync(Slug, ct)` just succeeds where it previously 404'd. Only the badge (2a) surfaces the distinction to the viewer.

### 2d. Tests (Phase 2)

Deferred to the dedicated test-writing pass, but plan should cover:
- bUnit: badge visible/hidden per policy (authorized vs. not), correct label for Draft/Scheduled/Published.
- bUnit: `NewsDetail` renders successfully for a draft article response (mock `INewsApi` returning a draft `ArticleDetailResponse`) when the viewer is authorized.
- E2E (if `tests/e2e/` has a News suite): admin/webmaster sees drafts in the list and can open one by direct URL; anonymous/member does not see the badge and gets 404 for a draft slug.

---

## Open items to confirm before/while implementing

1. Exact policy registration call shape in `AccountConfiguration.cs` (`AddAuthorization(...)` vs. `AddAuthorizationBuilder()`) — match whatever's already there.
2. Whether `NewsDetail.razor`'s badge and delete-button `<AuthorizeView>` blocks should be merged into one `CanManageArticlesPolicyName`-gated wrapper or kept separate (see 2a note) — matters once `CreateArticle`/`UpdateArticle` exist.
3. Confirm `Article.PublishDateUtc` is non-nullable today (referenced in 2b) — if nullable, sorting/display logic for drafts-with-no-date needs a decision.
