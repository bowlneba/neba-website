# ADR-0004: Error Code Naming Convention

## Status

Accepted

## Context

The codebase uses the `ErrorOr` library for the Result pattern. `Error` objects carry a `Code` string that flows through the application and surfaces as `errorCode` in ProblemDetails responses. As the first `*Errors` class was introduced (`DocumentErrors`), we needed a consistent naming convention for these codes before they proliferate across domains.

### Requirements

- Codes must be **human-readable** in ProblemDetails JSON responses (consumed by Blazor frontend and potentially third-party callers)
- Codes must be **greppable** across the codebase — searching for a code from a log or API response should lead directly to its definition
- Codes must be **self-describing** — a developer seeing a code in a log should understand which entity and what went wrong without looking up the definition
- Codes must work across all layers: domain value objects, application handlers, and infrastructure services

### Options Considered

#### Option 1: `Domain.Entity.ErrorCode` (three segments)

Examples: `Documents.Document.NotFound`, `Tournaments.Squad.AtCapacity`

- **Pro**: Maximally explicit, includes bounded context
- **Con**: Verbose. The domain is already implied by the namespace, API route, and feature folder. Three segments add noise in logs and ProblemDetails without adding information that isn't already available from context

#### Option 2: `Entity.ErrorCode` (two segments)

Examples: `Document.NotFound`, `Squad.AtCapacity`, `Money.Amount.Negative`

- **Pro**: Concise, self-describing, easy to grep, natural to write (the first code written — `Document.NotFound` — followed this pattern instinctively)
- **Pro**: Aligns with how ErrorOr examples and documentation structure codes
- **Con**: Potential for collision if two domains have identically-named entities (unlikely given the project's bounded contexts)

#### Option 3: `entity.errorCode` (lowercase dot-notation)

Examples: `document.notFound`, `money.amount.negative`

- **Pro**: Feels like a property path, common in validation libraries
- **Con**: Inconsistent with the PascalCase conventions used everywhere else in C#. Looks like JSON property paths rather than error identifiers

#### Option 4: Flat codes with no separator

Examples: `DocumentNotFound`, `SquadAtCapacity`

- **Pro**: Simple
- **Con**: Hard to parse visually, no structure, doesn't scale as error count grows

## Decision

**`Entity.ErrorCode`** (Option 2) — PascalCase, dot-separated, two segments.

### Convention

| Layer | Pattern | Example |
| ----- | ------- | ------- |
| Application/Domain errors | `Entity.ErrorCode` | `Document.NotFound`, `Tournament.AlreadyStarted` |
| Value object validation | `Entity.Property.Rule` | `Money.Amount.Negative`, `Money.Currency.InvalidFormat` |
| Infrastructure errors | `Service.Operation.ErrorKind` | `GoogleDrive.GetDocument.HttpError`, `Api.GetFoo.Cancelled` |

### Rules

1. **Use PascalCase** for all segments
2. **First segment** is the entity or service name (singular): `Document`, `Tournament`, `Squad`, `Money`
3. **Last segment** describes the error condition: `NotFound`, `AtCapacity`, `AlreadyStarted`, `Negative`
4. **Value objects** may use three segments when the error is property-specific: `Money.Amount.Negative`
5. **Infrastructure services** follow `Service.Operation.ErrorKind` — this is already established in `ApiExecutor` and should be preserved
6. **Error classes** are named `{Entity}Errors` and live in the Application layer alongside the handlers that use them (e.g., `DocumentErrors.cs` in `Documents/`)

### Examples

```csharp
// Application layer — DocumentErrors.cs
internal static class DocumentErrors
{
    public static Error DocumentNotFound(string documentName)
        => Error.NotFound(
            code: "Document.NotFound",
            description: $"Document with name '{documentName}' was not found.");
}

// Domain value object
public static ErrorOr<Money> Create(decimal amount, string currency)
{
    if (amount < 0)
        return Error.Validation("Money.Amount.Negative", "Amount cannot be negative");
}

// Infrastructure service (existing pattern, unchanged)
return Error.Failure("GoogleDrive.GetDocument.HttpError", "Request failed.");
```

## Consequences

### Positive

- Every error code is immediately searchable — grep for `"Document.NotFound"` and find the exact definition
- ProblemDetails responses contain meaningful, structured error codes that clients can programmatically match on
- Convention is lightweight — no custom types or abstractions needed beyond what ErrorOr provides
- Natural to write — the first error code in the codebase (`Document.NotFound`) already followed this pattern

### Negative

- No compile-time enforcement of the naming convention (relies on code review)
- Value object codes in the architecture doc examples previously used lowercase (`money.amount`) and needed updating for consistency

### Mitigation

- PR review guidelines updated to flag non-conforming error codes
- Architecture doc examples updated to match the convention
