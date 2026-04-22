# Tournaments API Requirements

This document describes what the API layer needs to expose so the website's `TournamentDataService` can replace its temporary JSON files with real data.

## Endpoints Required

### 2. `GET /seasons/{seasonId}/tournaments`

Returns all tournaments belonging to a given season.

**Path parameter** — `season`: the season label exactly as returned by the `/seasons` endpoint (e.g., `"2026"`, `"2020-21"`).

**Response** — array of `TournamentSummaryDto` objects (see schema below). All tournaments for the season in a single call; the website does its own filtering for Upcoming vs. Past.

---

## `TournamentSummaryDto` Schema

All fields that the website reads, mapped to `TournamentSummaryViewModel`:

| JSON field            | Type            | Nullable | Notes                                                                 |
|-----------------------|-----------------|----------|-----------------------------------------------------------------------|
| `id`                  | string          | No       | ULID string (26 chars). Used to build detail URLs: `/tournaments/{season}/{id}` |
| `name`                | string          | No       | Public tournament name                                                |
| `season`              | string          | No       | Season label; must match the value in the URL                         |
| `startDate`           | string (date)   | No       | ISO 8601 date-only: `"2026-05-03"`                                   |
| `endDate`             | string (date)   | No       | Same format; equals `startDate` for single-day events                 |
| `tournamentType`      | string (enum)   | No       | One of: `Singles`, `Doubles`, `Trios`, `Team`, `Senior`, `Women`, `SpecialEvent` |
| `eligibility`         | string (enum)   | No       | One of: `Open`, `Senior50Plus`, `Women`, `NonChampions`, `Champions` |
| `entryFee`            | decimal         | Yes      | Per-bowler entry fee in USD; null if not set                          |
| `registrationStatus`  | string (enum)   | Yes      | One of: `Open`, `ClosingSoon`, `Closed`, `Full`, `Completed`; null when registration hasn't opened |
| `registrationUrl`     | string (uri)    | Yes      | Full URL for the external registration form; null if not available    |
| `bowlingCenterName`   | string          | Yes      | Host center name; null until confirmed                                |
| `bowlingCenterCity`   | string          | Yes      | Host center city; null until confirmed                                |
| `sponsor`             | string          | Yes      | Primary sponsor name; null if unsponsored                             |
| `addedMoney`          | decimal         | Yes      | Sponsor-added prize money in USD; null if none                        |
| `entries`             | integer         | Yes      | Current entry count; null until tracking begins                       |
| `maxEntries`          | integer         | Yes      | Entry cap; null if uncapped                                           |
| `patternName`         | string          | Yes      | Oil pattern name (public portion only); null until set                |
| `patternLength`       | integer         | Yes      | Pattern length in feet; null until set                                |
| `patternLengthCategory` | string        | Yes      | Pattern difficulty/length bucket label used when exact pattern details are not published |
| `tournamentLogoUrl`   | string (uri)    | Yes      | URL to the tournament logo image; null when unavailable               |
| `winners`             | string[]        | No       | Names of the winning bowler(s); empty array for pending/upcoming events. Single-winner events have one element; doubles/trios/team events have multiple. |

**Enum serialization**: all enum fields are serialized as their string name (e.g., `"Singles"`, not `0`). The website uses `JsonStringEnumConverter`.

**Date serialization**: `startDate` / `endDate` must be `DateOnly`-compatible ISO 8601 strings (`yyyy-MM-dd`). Do NOT include time or timezone.

---

## Derived/Computed Fields (website-side only)

These are **not** returned by the API — the website computes them from the fields above:

| ViewModel property    | Computed from                                                         |
|-----------------------|-----------------------------------------------------------------------|
| `IsMultiDay`          | `EndDate > StartDate`                                                 |
| `HasAddedMoney`       | `AddedMoney > 0`                                                      |
| `HasCapacityData`     | `Entries != null && MaxEntries != null`                               |
| `HasHost`             | `BowlingCenterName != null`                                           |
| `HasSponsor`          | `Sponsor != null`                                                     |
| `CanRegister`         | `RegistrationUrl != null`                                             |
| `IsPast`              | `EndDate < today` (computed at render time)                           |
| `IsMergedSeason`      | `Season == "2020-21"`                                                 |
| `DaysUntilStart`      | `max(0, StartDate - today)`                                           |
| `IsUrgent`            | `0 ≤ DaysUntilStart ≤ 21`                                            |
| `DisplayLocation`     | `"{BowlingCenterName} · {BowlingCenterCity}"` or just the name       |
| `DisplayPriceLabel`   | `"Added money"` if `HasAddedMoney`, else `"Entry fee"`                |
| `DisplayPrice`        | `AddedMoney` if set, else `EntryFee`                                  |
| `PatternDisplay`      | `"{PatternName} · {PatternLength} ft"` when both exist, else `PatternLengthCategory` |
| `HasWinners`          | `Winners.Count > 0`                                                   |

---

## Domain Context — What Drives These Fields

The API layer maps from the `Tournament` aggregate. Notable mappings:

- **`id`** → `Tournament.Id` (ULID, serialized as string)
- **`season`** → Derived from the tournament's season relationship or a `Season` value object
- **`tournamentType`** / **`eligibility`** → Value objects or enums on the `Tournament` aggregate
- **`registrationStatus`** → Computed from registration state (open date, close date, entry count vs. cap)
- **`winners`** → From a `TournamentResult` or equivalent child entity; populated after the event concludes. Empty array until results are recorded.
- **`tournamentLogoUrl`** → Stored on the `Tournament` aggregate; the URL points to an externally hosted or CDN-served image.
- **`entries`** / **`maxEntries`** → From the `TournamentSponsors` table (see migration `20260421202302_TournamentSponsors_Init`): `maxEntries` maps to the cap on entries; `entries` is a live count from the entries table

---

## Query Handler Notes

- Both endpoints are **read-only queries** — no commands needed for the tournament list page.
- `/seasons` can be aggressively cached (seasons list changes rarely — a new season is added at most once a year).
- `/seasons/{season}` can be cached per season label; past seasons are immutable once complete.
- The website never requests partial data — it always fetches all tournaments for a season and filters client-side.

---

## Wiring Up in the Website

When the API is ready, replace `TournamentDataService` (currently reads from `wwwroot/data/tournaments/*.json`) with an `HttpClient`-backed implementation of `ITournamentDataService`:

```csharp
using Neba.Website.Server.Tournaments.Schedule;

internal interface ITournamentDataService
{
    Task<List<TournamentSummaryViewModel>> GetTournamentsForSeasonAsync(string season, CancellationToken ct = default);
    Task<List<string>> GetAvailableSeasonsAsync(CancellationToken ct = default);
}
```

The `Singleton` registration in `Program.cs` remains; the JSON reader just swaps for an HTTP reader. `TournamentSummaryViewModel` now lives under `Neba.Website.Server.Tournaments.Schedule`, so the service keeps the interface in `Neba.Website.Server.Tournaments` and imports the schedule namespace for model mapping.
