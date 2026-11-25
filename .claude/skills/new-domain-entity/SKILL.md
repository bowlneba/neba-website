---
name: new-domain-entity
description: Add an aggregate root or a child entity to a feature's Domain folder - typed ID, always-valid Create factory, aggregate method, errors, ubiquitous-language entry, and test factory. Use when modeling a new domain concept before its endpoints and table. Not for endpoints (new-endpoint), tables and migrations (add-ef-migration) or glossary audits (ubiquitous-language-review).
---

# New domain entity

Read `docs/learnings/domain-design.md` and the `vsa-ddd` skill. The `backend-api` agent does this work; this is the procedure. A table change is a hand-off to `efcore`.

## Copy this

| Need | Copy from `src/Neba.Api/Features/` |
|---|---|
| Aggregate root | `Sponsors/Domain/Sponsor.cs` |
| Typed ID | `Sponsors/Domain/SponsorId.cs` |
| Child entity with an `internal static ErrorOr<T> Create` | `News/Domain/ArticleAttachment.cs` |
| Aggregate method that calls a child's `Create` | `Seasons/Domain/Season.cs` (`AddHighAverageWinner`, with `HighAverageAward.Create`) |
| Errors | `Sponsors/SponsorErrors.cs`, or a `*Errors.cs` in `Domain/` |
| Glossary entry | `docs/ubiquitous-language.md` ("Article") |

## Steps

1. **Glossary first.** Add the term to `docs/ubiquitous-language.md` in the existing format: Definition, Properties table, Business Rules, and an "In Code" block (namespace, type, identity type). Use that exact name in the code. The `ubiquitous-language-review` skill checks both directions.
2. **Typed ID.** `[StronglyTypedId("ulid-full")] public readonly partial struct XId;` with a short XML summary.
3. **Aggregate root.** A `public sealed class` deriving from `AggregateRoot`. `required` and `init` for identity, `private set` for state, no public setters, no public mutable collections. A collection navigation uses a `List<T>` backing field and exposes `IReadOnlyCollection<T>` (the `= []` rule in `docs/learnings/backend.md`). Reference other aggregates by typed ID.
4. **Behavior on the aggregate.** State changes are methods that return `ErrorOr`. Do not put rules in a handler.
5. **Child entities.** Give each an `internal static ErrorOr<T> Create(...)` for its own invariants. The aggregate's add or assign method (for example `Season.AddHighAverageWinner`) calls it and adds aggregate-level checks. Never validate child fields on the aggregate and never make `Create` public.
6. **Cross-aggregate data.** Live data owned by another aggregate comes in as a parameter; the handler queries it. A per-instance formula coefficient persists on the aggregate. The handler never computes the formula.
7. **Errors.** An `internal static class {Type}Errors`. Codes are `Entity.Rule`. `Error.Validation` for bad input, `Error.Conflict` for state that blocks valid input.
8. **XML summaries** are short IntelliSense blurbs. The full definition belongs in the glossary.
9. **Factory.** Add a test factory in `tests/Neba.TestFactory/` (`test-factory-generator`). `Create()` with no arguments must produce a persistable entity.
10. **Hand-offs.** The table and configuration go to `efcore`. The tests go to `test-writer`. An endpoint is a `new-endpoint` task.

## Gotchas

- An EF collection navigation on an `IReadOnlyCollection<T>` auto-property with `= []` becomes a fixed-size array. Use a `List<T>` backing field and add an `ICollection<T>.Add` regression test.
- A domain event goes through `IDomainEventPublisher`, one `IDomainEventJob<T>` per event, and carries typed IDs only. The outbox (#56) is deferred.
- Calling `Create` on a child from outside the aggregate means someone made it public. Make it `internal`.
- A new `Create()` factory default must give a persistable entity with no arguments, including any required complex property.

## Checklist

- [ ] Glossary entry, and the code uses its name
- [ ] Typed ID, sealed aggregate, no public setters
- [ ] Child `Create` is `internal` and the aggregate calls it
- [ ] Rules sit in the domain, not in a handler
- [ ] Errors class and the 409/422 choice
- [ ] Factory and hand-offs listed
