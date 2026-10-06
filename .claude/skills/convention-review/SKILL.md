---
name: convention-review
description: Check changed code against this repo's own conventions (page titles and render modes, EF collection navigations, endpoint authorization, strict mocks, test traits, route shape, PII on log parameters, clean-code smells such as Helper/Manager names, #region, async void, bool flag parameters). Use when the user asks to "review conventions", "check my changes against our standards", or before a PR. Run it in a NEW session, not the one that wrote the code. Do NOT use for correctness bugs or security (use /code-review and /security-review).
---

# Convention review

Run in a fresh session. The session that wrote the code defends its choices; a new one reads the code.

## Step 1. Scope

List changed files: `git diff --name-only main...HEAD` (add `git status --short` for uncommitted work). Limit every search below to those files. If the user names a path, use that instead.

## Step 2. Run the detection

Run each command against the scoped files. A hit is a candidate, not a verdict; read the code before reporting it.

| # | Rule | Detection |
|---|------|-----------|
| 1 | No `async void` | `rg -n --glob '*.cs' '\basync\s+void\b'` |
| 2 | No `#region` | `rg -n --glob '*.cs' '^\s*#region'` |
| 3 | No Manager/Helper/Utils type names | `rg -n --glob '*.cs' '\b(class\|record\|struct\|interface)\s+\w*(Manager\|Helpers?\|Utils?\|Utility\|Utilities)\b'` |
| 4 | No bool flag parameters on non-test methods | `rg -n --glob '*.cs' '\((.*,\s*)?bool \w+[,)]' src` |
| 5 | Routable pages have `<PageTitle>` | for each changed `*.razor` with `@page`: `rg -L '<PageTitle' <file>` |
| 6 | Routable pages declare `@rendermode` (except Login, Logout, Error, NotFound) | `rg -L '@rendermode' <file>` |
| 7 | No `= []` on `IReadOnlyCollection`/`IEnumerable`/`IReadOnlyList` auto-properties in `src/Neba.Api` (EF fixed-size array bug) | `rg -n --glob '*.cs' 'I(ReadOnly)?(Collection\|List)<[^>]+> \w+ \{ get; init; \} = \[\]' src/Neba.Api` |
| 8 | Every endpoint sets authorization explicitly | `rg -L 'AllowAnonymous\|Roles\(\|Policies\(' <changed *Endpoint.cs>` |
| 9 | No `/api` or `/v1` in routes | `rg -n --glob '*.cs' '(Get\|Post\|Put\|Patch\|Delete)\("/?(api\|v[0-9])' src/Neba.Api` |
| 10 | Mocks are `MockBehavior.Strict` | `rg -n --glob '*.cs' 'new Mock<[^>]*>\(\)' tests` |
| 11 | Tests carry `[UnitTest]`/`[IntegrationTest]` and `[Component(...)]` | `rg -L '\[(UnitTest\|IntegrationTest)\]' <changed *Tests.cs>`; same for `\[Component\(` |
| 12 | Fact/Theory have `DisplayName` | `rg -n --glob '*Tests.cs' '\[(Fact\|Theory)\]'` |
| 13 | AAA comments in tests | `rg -L '// Arrange' <changed *Tests.cs>` |
| 14 | No FluentAssertions, no mocked `ILogger` | `rg -n 'FluentAssertions\|Mock<ILogger' tests src` |
| 15 | Names, emails, phones, addresses in `[LoggerMessage]` parameters carry `[PersonalData]` or `[PrivateData]` | `rg -n -A4 '\[LoggerMessage' <changed .cs>`; read each parameter list |
| 16 | Methods returning collections never return `null` | `rg -n --glob '*.cs' '(List\|IEnumerable\|IReadOnly\w+)<[^>]+>\? \w+\('` |
| 17 | Data-entry pages use `DirtyFormGuard`; form labels use `FormLabel` | `rg -L 'DirtyFormGuard' <changed pages with EditForm>` |
| 18 | New `[Fact]`/`[Theory]` use Shouldly | `rg -n 'Assert\.' tests --glob '*.cs'` |

Rules 5-6 and 17 need the file list from step 1, not a whole-repo search.

## Step 3. Judgement pass

Greps cannot catch these; read the diff for them:

- Handler computes a domain formula and passes the result in, instead of the aggregate owning it.
- Validator does a DB lookup or enforces a business rule.
- Command throws for a business rule instead of returning `ErrorOr`.
- `Error.Validation` vs `Error.Conflict` chosen wrongly (retry test in CLAUDE.md).
- Query returns a domain entity, or a cached DTO holds a SmartEnum.
- A new query that duplicates one that already exists.
- Interface extracted for a single implementation with no test or cross-module need.
- Names that say nothing about the job (`Process`, `Handle` helpers, `Data`, `Info`).

## Step 4. Report

Write one table with a row for every numbered rule above and every judgement item, whether or not it found anything:

| Rule | Result | Files |
|------|--------|-------|
| 1 async void | OK | - |
| 7 `= []` navigation | VIOLATION | src/Neba.Api/... :42 |

Then list violations ordered by effort to fix, each with the file, line and the one-line fix. Do not edit code unless the user asks.
