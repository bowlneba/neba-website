---
name: ubiquitous-language-review
description: Cross-check all domain types (aggregates, entities, value objects, SmartEnums, strongly-typed IDs) against docs/ubiquitous-language.md in both directions — flag missing UL entries, stale UL references, and XML comments that contradict or omit the UL definition. Prioritizes branch changes but reviews the full UL. Usage: /ubiquitous-language-review
---

Audit the alignment between `docs/ubiquitous-language.md` and the XML documentation on domain types. Work in both directions: code → UL and UL → code. Produce a structured list of proposed edits. Do not apply any changes automatically.

## Steps

### 1. Establish scope

```
git diff main...HEAD --name-only
```

Collect every `src/Neba.Domain/**/*.cs` file that appears in the diff — these are **priority files** reviewed first. After reviewing priority files, extend the scan to all domain types.

### 2. Load the Ubiquitous Language

Read `docs/ubiquitous-language.md` in full. For each term, extract:

- **Term name** (the `###` heading)
- **Definition** (the `**Definition**:` line and its paragraph)
- **In Code** reference (namespace, type name, property name if present)

Build a lookup: `TypeName → { definition, properties[] }`.

### 3. Scan domain types

Glob `src/Neba.Domain/**/*.cs`. For each file, identify:

- **Type declarations**: `class`, `record`, `record struct`, `struct`, `enum` — capture name, kind (aggregate root / entity / value object / SmartEnum / strongly-typed ID), and any existing `<summary>` comment
- **Property declarations**: on each type, capture name and existing `<summary>` comment
- **Public method signatures**: on each type, capture parameter names and types; note any `<param>` tags present

**Type classification rules**:
- Inherits `AggregateRoot` → aggregate root
- Inherits `SmartEnum` or `SmartFlagEnum` → SmartEnum
- Has `[StronglyTypedId]` attribute → strongly-typed ID
- `sealed record` or `readonly record struct` that is not an aggregate → value object
- All others in `Neba.Domain` → entity or domain concept

### 4. Cross-reference — Code → UL

For each domain type found in step 3, check `docs/ubiquitous-language.md`:

**A. Missing UL entry**
The type has no matching entry in the UL at all.

Flag as: `[MISSING UL] TypeName — no entry in ubiquitous-language.md`

Exception: if the type is clearly an internal infrastructure concern (e.g., EF configuration class, shadow ID holder, partial stub) and has no business meaning on its own, note it as `[EXEMPT — infrastructure]` and skip.

**B. Missing XML `<summary>` on the type**
The UL has an entry for this type but the C# declaration has no `<summary>` comment.

Flag as: `[MISSING XML] TypeName — UL entry exists but no <summary> on the type declaration`

**C. XML `<summary>` contradicts or omits the UL definition**
The summary exists but says something meaningfully different from — or leaves out the core purpose of — the UL definition. Not a word-for-word match requirement: the test is whether an engineer reading only the XML comment would understand the same concept as an engineer reading the UL.

Flag as: `[MISALIGNED XML] TypeName — XML says "…", UL says "…"`

**D. Missing property-level XML**
The UL entry describes named properties (via a properties table or explicit field descriptions) but the corresponding C# property has no `<summary>`.

Flag as: `[MISSING PROP XML] TypeName.PropertyName — UL describes this property but no <summary> on the property`

**E. Property XML contradicts UL property description**
Same test as C but at the property level.

Flag as: `[MISALIGNED PROP XML] TypeName.PropertyName — XML says "…", UL says "…"`

### 5. Cross-reference — UL → Code

For each **In Code** type reference in the UL:

**F. Referenced type not found in codebase**
The UL entry says `Type: Foo` or `Type: Foo (aggregate root)` but no such type exists under the stated namespace.

Flag as: `[STALE UL] UL entry "TermName" references TypeName but no such type found in Neba.Domain`

**G. Referenced namespace mismatch**
The type exists but lives in a different namespace than the UL states.

Flag as: `[STALE UL NAMESPACE] UL entry "TermName" says Neba.Domain.X but TypeName is in Neba.Domain.Y`

### 6. Method parameter notes (lightweight)

For public methods on domain types where a parameter's type is itself a UL-defined domain concept, check whether a `<param>` tag is present. This is advisory only — do not flag an absence as a blocker. If a `<param>` tag is present but says something that conflicts with the UL definition of that type, flag it as `[MISALIGNED PARAM XML]`.

### 7. Present findings

Output findings grouped as follows. Within each group, sort priority files (from step 1 diff) first, then remaining files alphabetically.

---

## UL Review

### Summary
> N types reviewed across M bounded contexts. P issues found (Q in branch-changed files).

### 🚫 Missing UL Entries
Types in the codebase with no UL definition and no documented exemption.

| Type | Kind | File |
|---|---|---|
| … | … | … |

*Proposed action*: For each, either add a UL entry to `docs/ubiquitous-language.md` under the correct section, or — if the type has no business meaning warranting a UL entry — add a one-line XML `<summary>` stating what it is and why it isn't in the UL (e.g., `/// <summary>EF Core configuration type — no UL entry needed.</summary>`).

### 📝 Missing or Misaligned XML Comments
Types and properties where the XML diverges from — or is absent relative to — the UL.

For each item:
- **Type/property** (link to file)
- **Issue** (MISSING XML / MISALIGNED XML / MISSING PROP XML / MISALIGNED PROP XML)
- **UL says**: quote the relevant definition fragment
- **XML currently says**: quote current comment, or *(none)*
- **Proposed `<summary>`**: the exact comment text to use

### 🗄️ Stale UL References
UL entries whose "In Code" type or namespace no longer matches the codebase.

For each item:
- **UL term** (link to heading in `docs/ubiquitous-language.md`)
- **Issue** (type not found / namespace mismatch)
- **Proposed UL edit**: the corrected "In Code" block

### ✅ Clean
Brief note on bounded contexts that passed without issues.

---

After presenting findings, ask: **"Would you like me to apply any of these proposed changes, or handle them section by section?"**

Do not write any file edits until the user confirms.

## Calibration notes

- **Not every domain concept needs a property-level UL entry** — if the UL only has a type-level definition with no property table, skip property-level XML alignment checks for that type.
- **SmartEnum values**: the UL often describes each enum value by name. Check that the C# SmartEnum members match the names in the UL table (spelling, casing). Flag mismatches as `[MISALIGNED XML]`.
- **Strongly-typed IDs** (e.g., `TournamentId`, `BowlerId`): the UL typically documents them inline under the owning type's "Identity" section, not as a top-level term. Their `<summary>` just needs to state what aggregate they identify — no separate UL entry required, but they should have a `<summary>`.
- **Internal navigation properties**: `internal` EF navigation properties do not require `<summary>` comments and should not be flagged.
- **Exempt types**: EF `*Configuration` classes, migration files, partial stubs generated by source generators — skip entirely.
