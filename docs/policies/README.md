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
| `CanManageArticles` | Static, OR-of-many | Any permission in `Permissions.ArticleManagementPermissions` (`News.CreateArticle` or `News.DeleteArticle`) | Registered via `AddNebaPolicies()` (API and Website); drives status-badge visibility only in `ArticleCard.razor`, `NewsDetail.razor`, `NewsList.razor` — every actual page/action (Create Article, Delete) gates on the specific `Permission:News.CreateArticle` / `Permission:News.DeleteArticle` policy matching its own endpoint, not this one | [can-manage-articles.md](can-manage-articles.md) |
| `CanManageSponsors` | Static, OR-of-many | Any permission in `Permissions.SponsorManagementPermissions` (`Sponsors.CreateSponsor` or `Sponsors.EditSponsor`) | Registered via `AddNebaPolicies()` (API and Website); drives Active/Inactive badge and Inactive Sponsors section visibility only in `Sponsors.razor` and `SponsorDetail.razor` — the Create/Edit Sponsor pages and actions gate on the specific `Permission:Sponsors.CreateSponsor` / `Permission:Sponsors.EditSponsor` policy matching their own endpoint, not this one | [can-manage-sponsors.md](can-manage-sponsors.md) |
| `CanManageTournaments` | Static, OR-of-many | Any permission in `Permissions.TournamentManagementPermissions` (currently only `Tournaments.CreateTournament`) | Registered via `AddNebaPolicies()` (API and Website), but not currently referenced by any endpoint or `<AuthorizeView>` — every tournament action gates on its own specific `Permission:{value}` policy instead (e.g. `Permission:Tournaments.CreateTournament`). Currently dead code. | — |

## When to update this file

- **New policy added** (a new `AddPolicy(...)` call): add a row here. If it's a straightforward single-permission or single-role check, the row is enough — no dedicated file needed.
- **New policy has real nuance** (OR/AND-of-many semantics, exceptions to explain, reasoning a reviewer would otherwise have to reverse-engineer from `SecurityConfiguration.cs`): add a dedicated `docs/policies/<policy-name>.md` and link it from the row.
- **Existing policy's semantics change** (e.g. a permission added to `ArticleManagementPermissions`): update the row and, if one exists, the dedicated file.
