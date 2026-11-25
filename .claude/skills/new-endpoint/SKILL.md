---
name: new-endpoint
description: Add a Fast Endpoints use case to Neba.Api (query, command, edit or delete) with its contracts, Refit method, factories, tests and e2e mock. Use when asked to add or change an API endpoint. Not for modeling a domain type (new-domain-entity, vsa-ddd), a table change (add-ef-migration), a Blazor page (new-blazor-page) or background work (new-background-job). Usage - /new-endpoint <description: feature, verb, route, who may call it>
---

# New endpoint

Add one use case. Copy a real slice, do not write from memory. Read `CLAUDE.md` (API endpoint checklist), `docs/architecture/backend.md` and the `vsa-ddd` skill first.

## Copy this

| Case | Copy from `src/Neba.Api/Features/` |
|---|---|
| Query, no input | `Sponsors/ListActiveSponsors` |
| Query with a request and validator | `Sponsors/GetSponsorDetail` |
| Command (create) | `Sponsors/CreateSponsor` |
| Command (edit, adds a 404) | `Sponsors/EditSponsor` |
| Delete | `News/DeleteArticle` |
| Endpoint group | `Sponsors/SponsorsEndpointGroup.cs` |
| Contracts and Refit | `src/Neba.Api.Contracts/Sponsors/` (`ISponsorsApi.cs`) |
| Factories | `tests/Neba.TestFactory/Sponsors/` (use `test-factory-generator`) |
| Tests | `tests/Neba.Api.Tests/Features/Sponsors/` |

## Steps

1. **Folder.** `Features/{Feature}/{Action}{Entity}/` with Endpoint, Summary, Command or Query, Handler, DTO, and a `RequestValidator` when the request has rules. Create `{Feature}EndpointGroup` if the feature has none. Name the DTO after the use case. Never reuse another use case's DTO.
2. **Contracts.** Add the Request (wraps Input for commands) and Response records in `Neba.Api.Contracts/{Feature}/{UseCase}/`, each with XML `<summary>` and `<example>`. Add the method to `I{Feature}Api`. A GET with query params gets an `internal` request class in the API project with `[BindFrom("key")]` on every property.
3. **Authorization.** Always explicit: `AllowAnonymous()`, `Roles()` or `Policies()`. A new permission goes in the permissions catalog first (`docs/architecture/permission-based-security.md`).
4. **Endpoint.** `internal sealed`. Version with `Options(WithVersionSet(...).MapToApiVersion(...))`, never in the path. Routes have no `/api` prefix. Standard REST: id in the path, the verb names the action.
5. **Description.** `WithName()` and `WithTags()`. Declare every status code with `Produces` and `ProducesProblemDetails`.
6. **Handler.** Commands return `ErrorOr<T>`. Queries return DTOs. Inject `AppDbContext`, no repository.
7. **Factories.** One for every new type: internal DTOs, response contracts, request types. (An agent running this skill lists them for `test-writer`.)
8. **Tests.** Endpoint, handler, validator, summary and authorization tests. Rules: `docs/learnings/testing.md`. If a Blazor page calls the new method, add a bUnit test using `StubApiResponse<T>`.
9. **E2E mock.** A new `required` field on a Contracts DTO needs a matching change in `tests/e2e/mock-api/mock-api-server.ts`, or Playwright fails every time.
10. **Help and policy docs.** Run `help-documentation` or `policy-documentation` if the change is user-facing or adds a policy.

## Status codes

| Code | Source | Meaning |
|---|---|---|
| 400 | `Validator<T>`, automatic | Shape is wrong (missing, too long, bad format) |
| 422 | `Error.Validation` from the handler | Well-formed input breaks a domain rule |
| 409 | `Error.Conflict` from the handler | Input is valid, but current state blocks it. Retry test: the same payload could succeed after a state change. |
| 404 | `Error.NotFound` from the handler | Target is missing (get and edit only) |
| 204 | Delete handler | Also returned when the target is already gone |

An endpoint with a `RequestValidator` must declare 400 in `Description()`, even though the endpoint code never sees it. An endpoint with no validator omits 400. Send 409 and 422 from `HandleAsync` with `AddError()` and `Send.ErrorsAsync(code, ct)`. See `Sponsors/SponsorMutationResultSender.cs`.

## SmartEnum serialization

- GET responses project the SmartEnum with `.Name`.
- Commands (POST, PUT, PATCH) parse the request value with `.FromValue`, never `.FromName`.
- Exceptions that use the code both ways: `UsState`, `CanadianProvince`, `Country`, `Permissions`, `TournamentType`, `PatternLength`, `PatternRatio`. Roles use the name string on purpose.
- Cached query DTOs hold primitives, never SmartEnum instances.

Rationale: "SmartEnum Serialization" in `.github/instructions/pull-request-review.instructions.md`.

## Gotchas

- A new `required` field on a Contracts DTO breaks the e2e mock in a way that looks like flakiness. Update `tests/e2e/mock-api/mock-api-server.ts` in the same commit.
- Refit 14: register clients with `AddRefitGeneratedClient<T>()` (see `ApiServicesConfiguration.cs`), not `AddRefitClient<T>()`. Refit's `[Url]` attribute collides with DataAnnotations' `[Url]`.
- A new permission goes to Admin and Webmaster. Webmaster needs an explicit entry in `SecurityRoleSeeder`. Admin-only is a question for the user.
- Reuse an existing query and project down before adding a lightweight one. Name the DTO `*Summary`, not after the query or route.
- `EF.Functions.ILike` takes an explicit escape character (see `SearchBowlersQueryHandler`).
- Delete handlers return 204 when the target is already gone. `NotFound` stays valid for get and edit.
- A list or collection method returns an empty collection, never `null`.

## Checklist

- [ ] Group exists, endpoint is `internal sealed`, version set and mapped
- [ ] Authorization explicit, permission in the catalog if new
- [ ] `WithName`, `WithTags`, every status code declared (400 only with a validator)
- [ ] Validator checks structure only (no DB lookups, no business rules)
- [ ] Contracts and Refit updated, XML docs on public types
- [ ] SmartEnum rules followed
- [ ] Factories for every new type
- [ ] Endpoint, handler, validator, summary, authorization tests; snapshot accepted
- [ ] e2e mock updated if a Contracts DTO changed
- [ ] Build and run `dotnet test --filter-trait "Component={Feature}"`
