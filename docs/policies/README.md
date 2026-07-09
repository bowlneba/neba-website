# Authorization Policies

Reference for every ASP.NET Core authorization policy registered in the system. See [ADR-0008](../adr/0008-policy-documentation-structure.md) for why this exists and how it's structured.

Source of truth is code — this table describes it for reviewers and help-doc authors:

- Policies are registered in `src/Neba.Api/Security/SecurityConfiguration.cs` (and mirrored in `src/Neba.Website.Server/Account/AccountConfiguration.cs` for Blazor `<AuthorizeView>` use).
- Permissions are defined in `src/Neba.Api.Contracts/Security/Permission.cs`.
- Roles and their permission grants are defined in `src/Neba.Api/Security/Domain/Roles.cs` / `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`.

## Policies

| Policy | Kind | Satisfied by | Enforced at | Details |
| --- | --- | --- | --- | --- |
| `Authenticated` | Static | Any signed-in user (no specific permission/role) | `LogoutEndpoint`, `GetCurrentUserEndpoint` | — |
| `Permission:{value}` | Dynamic (one per `Permissions` value, resolved by `PermissionPolicyProvider`) | Caller holds the single named permission claim (e.g. `Permission:Read`, `Permission:News.DeleteArticle`) | Any endpoint calling `.Policies(Permissions.X.PolicyName)` | — |
| `CanManageArticles` | Static, OR-of-many | Any permission in `Permissions.ArticleManagementPermissions` (currently just `News.DeleteArticle`) | Registered via `AddNebaPolicies()` (API and Website); drives status-badge visibility in `ArticleCard.razor`, `NewsDetail.razor`, `NewsList.razor` — the delete action itself still gates on `Permission:News.DeleteArticle` directly, not this policy | [can-manage-articles.md](can-manage-articles.md) |

## When to update this file

- **New policy added** (a new `AddPolicy(...)` call): add a row here. If it's a straightforward single-permission or single-role check, the row is enough — no dedicated file needed.
- **New policy has real nuance** (OR/AND-of-many semantics, exceptions to explain, reasoning a reviewer would otherwise have to reverse-engineer from `SecurityConfiguration.cs`): add a dedicated `docs/policies/<policy-name>.md` and link it from the row.
- **Existing policy's semantics change** (e.g. a permission added to `ArticleManagementPermissions`): update the row and, if one exists, the dedicated file.
