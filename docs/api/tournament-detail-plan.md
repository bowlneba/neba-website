# Tournament Detail Page — Implementation Plan

## Goal

Add a tournament detail page reachable from the tournament schedule list. The page serves two purposes depending on tournament state:

- **Past tournaments** — "See Results" link; page shows a full results breakdown (place, prize money, points per bowler, side cut).
- **Upcoming tournaments** — "View Details" or "Register" link; page shows expanded info (oil patterns, entry fee, registration, sponsors) that is too dense to fit on the schedule card.

A single route (`/tournaments/{id}`) and a single `TournamentDetailDto` handle both cases. The results collection is empty for upcoming or data-unavailable tournaments; the UI adapts based on a `HasResults` flag.

---

## Deferred

The following are explicitly out of scope for this phase:

- **Qualifying scores** — per-bowler game-by-game scores; team events add multi-bowler-per-entry complexity that needs its own design pass.
- **Match play records** — same team-event complexity; deferred to a follow-up.
- **2026+ results** — stat tables for post-2025 tournaments do not exist yet. The query returns `Results = []` for those tournaments; a second branch will be added when the stat tables land (mirrors the existing comment in `TournamentQueries.cs`).

---

## Data Sources

| Era | Results source | Entry count source | Champions source |
|-----|---------------|-------------------|-----------------|
| Pre-2026 (historical) | `historical.tournament_results` (`HistoricalTournamentResult`) | `historical.tournament_entries` (`HistoricalTournamentEntry`) | `historical.tournament_champions` (`HistoricalTournamentChampion`) |
| 2026+ | _(stat tables, not yet available)_ | _(stat tables)_ | _(stat tables)_ |

---

## Backend

### 1. `TournamentResultDto`

New DTO in `Neba.Application/Tournaments/GetTournamentDetail/`.

| Property | Type | Notes |
|----------|------|-------|
| `BowlerName` | `Name` | From `Bowler` navigation |
| `Place` | `int?` | Null when place was not recorded |
| `PrizeMoney` | `decimal` | Total payout for this result row |
| `Points` | `int` | Season points awarded |
| `SideCutName` | `string?` | Null when bowler competed in the main cut |
| `SideCutIndicator` | `Color?` | Display color for the side cut; null when main cut |

### 2. `TournamentDetailDto`

New DTO in `Neba.Application/Tournaments/GetTournamentDetail/`.

Contains all fields currently in `SeasonTournamentDto` plus:

| Property | Type | Notes |
|----------|------|-------|
| `Results` | `IReadOnlyCollection<TournamentResultDto>` | Ordered by Place (nulls last), then bowler last name |
| `EntryCount` | `int?` | Total entry count; null when unknown |

> **Note**: `SeasonTournamentDto` stays as-is for the list endpoint. `TournamentDetailDto` is a separate type — it carries more data and the list endpoint should stay lean.

### 3. `ITournamentQueries` — new method

```csharp
Task<TournamentDetailDto?> GetTournamentDetailAsync(TournamentId tournamentId, CancellationToken cancellationToken);
```

Returns `null` when the tournament is not found (handler converts to `Error.NotFound`).

### 4. `TournamentQueries` implementation

Query strategy for historical results:

```
tournaments
  LEFT JOIN historical.tournament_results ON db_id
    LEFT JOIN bowlers ON BowlerId
    LEFT JOIN side_cuts ON SideCutId (nullable)
  LEFT JOIN historical.tournament_entries ON db_id
  LEFT JOIN historical.tournament_champions ON db_id (already exists)
```

- Order results by `Place ASC NULLS LAST`, then bowler last name.
- For 2026+ tournaments: return an empty `Results` collection and `HasResults = false`.

### 5. Application layer

`GetTournamentDetailQuery` + `GetTournamentDetailQueryHandler` in `Neba.Application/Tournaments/GetTournamentDetail/`.

- Returns `ErrorOr<TournamentDetailDto>`.
- Handler calls `ITournamentQueries.GetTournamentDetailAsync`; maps `null` → `Error.NotFound`.
- Cache descriptor: cache key `tournament-detail:{tournamentId}`; TTL aligned with the existing tournament cache strategy.

### 6. API endpoint

`GET /tournaments/{tournamentId}`

- FastEndpoints endpoint in `Neba.Api/Tournaments/GetTournamentDetail/`.
- Path parameter: `tournamentId` (ULID string, 26 chars).
- Authorization: `AllowAnonymous()`.
- Response: `TournamentDetailResponse` (contracts type mapping from `TournamentDetailDto`).
- Produces: `200 OK`, `404 Not Found`.
- Not nested under `/seasons/` — season context is not needed to fetch a single tournament.

### `TournamentDetailResponse` (Contracts)

Mirror of `TournamentDetailDto` for the public contract. Includes a `TournamentResultResponse` nested type.

---

## Frontend (Blazor)

### Route

`/tournaments/{Id}` — `TournamentDetail.razor`

### `TournamentDetailViewModel`

Wraps `TournamentDetailResponse` and computes display states:

| Property | Computed from |
|----------|---------------|
| `HasRegistrationUrl` | `RegistrationUrl != null` |
| `IsUpcoming` | `StartDate >= today` |
| `IsPast` | `EndDate < today` |
| `MainCutResults` | `Results.Where(r => r.SideCutName == null)` |
| `SideCutGroups` | `Results.GroupBy(r => r.SideCutName)` for side cut sections |

### Page layout (past tournament with results)

```
Tournament name + date + bowling center
Oil patterns / sponsors strip

[ Results ]
  Main Cut
    Place | Bowler | Prize Money | Points
    ...
  Side Cut: {Name} (color indicator)
    Place | Bowler | Prize Money | Points
    ...

[ No results yet ] — shown when HasResults = false
```

### Page layout (upcoming tournament)

```
Tournament name + date + bowling center
Entry fee + [ Register ] button (if RegistrationUrl present)
Oil patterns
Sponsors
```

### Schedule card updates

- `TournamentPastCard` — add "See Results" link → `/tournaments/{Id}`.
- `TournamentUpcomingCard` — add "View Details" link (and "Register" if `RegistrationUrl` is set) → `/tournaments/{Id}`.

### `ITournamentDataService` — new method

```csharp
Task<TournamentDetailViewModel?> GetTournamentDetailAsync(string tournamentId, CancellationToken cancellationToken);
```

---

## Implementation Order

1. `TournamentResultDto` + `TournamentDetailDto`
2. `GetTournamentDetailAsync` on `ITournamentQueries` + `TournamentQueries` implementation
3. `GetTournamentDetailQuery` + handler + cache descriptor
4. `TournamentDetailResponse` + nested result type in Contracts
5. FastEndpoints endpoint + summary + validator
6. `TournamentDetailViewModel` + `ITournamentDataService.GetTournamentDetailAsync`
7. `TournamentDetail.razor` page (past and upcoming layouts)
8. Update `TournamentPastCard` and `TournamentUpcomingCard` with links

---

## Future Phases

- **Qualifying scores** — per-bowler game scores per squad; team events handled after schema is designed.
- **Match play records** — bracket results, wins/losses; team events handled in tandem with qualifying scores.
- **2026+ results** — wire in the stat-table branch in `TournamentQueries` once those tables exist.
- **Entry count view** — PostgreSQL view unifying `historical.tournament_entries` and derived 2026+ counts (described in `reference/tournaments/tournament_history.md`).
