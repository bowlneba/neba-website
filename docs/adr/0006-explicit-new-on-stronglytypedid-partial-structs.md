# ADR-0006: Explicit `New()` Factory on StronglyTypedId Partial Structs

## Status

Accepted

## Context

All domain IDs use `StronglyTypedId` with the `ulid-full` custom template. The template generates the full struct implementation — constructors, converters, equality, etc. — including a `New()` factory method:

```csharp
public static PLACEHOLDERID New() 
    => new PLACEHOLDERID(Ulid.NewUlid());
```

During mutation testing with Stryker.NET, the domain project is recompiled in-memory by Stryker's Roslyn compilation pipeline so it can inject mutation guards (`#if STRYKER_MUTANT_X`). This in-memory compilation invokes source generators, but the `StronglyTypedIds` generator requires the `.typedid` template as an `AdditionalFile`. Stryker does not pass `AdditionalFiles` to generators during its Roslyn workspace construction, so the generator runs but produces no output.

The result: `New()` does not exist in Stryker's compilation context. Any domain source file that calls `SomeId.New()` causes Stryker to log:

```bash
[WRN] An unidentified mutation resulted in a compile error (CS0117: 'SeasonAwardId' does not contain a definition for 'New')
[INF] Safe Mode! Stryker will remove all mutations in CreateOpen and mark them as 'compile error'.
[FTL] Stryker.NET could not compile the project after mutation.
```

When this happens, every mutation in the affected methods is blanket-marked as "compile error," the run crashes, and CI fails.

### Why Only Domain Source Files — Not Tests?

Test files call `BowlerId.New()`, `SeasonId.New()`, etc., but these calls don't trigger the compile failure. Test files reference the **pre-compiled** domain assembly (via project reference), which includes the source-generator output. Only domain source files are re-compiled in-memory by Stryker.

Domain source files that call `SeasonAwardId.New()` (e.g., `BowlerOfTheYearAward.cs`) are part of the domain project being mutated — so they go through Stryker's source-generator-free compilation path and fail.

### Options Considered

#### Option 1: Stryker `ignore-methods` config

The config already includes `"*.New"` in `ignore-methods`. This tells Stryker to skip mutations of *arguments* passed to those calls — but it does not prevent the compilation from needing to resolve `New()`. The compilation still fails.

#### Option 2: Stryker disable pragmas

`// Stryker disable all` on every `*.New()` call site prevents mutation *generation* for those lines, but Stryker must still compile the un-mutated code. Since `New()` is absent from Stryker's compilation context, the build fails before any mutations are injected.

#### Option 3: Exclude the ID files from mutation (rejected — already done, unrelated)

ID files (`*Id.cs`) are already excluded via `"!**/*Id.cs"` in the `mutate` array. Excluding them from mutation does not affect whether they appear in the compilation. The compile error is in the *caller* (award files), not the ID definition.

#### Option 4: Remove `Value`, the private constructor, and `New()` from the template; add all three explicitly to each partial struct (chosen)

Move the core trio from the source-generated template into the hand-written partial struct declaration for each ID type. Since these members now live in real `.cs` source (not generator output), Stryker's Roslyn compilation always finds them.

The hand-written partial struct body for each ID:

```csharp
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonAwardId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private SeasonAwardId(Ulid value) => Value = value;

    /// <summary>Creates a new <see cref="SeasonAwardId"/> with a randomly generated ULID value.</summary>
    public static SeasonAwardId New() => new(Ulid.NewUlid());
}
```

The template retains everything else (interfaces, Parse/TryParse, converters, operators, etc.) and continues to call `new PLACEHOLDERID(ulid)` — valid because the private constructor is now in the hand-written declaration of the same partial type.

This requirement applies to **all** structs using the template, including test helper structs (e.g., a `TestId` declared inside a test class).

### Why all three members, not just `New()`

Moving only `New()` to hand-written source was the original intent, but `New()` calls the private constructor — which is still template-generated. Stryker's compilation still can't find it, causing the same crash. All three members must travel together:

- `Value { get; }` — required by the private constructor body (`=> Value = value`)
- `private PLACEHOLDERID(Ulid value)` — required by `New()` and by generated Parse/TryParse/FromGuid calls
- `New()` — required by domain source files that create new IDs

## Decision

Remove `Value { get; }`, `private PLACEHOLDERID(Ulid value)`, and `New()` from `ulid-full.typedid` and define all three explicitly in the partial struct body of every type that uses the template — domain IDs and test helper structs alike. Any new `[StronglyTypedId("ulid-full")]` type must follow the same pattern.

## Consequences

- Stryker's Roslyn compilation resolves all three members from real source — the mutation run succeeds.
- The three members are no longer purely generated; they must be maintained alongside the template. If the template's private constructor signature ever changes, every hand-written partial struct must be updated.
- The PR checklist includes a check for this pattern so it is caught during review.
- No runtime or test behavior changes — the generated bodies were identical.
