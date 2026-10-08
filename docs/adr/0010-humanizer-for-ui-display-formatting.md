# ADR-0010: Humanizer for UI Display Formatting

## Status

Accepted

## Context

The website showed raw numbers where people read words: tournament places as "1", "2", "3", season ranks as bare digits or "#3", and a countdown tag that read "In 0 days" and "In 1 days". Counts were pluralized by hand (`group.Count == 1 ? "title" : "titles"`), and some labels never pluralized at all ("1 events"). Each fix was a one-off ternary, so the rules lived in many places and drifted.

[Humanizer](https://github.com/humanizr/humanizer) gives tested answers to these cases, including the English irregulars (11th, 12th, 13th; "entry" to "entries").

The API returns data, not display text. Ordinals and plurals are presentation.

## Decision

Use **`Humanizer.Core`** in `Neba.Website.Server` only.

- The package is `Humanizer.Core`, not `Humanizer`. The meta-package pulls in every locale. The site is English-only, so we take no locale assets.
- `Neba.Api`, `Neba.Api.Contracts`, and `Neba.Website.Client` do not reference it. The API keeps returning raw numbers and counts.
- Use it for **ordinals** (`Ordinalize`) and **count-aware plurals** (`ToQuantity`).
- Pass `DisplayCulture.English` where an overload takes a culture. Analyzer CA1304 requires it, and it keeps output independent of server locale. The `ShowQuantityAs` overload of `ToQuantity` takes no culture and uses the current UI culture.
- Keep formatting in view models when a value is reused (`FormattedPlace`, `UrgencyLabel`). Inline the call in a `.razor` file for one-off labels.

### Where we do not use it

- **Dates and times.** Our formats ("MMM d, yyyy") are fixed on purpose, and relative text goes stale on cached pages.
- **Currency and number formats.** `C0`, `N0`, and `F2` already do the job.
- **Anything an e2e or help doc quotes verbatim**, unless the text is meant to change.
- **Localization.** If the site gains a second language, revisit this ADR.

## Consequences

### Positive

- One tested source for ordinal and plural rules. No more hand-written ternaries or "1 events".
- Fixed a visible bug: the countdown now says "Today", "Tomorrow", "In 5 days".
- The API contract stays free of display concerns.

### Negative

- One more dependency to patch. It is small and has no transitive packages.
- Plural output follows the current UI culture for the `ShowQuantityAs` overloads. Fine for an English-only site; a risk if a non-English culture is ever set on a request.
- Tests that match display text must use the new wording ("1st", not "1").

### Guidance for new UI

Before writing a `== 1 ? "x" : "xs"` ternary or appending "th", use `ToQuantity` or `Ordinalize`.
