# Value objects, typed IDs and SmartEnums

Read this when you add a value object, a strongly typed ID or a SmartEnum.

## Value objects

| `sealed record class` | `readonly record struct` |
|---|---|
| Persisted by EF Core (owned or complex type) | Not persisted (commands, transient values) |
| Private constructor plus `Create` factory | Simple values |

EF binds to private constructors on classes. A struct materializes as `default(T)` and skips them.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency) => (Amount, Currency) = (amount, currency);

    public static ErrorOr<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Error.Validation("Money.Amount.Negative", "Amount cannot be negative");
        return new Money(amount, currency);
    }
}
```

## Strongly-typed IDs

Use the `[StronglyTypedId("ulid-full")]` source generator (`ulid-full.typedid`). Example: `Features/Sponsors/Domain/SponsorId.cs`.

## SmartEnums

Domain enumerations use SmartEnum types. Cached query DTOs hold primitives, never SmartEnum instances. For which serialization form (name or value) each API uses, see the new-endpoint skill.
