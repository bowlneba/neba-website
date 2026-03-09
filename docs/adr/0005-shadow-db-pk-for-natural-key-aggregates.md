# ADR-0005: Shadow Database PK for Natural-Key Aggregates

## Status

Accepted

## Context

Some aggregates have stable, always-present natural keys assigned by an external authority. `BowlingCenter` is the first such case — its USBC Certification Number is permanent, externally issued, and is the true domain identity.

An initial architecture sketch in `backend.md` showed `BowlingCenterId` as a wrapper record around the certification number string — mirroring the `BowlerId` surrogate pattern. This raised a design question: should natural-key aggregates expose a typed ID wrapper, or use the natural key value object directly as their identity?

### Options Considered

#### Option 1: Typed ID wrapper (`BowlingCenterId`)

```csharp
public record BowlingCenterId
{
    public string Value { get; }
    private BowlingCenterId(string value) => Value = value;
    public static BowlingCenterId FromCertification(string certNumber) => new(certNumber);
    public static BowlingCenterId Synthetic() => new($"HISTORICAL-{Ulid.NewUlid()}");
    public bool IsSynthetic => Value.StartsWith("HISTORICAL-");
}
```

- **Pro**: Mirrors the surrogate ID pattern; consistent surface area across aggregates
- **Con**: Redundant — `BowlingCenterId` and `CertificationNumber` represent the same concept with different names
- **Con**: Requires a synthetic fallback scheme (`HISTORICAL-{ulid}`) for historical centers without a known certification number, adding complexity and a concept the domain doesn't really have
- **Con**: Domain code that works with certification numbers must constantly unwrap/re-wrap the value

#### Option 2: Natural key value object directly (no wrapper ID type)

- `BowlingCenter.CertificationNumber` is the domain identity — no separate `Id` property, no `BowlingCenterId` type
- The database PK is a shadow `int` property (`db_id`), configured in EF Core only — never exposed on the domain model
- Cross-aggregate references use `CertificationNumber` in domain code; EF Core maps these to the shadow int FK in the database

- **Pro**: No redundancy — one concept, one name
- **Pro**: Domain code uses `CertificationNumber` directly, making intent explicit
- **Pro**: Eliminates the synthetic `HISTORICAL-{ulid}` scheme; placeholder centers are handled by `CertificationNumber` itself (via its `IsPlaceholder` property)
- **Pro**: Domain model is free of persistence concerns — `db_id` is invisible at the domain level
- **Con**: EF Core configuration is more explicit — shadow PK and FK mappings require fluent configuration rather than convention
- **Con**: Cross-aggregate references via a value object (rather than a primitive or typed ID) require value converter setup in EF Core

## Decision

**Option 2**: Natural-key aggregates expose the natural key value object directly. No wrapper ID type is created.

### Rules

1. A strongly-typed ULID ID (e.g., `BowlerId`) is created **only** for aggregates that have no stable natural key.
2. Aggregates with a natural key (e.g., `BowlingCenter`) use the natural key value object as their domain identity. No `{Aggregate}Id` type is created.
3. The database PK for all aggregates is a shadow `int` property named `db_id`, configured in EF Core entity configuration. It is never a property on the domain class.
4. Cross-aggregate references in the domain model use the natural key value object (e.g., `CertificationNumber VenueCertificationNumber`). EF Core entity configuration maps this to the shadow int FK via a value converter.

## Consequences

### Positive

- `BowlingCenter` has `CertificationNumber CertificationNumber` as its single identity property — no separate `Id` that means the same thing
- Eliminates the `BowlingCenterId` wrapper type and the `HISTORICAL-{ulid}` synthetic fallback (the architecture doc sketch of these is superseded by this decision)
- Domain code that references a bowling center reads as intent (`center.CertificationNumber`) rather than an opaque ID
- The `db_id` shadow property keeps persistence concerns out of the domain model entirely

### Negative

- EF Core configuration for `BowlingCenter` and any aggregate that references it requires explicit fluent API setup (shadow properties, value converters for the FK relationship)
- The asymmetry between natural-key and surrogate-key aggregates (one has an `Id` property, the other doesn't) is a small cognitive gap — mitigated by this ADR

### Supersedes

The `BowlingCenterId` sketch in `backend.md` (Entity Identity Strategy section) is superseded by this decision.
