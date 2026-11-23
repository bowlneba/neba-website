---
name: write-tests
description: Choose the right kind of test for code in this repo (unit, integration, bUnit, Playwright), then write it with the repo's traits, factories, strict mocks and AAA layout. Use when adding tests for new or changed code or before starting a bug fix.
---

# Write tests

Read `docs/learnings/testing.md` and "Testing Requirements" in `CLAUDE.md`. The `test-writer` agent does this work; this is the quick procedure.

## Choose the type

| Code under test | Test | Copy from |
|---|---|---|
| Aggregate, entity, value object | Unit | `tests/Neba.Api.Tests/Features/Sponsors/Domain/SponsorTests.cs` |
| Handler (uses `AppDbContext`) | **Integration** on the Postgres fixture, not mocks | `Features/Sponsors/CreateSponsor/CreateSponsorCommandHandlerTests.cs` |
| Endpoint | Unit with `Factory.Create<TEndpoint>()` and a Verify snapshot | `Features/Sponsors/ListActiveSponsors/ListActiveSponsorsEndpointTests.cs` |
| Endpoint authorization | Integration | `Features/Sponsors/CreateSponsor/CreateSponsorEndpointAuthorizationTests.cs` |
| Validator, summary | Unit | `CreateSponsorRequestValidatorTests.cs`, `CreateSponsorSummaryTests.cs` |
| EF configuration or migration | Integration | `tests/Neba.Api.Tests/Database/` |
| Blazor page or component | bUnit | `tests/Neba.Website.Tests/Sponsors/CreateSponsorTests.cs` |
| Real browser plus HTTP flow | Playwright | `tests/e2e/`; table in `docs/learnings/testing.md` |

A test goes in the folder that mirrors its source folder.

## Every test

- Traits: `[UnitTest]` or `[IntegrationTest]`, and `[Component("Feature")]`.
- `DisplayName` on every `Fact` and `Theory`.
- `// Arrange`, `// Act`, `// Assert` in every method.
- `MockBehavior.Strict`. `NullLogger<T>.Instance`, or `FakeLogger<T>` to assert logs. Shouldly only.
- Data from `Neba.TestFactory`, never `new` on an entity. A missing factory: use the `test-factory-generator` skill. `Create()` with no arguments must give a persistable entity.
- Seed `Bogus` only when the values matter, and use a distinct seed per test.
- Null input on a non-nullable parameter: `#nullable disable` around the test, not `null!`.
- Never change process-wide static state in an integration test.

## Bug fix

Write the failing test first, run it, and confirm it fails for the right reason. Then make the smallest fix and run the full suite.

## Run

```
dotnet test --filter-trait "Component=Sponsors"
dotnet test --filter-trait "Category=Unit"
```

The runner is MTP, so VSTest `--filter` does not work. A project with no matching tests exits 8, which is not a failure; read each project's summary line. Integration tests need Docker.
