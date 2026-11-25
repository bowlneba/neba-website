# Finding types

Read this in steps 4 to 6. Each type has a flag format; use it exactly so the report groups cleanly.

## Code → UL

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

## UL → Code

For each **In Code** type reference in the UL:

**F. Referenced type not found in codebase**
The UL entry says `Type: Foo` or `Type: Foo (aggregate root)` but no such type exists under the stated namespace.

Flag as: `[STALE UL] UL entry "TermName" references TypeName but no such type found in Neba.Domain`

**G. Referenced namespace mismatch**
The type exists but lives in a different namespace than the UL states.

Flag as: `[STALE UL NAMESPACE] UL entry "TermName" says Neba.Domain.X but TypeName is in Neba.Domain.Y`

## Method parameter notes (lightweight)

For public methods on domain types where a parameter's type is itself a UL-defined domain concept, check whether a `<param>` tag is present. This is advisory only — do not flag an absence as a blocker. If a `<param>` tag is present but says something that conflicts with the UL definition of that type, flag it as `[MISALIGNED PARAM XML]`.
