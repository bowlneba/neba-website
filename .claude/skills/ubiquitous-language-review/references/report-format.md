# Report format

Read this in step 7. Sort branch-changed files first, then the rest alphabetically.

## Report template

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
