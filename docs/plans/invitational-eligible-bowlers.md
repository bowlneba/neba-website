# Invitational Eligible Bowlers

The Invitational tournament detail page shows a public list of bowlers who qualify to bowl it: those with a result record in 3 or more tournaments of the Invitational's season. The list shows only while the Invitational is `Scheduled`.

## Decisions locked in during scoping

Dated 2026-11-25.

- **Rule**: a bowler is eligible when they have a `TournamentResult` in 3 or more distinct tournaments of the Invitational's own season. Stats eligible or title eligible does not matter; a result record counts as one tournament bowled. The list grows through the season (a visitor in July sees who qualified so far).
- **Threshold**: a constant of 3 for now. Past rules used 5. It should become a per-tournament setting when the WinForms app moves to the website and the create-tournament flow gains more fields. Not built here.
- **Data source**: `Tournament.Results` only. The `HistoricalTournamentResults` table is not read, because the feature looks forward only. This supersedes any plan to merge historical and recorded results as `GetTournament` does for its results table.
- **Exposure**: the endpoint returns only the eligible list (display name and USBC ID). It never returns counts, ineligible bowlers, dates of birth or internal IDs beyond what the list needs. The public sees nothing that a bowler outside the list could use to learn another bowler's count.
- **Search**: the page filters the list in the browser by name or USBC ID. A per-bowler "how many tournaments have I bowled" lookup is deferred: it would reveal counts for any bowler, and matters more at a threshold of 5 than 3. Revisit when the threshold becomes configurable.
- **USBC ID**: shown in full. Bowlers use it to find themselves and to tell same-name bowlers apart. A bowler with no USBC ID shows without one.
- **Visibility**: the section shows only for `TournamentType.Invitational` while the tournament status is `Scheduled`. For `Completed`, `Truncated` and `Cancelled` tournaments the endpoint returns no list and the page hides the section.
- **Freshness**: the list is cached and clears when the cache clears after a tournament records results.
- **Response shape** (2026-11-25, supersedes the bare `CollectionResponse<EligibleBowlerSummaryResponse>` first drafted): the endpoint returns `EligibleBowlersResponse` with `Requirements` and `Bowlers`. `Requirements` holds `MinTournamentsCount` (nullable, 3 for the Invitational) and is `null` when the tournament has no eligibility list. `Bowlers` is never null. So `null` requirements means "does not apply", and requirements with no bowlers means "nobody qualifies yet". Reason: the rule and the list come from one place, and a later rule (for example a champion requirement for the Tournament of Champions) adds one nullable property. Do not add any other requirement now. The shape describes rules and does not evaluate them. `MinTournamentsCount` is the rule, not a bowler's count, so it leaks nothing.
- **Phase 2 choices** (2026-11-25): the list sits in a fixed-height scroll box; the filter runs in C# as the visitor types; the component loads through `TournamentApiService`; the page text takes the number from `Requirements.MinTournamentsCount`, never a hard-coded 3.
- **Wrong type or status** (2026-11-25): an unknown tournament ID returns 404. A tournament that is not a `Scheduled` Invitational returns 200 with `Requirements` null and an empty `Bowlers` list. The page already loads the tournament, so it hides the section from `TournamentType` and `Status` and shows the "none yet" text only for a `Scheduled` Invitational. Rejected: 422 for a non-Invitational and 409 for a non-Scheduled one, because a normal state would log a 4xx on most detail pages.
- **Cache clearing** (2026-11-25): the list carries a new tag `neba:tournaments:eligible-bowlers` plus the tournament's own tag, and expires after 1 day as a backstop. The results sync job, delete tournament and edit tournament (on season change) clear the new tag. Rejected: clearing the global `neba:tournaments` tag (wipes too much) and resolving the season before the cache lookup (extra database read on every request). Reason: the list depends on other tournaments' results, and today the results sync job clears only the tournament's own tag and the champions tag.
- **Defaults taken** (correct me if wrong): the list sorts by legal last name, then legal first name, then bowler ID as a tie-break. The API sorts after it reads the rows, with an ordinal ignore-case comparer, so the order does not depend on the database collation. The displayed name uses the nickname when a bowler has one, so the list can read slightly out of order for nicknamed bowlers. The response carries no bowler ID. The list key on the page is its index.
- **Search apostrophes** (2026-11-25, default taken): the filter treats a curly apostrophe as a straight one, so a phone's `O’Brien` finds `O'Brien`.
- **Permissions**: no new policy, role or permission. The endpoint is `AllowAnonymous()`, like `CheckTeamEligibility`. Webmaster and Admin need no change.
- **Criticality**: not critical. The NFR checkpoint is skipped.
- **Flow** (confirmed): visitor opens an Invitational detail page; if `Scheduled`, an "Eligible bowlers" section loads from a separate call, sorted last name then first name, each row with display name and USBC ID; a search box filters in the browser; an empty list shows "No bowlers have reached {n} tournaments yet this season.", with {n} from the response
- **UI inspiration**: the `bowlingmegabucks/tournament-director` repo (React/Node, on the user's disk) has a registration list with search by name or USBC ID. Use it for look and feel, not as code.

## Implementing this plan

Work on branch `invitationalEligibility`, in one session, with **no subagents**. Agents drafted this plan; they do not build it.

### Read first

This file is about 2,800 lines. Do not read it all. Read, in order:

1. This section.
2. "Decisions locked in during scoping".
3. The "Notes and not checked" subsection at the end of Phase 1 and of Phase 2.
4. "Commit plan".
5. Only the sections the current commit needs (see the map below). Find them by heading with `grep -n '^##' docs/plans/invitational-eligible-bowlers.md`.

### When parts disagree

Decisions win over notes, notes over code drafts, code drafts over `docs/ubiquitous-language.md` and older plans. The repo wins over the plan on a name, signature or path that does not match; fix the plan when you find one.

### State of the drafts

- **Nothing here has been compiled or run.** Treat every code block as a close draft.
- Phase 2 cannot build until commit 6 adds the contracts and the Refit method.
- Checked against the repo: the test files and factories the plan names exist, as do `TournamentType.FromName`, the unique (tournament, bowler) key on results and the mock server's Invitational IDs.
- Not checked: the SQL EF writes for the eligibility query (the handler integration tests prove it works); analyzer warnings, above all the `field` accessors and `SuppressMessage` in `EligibleBowlerViewModel`; whether `.tournament-detail__section:empty` matches Blazor's markup; phone layout and typing lag.
- Factories: `CLAUDE.md` wants `Create()` and `Bogus(...)` on each. The drafts give the four wrapper factories (`EligibleBowlersDto`, `EligibilityRequirementsDto` and their responses) only `Create`; add `Bogus` in the commit that creates each. Write the response wrappers from the DTO ones, with public members.
- Tests drafted as signatures (validator, pipeline, query, summary, eviction) need bodies written from their one-line assertions and the test class each one names.
- The endpoint Verify snapshot does not exist yet. Show the `.received.txt` content to the user before accepting it.

### Each commit

1. Read the commit entry and its sections.
2. Make the changes yourself, code and tests together.
3. Build, then run only the matching tests, and show failures rather than whole logs:
   - API: `dotnet test --project tests/Neba.Api.Tests --filter-trait "Component=Tournaments"` (integration tests need Docker)
   - Website: `dotnet test --project tests/Neba.Website.Tests --filter-class "*EligibleBowler*" "*TournamentDetail*" "*TournamentApiService*" "*ChampionsPage*" "*TournamentsPage*"`
   - Browser: `npm run test:e2e`
4. Fix failures. If the code drifts from the plan, update the plan.
5. Stage that commit's files by name (never `git add -A`), commit with the subject and the attribution lines the session gives, and tick the box under "Progress". Include the ticked plan file in the same commit.

Commits 1 and 2 are already done. Start at commit 3.

### Do not

- Spawn subagents.
- Read `HistoricalTournamentResults` for this feature.
- Return counts, ineligible bowlers, dates of birth or bowler IDs from the endpoint.
- Build a "how many have I bowled" lookup, a configurable threshold, or any requirement besides `MinTournamentsCount`.
- Add a permission, policy, migration or index.
- Hard-code 3 in the website. The page takes the number from the response.
- Clear the global `neba:tournaments` tag for this.
- Move the filter to JavaScript, unless the phone check shows lag and the user agrees.

### Before particular commits

- Commit 4: confirm `CancelTournamentCommandHandler` still clears `neba:tournaments:{id}`.
- Commit 9 or 10: check the section in a browser at phone width, including the scroll box, typing lag and focus after "Try again". Option 1 mockup is the reference.
- Commit 12: needs the app running for screenshots.
- After commit 12: run the full test suite, then ask the user to run `/pull-request-prep`, which includes the convention review. A PR to `main` must raise `<Version>` in `Directory.Build.props` above `main`'s; ask the user for the number.

### Commit to section map

| Commit | Sections |
|---|---|
| 1, 2 | Commit plan |
| 3 | Phase 1 "Domain" (glossary paragraph) |
| 4 | Phase 1 "Domain" (constant), "Cache"; "Tests": "Query and cache descriptor" (the `TournamentCacheTagTests` block) and "Eviction" |
| 5 | Phase 1 "Handlers" (query, three DTOs, handler), "Database"; "Tests": "Factories" (DTO factories), "Handler", "Query and cache descriptor" (query tests) |
| 6 | Phase 1 "Handlers" (request, validator, endpoint, summary), "Contracts"; "Tests": "Factories" (response factories), "Validator", "Endpoint", "Pipeline", "Summary" |
| 7 | Phase 2 "Playwright spec and mock" (part 1) |
| 8 | Phase 2 "Code": view models, mapping, both service files; "Tests": "Factories", "Mapping and service", "Required edits to the two fakes" |
| 9 | Phase 2 "Behavior", "Mockups", "Code": `EligibleBowlers.razor` and `.razor.css`; "Tests": `EligibleBowlersTests` and "Extra cases" |
| 10 | Phase 2 "Where it goes", "Code": the three `TournamentDetail*` blocks; "Tests": "View model test", "Page tests" |
| 11 | Phase 2 "Playwright spec and mock" (part 2) |
| 12 | Phase 2 "Docs" |

### Mockups

`docs/plans/mockups/invitational-eligible-bowlers/option-1.html` is chosen and guides commit 9. `option-2.html` was not picked.

### Progress

- [x] 1. Add invitational eligible bowlers plan (committed 2026-11-25 as `1391a3c6`, "Plan Invitational eligible-bowler list")
- [x] 2. Keep feature-plan agents to planning (committed 2026-11-25 as `31b8f50f`)
- [ ] 3. Define Invitational Eligibility
- [ ] 4. Add eligible bowlers cache tag and eviction
- [ ] 5. Add eligible bowlers query
- [ ] 6. Add eligible bowlers endpoint
- [ ] 7. Mock the eligible bowlers route
- [ ] 8. Load eligible bowlers in the tournament service
- [ ] 9. Add eligible bowlers component
- [ ] 10. Show eligible bowlers on Invitational details
- [ ] 11. Cover the eligible bowlers section
- [ ] 12. Document the eligible bowlers list

## Phase 1: API

Code draft. No schema change, no migration. Route: `GET /tournaments/{id}/eligible-bowlers`, anonymous. Paths are relative to the repo root. Nothing here has been compiled; build before relying on it.

### Domain

Edit `src/Neba.Api/Features/Tournaments/Domain/Tournament.cs`. Add inside the class, before `public required TournamentId Id` (the class holds no other `const` today):

```csharp
/// <summary>
/// The number of distinct tournaments in the Invitational's season a bowler must have a result in to be Invitational eligible.
/// </summary>
public const int InvitationalEligibilityThreshold = 3;
```

Glossary (own docs commit): add **Invitational Eligibility** to `docs/ubiquitous-language.md` under the tournaments section: a bowler with a result in 3 or more distinct tournaments of the Invitational's own season, shown only while the Invitational is Scheduled. Say it is separate from Title Eligible and Stats Eligible.

### Handlers

New folder `src/Neba.Api/Features/Tournaments/ListEligibleBowlers/`.

`ListEligibleBowlersRequest.cs`

```csharp
using FastEndpoints;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }
}
```

`ListEligibleBowlersRequestValidator.cs`

```csharp
using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersRequestValidator : Validator<ListEligibleBowlersRequest>
{
    public ListEligibleBowlersRequestValidator()
    {
        RuleFor(r => r.TournamentId)
            .NotEmpty()
            .WithErrorCode("ListEligibleBowlersRequest.TournamentIdRequired")
            .WithMessage("Tournament ID is required.")
            .Length(26)
            .WithErrorCode("ListEligibleBowlersRequest.TournamentIdInvalidLength")
            .WithMessage("Tournament ID must be a 26-character ULID.");
    }
}
```

`ListEligibleBowlersQuery.cs`

```csharp
using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed record ListEligibleBowlersQuery
    : ICachedQuery<ErrorOr<EligibleBowlersDto>>
{
    public required TournamentId Id { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.EligibleBowlers(Id);

    // A backstop: the results sync, delete and edit handlers clear the list by tag.
    public TimeSpan Expiry
        => TimeSpan.FromDays(1);
}
```

`EligibleBowlersDto.cs`

```csharp
namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>An eligibility list and the requirements it was built from.</summary>
internal sealed record EligibleBowlersDto
{
    // Null when the tournament has no eligibility list.
    public EligibilityRequirementsDto? Requirements { get; init; }

    public required IReadOnlyCollection<EligibleBowlerSummaryDto> Bowlers { get; init; }
}
```

`EligibilityRequirementsDto.cs`

```csharp
namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>What a bowler must meet to be eligible. A null property means that rule does not apply.</summary>
internal sealed record EligibilityRequirementsDto
{
    public int? MinTournamentsCount { get; init; }
}
```

`EligibleBowlerSummaryDto.cs`

```csharp
namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

/// <summary>A bowler who qualifies for an Invitational: display name and USBC ID only.</summary>
internal sealed record EligibleBowlerSummaryDto
{
    public required string Name { get; init; }

    public string? UsbcId { get; init; }
}
```

`ListEligibleBowlersQueryHandler.cs`

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>
{
    public async Task<ErrorOr<EligibleBowlersDto>> HandleAsync(
        ListEligibleBowlersQuery query, CancellationToken cancellationToken)
    {
        var invitational = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == query.Id)
            .Select(tournament => new { tournament.SeasonId, tournament.TournamentType, tournament.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (invitational is null)
        {
            return TournamentErrors.TournamentNotFound(query.Id);
        }

        // Compared in memory, after the SmartEnums load; not a SQL predicate.
        if (invitational.TournamentType != TournamentType.Invitational
            || invitational.Status != TournamentStatus.Scheduled)
        {
            return new EligibleBowlersDto { Requirements = null, Bowlers = [] };
        }

        var seasonId = invitational.SeasonId;
        var threshold = Tournament.InvitationalEligibilityThreshold;

        // Every result counts: results exist only on Completed or Truncated tournaments, and
        // (tournament, bowler) is unique, so Count() is the number of distinct tournaments.
        var eligibleBowlerIds = appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId)
            .SelectMany(tournament => tournament.Results)
            .GroupBy(result => result.BowlerId)
            .Where(group => group.Count() >= threshold)
            .Select(group => group.Key);

        var rows = await appDbContext.Bowlers
            .AsNoTracking()
            .Where(bowler => eligibleBowlerIds.Contains(bowler.Id))
            .Select(bowler => new { bowler.Id, bowler.Name, bowler.UsbcId })
            .ToListAsync(cancellationToken);

        // Sorted here, not in SQL, so the order does not depend on the database collation.
        var bowlers = rows
            .OrderBy(row => row.Name.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Name.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Id.Value)
            .Select(row => new EligibleBowlerSummaryDto
            {
                Name = row.Name.ToDisplayName(),
                UsbcId = row.UsbcId
            })
            .ToList();

        return new EligibleBowlersDto
        {
            Requirements = new EligibilityRequirementsDto { MinTournamentsCount = threshold },
            Bowlers = bowlers
        };
    }
}
```

Compile risk to check: `ThenBy(row => row.Id.Value)` has precedent in `GetSeriesAdvancementQueryHandler`. An integration test must prove the group-by subquery runs as one statement.

`ListEligibleBowlersEndpoint.cs`

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersEndpoint(
    IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>> queryHandler)
    : Endpoint<ListEligibleBowlersRequest, EligibleBowlersResponse>
{
    private readonly IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/eligible-bowlers");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListEligibleBowlers")
            .WithTags("Public")
            .Produces<EligibleBowlersResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
    }

    public override async Task HandleAsync(ListEligibleBowlersRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(
            new ListEligibleBowlersQuery { Id = new TournamentId(req.TournamentId) },
            ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description, error.Code);
            }

            await Send.ErrorsAsync(
                result.FirstError.Type == ErrorType.NotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status500InternalServerError,
                ct);

            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.OkAsync(
            new EligibleBowlersResponse
            {
                Requirements = result.Value.Requirements is null
                    ? null
                    : new EligibilityRequirementsResponse
                    {
                        MinTournamentsCount = result.Value.Requirements.MinTournamentsCount
                    },
                Bowlers = [.. result.Value.Bowlers.Select(bowler => new EligibleBowlerSummaryResponse
                {
                    Name = bowler.Name,
                    UsbcId = bowler.UsbcId
                })]
            },
            ct);
    }
}
```

`ListEligibleBowlersSummary.cs`

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersSummary
    : Summary<ListEligibleBowlersEndpoint>
{
    public ListEligibleBowlersSummary()
    {
        Summary = "Lists the bowlers eligible for an Invitational.";
        Description = "Returns the bowlers with a result in 3 or more tournaments of the Invitational's season, sorted by last name then first name. "
            + "Only a scheduled Invitational has a list; any other tournament returns null requirements and no bowlers. "
            + "Only the display name and USBC ID return. Tournament counts, ineligible bowlers and dates of birth never do.";

        Response(200, "The requirements and eligible bowlers. Requirements are null when the tournament has no list.",
            contentType: MediaTypeNames.Application.Json,
            example: new EligibleBowlersResponse
            {
                Requirements = new EligibilityRequirementsResponse { MinTournamentsCount = 3 },
                Bowlers =
                [
                    new EligibleBowlerSummaryResponse { Name = "Jane Smith", UsbcId = "1234-567890" },
                    new EligibleBowlerSummaryResponse { Name = "Sam Jones", UsbcId = null }
                ]
            });

        Response(400, "The tournament ID is not a 26-character ULID.");
        Response(404, "The tournament was not found.");
    }
}
```

### Database

Reads only. No configuration, migration or index change. The handler relies on the unique key on (tournament, bowler) in `TournamentResultConfiguration`, which exists. `Name.ToDisplayName()` runs after the rows load, and the sort runs in memory.

### Contracts

New `src/Neba.Api.Contracts/Tournaments/ListEligibleBowlers/EligibleBowlersResponse.cs`:

```csharp
namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// An eligibility list and the requirements it was built from.
/// </summary>
public sealed record EligibleBowlersResponse
{
    /// <summary>
    /// The eligibility requirements, or <see langword="null"/> when the tournament has no eligibility list.
    /// </summary>
    public EligibilityRequirementsResponse? Requirements { get; init; }

    /// <summary>
    /// The eligible bowlers, sorted by last then first name. Never null.
    /// </summary>
    public required IReadOnlyCollection<EligibleBowlerSummaryResponse> Bowlers { get; init; }
}
```

New `src/Neba.Api.Contracts/Tournaments/ListEligibleBowlers/EligibilityRequirementsResponse.cs`:

```csharp
namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// What a bowler must meet to be eligible. A null property means that rule does not apply.
/// </summary>
public sealed record EligibilityRequirementsResponse
{
    /// <summary>
    /// The number of tournaments in the season a bowler must have a result in.
    /// </summary>
    /// <example>3</example>
    public int? MinTournamentsCount { get; init; }
}
```

New `src/Neba.Api.Contracts/Tournaments/ListEligibleBowlers/EligibleBowlerSummaryResponse.cs`:

```csharp
namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// A bowler who qualifies for an Invitational.
/// </summary>
public sealed record EligibleBowlerSummaryResponse
{
    /// <summary>
    /// The bowler's display name (nickname when set).
    /// </summary>
    /// <example>Jane Smith</example>
    public required string Name { get; init; }

    /// <summary>
    /// The bowler's USBC ID, or <see langword="null"/> when none is on file.
    /// </summary>
    /// <example>1234-567890</example>
    public string? UsbcId { get; init; }
}
```

Edit `src/Neba.Api.Contracts/Tournaments/ITournamentsApi.cs`. Add the using after `ListChampions`:

```csharp
using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;
```

Add the method after `ListTournamentChampionsAsync`:

```csharp
/// <summary>
/// Gets the bowlers eligible for an Invitational: a result in 3 or more tournaments of its season.
/// Returns null requirements and no bowlers for a tournament that is not a scheduled Invitational.
/// </summary>
/// <param name="tournamentId">The Invitational's ID.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The requirements and the eligible bowlers, sorted by last then first name.</returns>
[Get("/tournaments/{tournamentId}/eligible-bowlers")]
Task<IApiResponse<EligibleBowlersResponse>> ListEligibleBowlersAsync(
    string tournamentId,
    CancellationToken cancellationToken = default);
```

### Cache

Edit `src/Neba.Api/Caching/CacheDescriptors.cs`, class `Tournaments`. Add after `Tag`:

```csharp
/// <summary>
/// The tag every cached eligible-bowlers list carries. Each list depends on other tournaments' results,
/// so the results sync, delete tournament and edit tournament (season change) clear it.
/// </summary>
public const string EligibleBowlersTag = "neba:tournaments:eligible-bowlers";
```

Add after `Types`:

```csharp
/// <summary>
/// Returns a cache descriptor for the bowlers eligible for an Invitational. Every caller sees the same
/// data, so the key has no caller scope.
/// </summary>
/// <param name="id">The Invitational's tournament identifier.</param>
public static CacheDescriptor EligibleBowlers(TournamentId id)
    => new()
    {
        Key = $"{Tag}:{id}:eligible-bowlers",
        Tags = ["neba", Tag, $"{Tag}:{id}", EligibleBowlersTag]
    };
```

Eviction edits. Each clears `EligibleBowlersTag`.

`src/Neba.Api/Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs` (`CacheDescriptors` is already imported). Add after the champions and Hall of Fame evictions, before the winner-titles loop:

```csharp
await cache.RemoveByTagAsync("neba:tournaments:champions", token: ct);
await cache.RemoveByTagAsync(CacheDescriptors.HallOfFame.PointsTag, token: ct);

// An Invitational's eligible list counts other tournaments' results, so recorded results can change it.
await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.EligibleBowlersTag, token: ct);
```

`src/Neba.Api/Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandler.cs`. Add `using Neba.Api.Caching;` (between `Neba.Api.BackgroundJobs` and `Neba.Api.Database`). After the season tag line:

```csharp
await cache.RemoveByTagAsync($"neba:tournaments:{seasonId}", token: cancellationToken);

// Deleting a finalized tournament removes its results, which other Invitationals' eligible lists counted.
await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.EligibleBowlersTag, token: cancellationToken);
await TournamentSuccessorCacheEviction.EvictAsync(cache, successors, cancellationToken);
```

`src/Neba.Api/Features/Tournaments/EditTournament/EditTournamentCommandHandler.cs` (`Neba.Api.Caching` is imported). In `EvictCachesAsync`, replace the season-change block:

```csharp
if (currentSeasonId != previousSeasonId)
{
    await cache.RemoveByTagAsync($"neba:tournaments:{currentSeasonId}", token: cancellationToken);

    // The tournament's results count in a different season now, so every eligible list can change.
    await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.EligibleBowlersTag, token: cancellationToken);
}
```

`CancelTournamentCommandHandler` already clears `neba:tournaments:{id}`, which the list carries, so it needs no edit.

### Tests

Folder `tests/Neba.Api.Tests/Features/Tournaments/ListEligibleBowlers/`. Seeds 2201 and 2202 were unused when drafted. Tournaments that carry results are always Completed or Truncated: `TournamentFactory.Create(results: ...)` throws for Scheduled or Cancelled. The Scheduled Invitational under test never carries results; qualifiers come from other tournaments in the season.

#### Factories

`tests/Neba.TestFactory/Tournaments/EligibleBowlerSummaryDtoFactory.cs`. `usbcId` defaults to a valid value (the `TeamEligibilityMemberResponseFactory` pattern) so a test can pass `null`:

```csharp
using Bogus;

using Neba.Api.Features.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlerSummaryDtoFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    internal static EligibleBowlerSummaryDto Create(
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<EligibleBowlerSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        // Every third row has no USBC ID, so any collection of 3 or more holds both shapes.
        return [.. Enumerable.Range(0, count).Select(index => Create(
            name: faker.Name.FullName(),
            usbcId: index % 3 == 2 ? null : faker.Random.Replace("####-#####")))];
    }

    internal static IReadOnlyCollection<EligibleBowlerSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
```

`tests/Neba.TestFactory/Tournaments/EligibleBowlerSummaryResponseFactory.cs`:

```csharp
using Bogus;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlerSummaryResponseFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    public static EligibleBowlerSummaryResponse Create(
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<EligibleBowlerSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(index => Create(
            name: faker.Name.FullName(),
            usbcId: index % 3 == 2 ? null : faker.Random.Replace("####-#####")))];
    }

    public static IReadOnlyCollection<EligibleBowlerSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
```

Also add factories for the two wrapper types, in the same folder:

```csharp
// EligibilityRequirementsDtoFactory.cs (public static class, internal members)
public const int ValidMinTournamentsCount = 3;
internal static EligibilityRequirementsDto Create(int? minTournamentsCount = ValidMinTournamentsCount)
    => new() { MinTournamentsCount = minTournamentsCount };

// EligibleBowlersDtoFactory.cs
internal static EligibleBowlersDto Create(
    EligibilityRequirementsDto? requirements = null,
    IReadOnlyCollection<EligibleBowlerSummaryDto>? bowlers = null)
    => new()
    {
        Requirements = requirements ?? EligibilityRequirementsDtoFactory.Create(),
        Bowlers = bowlers ?? [EligibleBowlerSummaryDtoFactory.Create()]
    };

internal static EligibleBowlersDto CreateNotApplicable()
    => new() { Requirements = null, Bowlers = [] };

// EligibilityRequirementsResponseFactory.cs and EligibleBowlersResponseFactory.cs mirror the two above
// with the Contracts types and public members.
```

#### Handler (integration)

`ListEligibleBowlersQueryHandlerTests.cs`. Setup is copied from `CheckTeamEligibilityQueryHandlerTests`. The season runs 2010 to 2030. `SeedParticipationAsync` gives each bowler a count of results: it creates as many tournaments as the largest count, and tournament `i` holds a result for every bowler whose count exceeds `i`.

```csharp
using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListEligibleBowlersQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string LowerBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";
    private const string HigherBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2F";

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2010, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private ListEligibleBowlersQueryHandler CreateHandler() => new(_dbContext);

    private static ListEligibleBowlersQuery Query(Tournament tournament) => new() { Id = tournament.Id };

    private async Task<Season> SeedSeasonAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: start, endDate: end);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    private async Task<Bowler> SeedBowlerAsync(
        CancellationToken ct,
        Name? name = null,
        string? usbcId = null,
        BowlerId? id = null)
    {
        var bowler = BowlerFactory.Create(id: id, name: name, usbcId: usbcId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        return bowler;
    }

    // The tournament under test: Scheduled and an Invitational unless the test says otherwise.
    private async Task<Tournament> SeedInvitationalAsync(
        CancellationToken ct,
        Season? season = null,
        TournamentStatus? status = null,
        TournamentType? type = null)
    {
        var target = season ?? _season;
        var tournament = TournamentFactory.Create(
            tournamentType: type ?? TournamentType.Invitational,
            startDate: target.StartDate.AddDays(300),
            endDate: target.StartDate.AddDays(301),
            seasonId: target.Id,
            status: status);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    // One finalized tournament in the season holding a result for each participant.
    private async Task<Tournament> SeedResultTournamentAsync(
        Season season,
        int index,
        IEnumerable<Bowler> participants,
        CancellationToken ct,
        TournamentType? type = null,
        TournamentStatus? status = null,
        bool? statsEligible = null,
        DateOnly? startDate = null)
    {
        var date = startDate ?? season.StartDate.AddDays(index);
        var tournament = TournamentFactory.Create(
            tournamentType: type ?? TournamentType.Singles,
            startDate: date,
            endDate: date,
            seasonId: season.Id,
            statsEligible: statsEligible,
            status: status ?? TournamentStatus.Completed,
            results: [.. participants.Select((bowler, place) =>
                TournamentResultFactory.Create(bowlerId: bowler.Id, place: place + 1))]);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    // Gives each bowler the stated number of results in the season.
    private async Task SeedParticipationAsync(
        Season season,
        CancellationToken ct,
        params (Bowler Bowler, int Tournaments)[] entries)
    {
        var tournamentCount = entries.Max(entry => entry.Tournaments);
        for (var index = 0; index < tournamentCount; index++)
        {
            var participants = entries.Where(entry => entry.Tournaments > index).Select(entry => entry.Bowler).ToArray();
            await SeedResultTournamentAsync(season, index, participants, ct);
        }
    }

    private static string[] Names(ErrorOr<EligibleBowlersDto> result)
        => [.. result.Value.Bowlers.Select(bowler => bowler.Name)];

    public static TheoryData<string> NonInvitationalTypeNames()
        => [.. TournamentType.List.Where(type => type != TournamentType.Invitational).Select(type => type.Name)];

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await CreateHandler().HandleAsync(new ListEligibleBowlersQuery { Id = TournamentId.New() }, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Theory(DisplayName = "HandleAsync includes a bowler only with at least the threshold number of results in the season")]
    [InlineData(Tournament.InvitationalEligibilityThreshold - 1, false, TestDisplayName = "One below the threshold is excluded")]
    [InlineData(Tournament.InvitationalEligibilityThreshold, true, TestDisplayName = "At the threshold is included")]
    [InlineData(Tournament.InvitationalEligibilityThreshold + 1, true, TestDisplayName = "Above the threshold is included")]
    public async Task HandleAsync_ShouldApplyThreshold_ToResultCount(int resultCount, bool expectedIncluded)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Dana", lastName: "Jones"));
        await SeedParticipationAsync(_season, ct, (bowler, resultCount));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Names(result).Contains("Dana Jones").ShouldBe(expectedIncluded);
    }

    [Fact(DisplayName = "HandleAsync lists a bowler with five results once")]
    public async Task HandleAsync_ShouldListBowlerOnce_WhenBowlerHasFiveResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 5));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync does not count results from another season")]
    public async Task HandleAsync_ShouldNotCountResults_FromAnotherSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(otherSeason, ct, (bowler, 5));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync does not add results across seasons: two here and two elsewhere stay excluded")]
    public async Task HandleAsync_ShouldNotCombineResults_AcrossSeasons()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 2));
        await SeedParticipationAsync(otherSeason, ct, (bowler, 2));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync matches the season by SeasonId, not by tournament date")]
    public async Task HandleAsync_ShouldMatchSeasonBySeasonId_NotByDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var included = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "In", lastName: "Season"));
        var excluded = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Date", lastName: "Only"));

        // Three tournaments of this season, dated inside the other season's range: they still count.
        for (var index = 0; index < 3; index++)
        {
            await SeedResultTournamentAsync(_season, index, [included], ct,
                startDate: new DateOnly(2006, 1, 1).AddDays(index));
        }

        // Two tournaments of this season plus one of the other season dated inside this season's range: two count.
        await SeedResultTournamentAsync(_season, 10, [excluded], ct, startDate: new DateOnly(2006, 2, 1));
        await SeedResultTournamentAsync(_season, 11, [excluded], ct, startDate: new DateOnly(2006, 2, 2));
        await SeedResultTournamentAsync(otherSeason, 12, [excluded], ct, startDate: new DateOnly(2020, 1, 1));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Names(result).ShouldBe(["In Season"]);
    }

    [Fact(DisplayName = "HandleAsync counts results from Truncated tournaments")]
    public async Task HandleAsync_ShouldCountTruncatedTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedResultTournamentAsync(_season, 0, [bowler], ct, status: TournamentStatus.Truncated);
        await SeedResultTournamentAsync(_season, 1, [bowler], ct, status: TournamentStatus.Truncated);
        await SeedResultTournamentAsync(_season, 2, [bowler], ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync counts results from tournaments that are not stats eligible")]
    public async Task HandleAsync_ShouldCountNonStatsEligibleTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        for (var index = 0; index < Tournament.InvitationalEligibilityThreshold; index++)
        {
            await SeedResultTournamentAsync(_season, index, [bowler], ct, statsEligible: false);
        }

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync counts a result from every tournament type")]
    public async Task HandleAsync_ShouldCountEveryTournamentType()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var everyType = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Every", lastName: "Type"));
        var index = 0;
        foreach (var type in TournamentType.List)
        {
            await SeedResultTournamentAsync(_season, index++, [everyType], ct, type: type);
        }

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Every Type"]);
    }

    [Theory(DisplayName = "HandleAsync returns an empty list when the Invitational is not Scheduled, even with qualifiers")]
    [InlineData("Completed", TestDisplayName = "Completed")]
    [InlineData("Truncated", TestDisplayName = "Truncated")]
    [InlineData("Cancelled", TestDisplayName = "Cancelled")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenInvitationalIsNotScheduled(string statusName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct, status: TournamentStatus.FromName(statusName));
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 4));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldBeNull();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Theory(DisplayName = "HandleAsync returns an empty list for a Scheduled tournament that is not an Invitational, even with qualifiers")]
    [MemberData(nameof(NonInvitationalTypeNames))]
    public async Task HandleAsync_ShouldReturnEmpty_WhenTournamentIsNotAnInvitational(string typeName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedInvitationalAsync(ct, type: TournamentType.FromName(typeName));
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 4));

        // Act
        var result = await CreateHandler().HandleAsync(Query(tournament), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldBeNull();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns a null USBC ID for a bowler without one")]
    public async Task HandleAsync_ShouldReturnNullUsbcId_WhenBowlerHasNone()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var withId = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Ann", lastName: "Adams"), usbcId: "4821-09317");
        var withoutId = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bob", lastName: "Baker"));
        await SeedParticipationAsync(_season, ct, (withId, 3), (withoutId, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.Select(bowler => bowler.UsbcId).ShouldBe(["4821-09317", null]);
    }

    [Fact(DisplayName = "HandleAsync returns an empty, non-null list when nobody qualifies")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNobodyQualifies()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 2));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldNotBeNull();
        result.Value.Requirements.MinTournamentsCount.ShouldBe(Tournament.InvitationalEligibilityThreshold);
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync sorts by last name then first name regardless of insert order")]
    public async Task HandleAsync_ShouldSortByLastNameThenFirstName_WhenInsertedOutOfOrder()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var zed = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Zed", lastName: "Young"));
        var carl = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Carl", lastName: "Adams"));
        var amy = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Amy", lastName: "Young"));
        var bea = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bea", lastName: "Adams"));
        await SeedParticipationAsync(_season, ct, (zed, 3), (carl, 3), (amy, 3), (bea, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Bea Adams", "Carl Adams", "Amy Young", "Zed Young"]);
    }

    [Fact(DisplayName = "HandleAsync sorts last names ignoring case and treating punctuation by ordinal value")]
    public async Task HandleAsync_ShouldSortOrdinalIgnoreCase_WithMixedCaseAndPunctuation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var obrien = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "Obrien"));
        var apostrophe = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "O'Brien"));
        var lower = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "mcDonald"));
        var upper = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "Nash"));
        await SeedParticipationAsync(_season, ct, (obrien, 3), (apostrophe, 3), (lower, 3), (upper, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert: ordinal ignore-case, so "mcdonald" < "nash" < "o'brien" < "obrien" (' sorts before 'b')
        Names(result).ShouldBe(["Pat mcDonald", "Pat Nash", "Pat O'Brien", "Pat Obrien"]);
    }

    [Fact(DisplayName = "HandleAsync sorts by legal first name while displaying the nickname")]
    public async Task HandleAsync_ShouldSortByFirstName_WhenBowlerHasNickname()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var nicknamed = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Alan", lastName: "Young", nickname: "Zed"));
        var plain = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bill", lastName: "Young"));
        await SeedParticipationAsync(_season, ct, (plain, 3), (nicknamed, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Zed Young", "Bill Young"]);
    }

    [Fact(DisplayName = "HandleAsync breaks a tie between identical names by bowler ID")]
    public async Task HandleAsync_ShouldSortByBowlerId_WhenNamesAreIdentical()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var name = NameFactory.Create(firstName: "Sam", lastName: "Lee");
        var higher = await SeedBowlerAsync(ct, name, usbcId: "2222-22222", id: new BowlerId(HigherBowlerId));
        var lower = await SeedBowlerAsync(ct, name, usbcId: "1111-11111", id: new BowlerId(LowerBowlerId));
        await SeedParticipationAsync(_season, ct, (higher, 3), (lower, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.Select(bowler => bowler.UsbcId).ShouldBe(["1111-11111", "2222-22222"]);
    }
}
```

#### Validator (unit)

`ListEligibleBowlersRequestValidatorTests.cs`. Copy `CheckTeamEligibilityRequestValidatorTests`. Each failing case asserts `result.Errors.ShouldContain(e => e.ErrorCode == "<code>")`.

```csharp
[UnitTest] [Component("Tournaments")]
public sealed class ListEligibleBowlersRequestValidatorTests
{
    private const string ValidId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E"; // 26 chars

    [Fact(DisplayName = "Validate should succeed when the tournament ID is 26 characters")]
    public void Validate_ShouldSucceed_WhenIdIsValid();           // result.IsValid.ShouldBeTrue()

    [Theory(DisplayName = "Validate should fail with TournamentIdRequired when the ID is empty")]
    [InlineData("", TestDisplayName = "Empty")] [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Validate_ShouldFailWithRequired_WhenIdIsEmpty(string id);   // "ListEligibleBowlersRequest.TournamentIdRequired"

    [Theory(DisplayName = "Validate should fail with TournamentIdInvalidLength when the ID is not 26 characters")]
    [InlineData(25, TestDisplayName = "25 characters")] [InlineData(27, TestDisplayName = "27 characters")]
    public void Validate_ShouldFailWithInvalidLength_WhenIdIsWrongLength(int length);   // new string('A', length); "ListEligibleBowlersRequest.TournamentIdInvalidLength"
}
```

#### Endpoint (unit, Verify snapshot)

`ListEligibleBowlersEndpointTests.cs`. The snapshot `ListEligibleBowlersEndpointTests.HandleAsync_ShouldReturnOkWithMappedBowlers_WhenQuerySucceeds.verified.txt` does not exist yet: the first run writes a `.received.txt` that a person reviews and accepts.

```csharp
using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[UnitTest]
[Component("Tournaments")]
public sealed class ListEligibleBowlersEndpointTests
{
    private static Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>> HandlerReturning(
        ErrorOr<EligibleBowlersDto> result,
        CancellationToken ct,
        Action<ListEligibleBowlersQuery>? capture = null)
    {
        var mock = new Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>>(MockBehavior.Strict);
        mock.Setup(handler => handler.HandleAsync(It.IsAny<ListEligibleBowlersQuery>(), ct))
            .Callback<ListEligibleBowlersQuery, CancellationToken>((query, _) => capture?.Invoke(query))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should return OK with the mapped bowlers when the query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedBowlers_WhenQuerySucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = EligibleBowlersDtoFactory.Create(bowlers: EligibleBowlerSummaryDtoFactory.Bogus(3, 2201));
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should pass the route tournament ID to the query")]
    public async Task HandleAsync_ShouldBuildQueryFromRoute()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournamentId = TournamentId.New();
        ListEligibleBowlersQuery? captured = null;
        var handler = HandlerReturning(
            EligibleBowlersDtoFactory.Create(bowlers: EligibleBowlerSummaryDtoFactory.Bogus(1, 2202)), ct, query => captured = query);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = tournamentId.Value.ToString() }, ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(tournamentId);
    }

    [Fact(DisplayName = "HandleAsync should map a bowler without a USBC ID to a null USBC ID")]
    public async Task HandleAsync_ShouldMapNullUsbcId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = EligibleBowlersDtoFactory.Create(
            bowlers: [EligibleBowlerSummaryDtoFactory.Create(name: "Pat O'Brien", usbcId: null)]);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        var item = endpoint.Response.Bowlers.ShouldHaveSingleItem();
        item.Name.ShouldBe("Pat O'Brien");
        item.UsbcId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return 200 with null requirements when the tournament has no list")]
    public async Task HandleAsync_ShouldReturnNullRequirements_WhenListDoesNotApply()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(
            HandlerReturning(EligibleBowlersDtoFactory.CreateNotApplicable(), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Requirements.ShouldBeNull();
        endpoint.Response.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(Error.NotFound(), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 when the query returns a non-NotFound error")]
    public async Task HandleAsync_ShouldReturn500_WhenQueryReturnsOtherError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(
            HandlerReturning(Error.Unexpected(description: "An unexpected error occurred."), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact(DisplayName = "Configure should register an anonymous GET route ending in eligible-bowlers")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(route => route.EndsWith("/eligible-bowlers", StringComparison.Ordinal));
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
```

#### Pipeline (integration, no database)

`ListEligibleBowlersPipelineTests.cs`. Copy `CheckTeamEligibilityPipelineTests`, including its `[SuppressMessage]` for CA2213, `UsePropertyNamingPolicy = false`, and stopping (never disposing) the app. The mock type is `IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>`, and the filter also lists `ListEligibleBowlersRequestValidator`.

```csharp
[IntegrationTest] [Component("Tournaments")]
public sealed class ListEligibleBowlersPipelineTests : IAsyncLifetime
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";
    // InitializeAsync / DisposeAsync copied from CheckTeamEligibilityPipelineTests

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 200 with the requirements and bowlers")]
    public async Task Get_ShouldReturn200WithItems_WhenRequestIsValid();
    // handler returns EligibleBowlersDtoFactory.Create(bowlers: ...Bogus(3, 2201)); GET; ReadFromJsonAsync<EligibleBowlersResponse>
    // Assert: 200, Requirements.MinTournamentsCount == 3, Bowlers.Count == 3, captured query Id.Value.ToString() == TournamentId

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 400 when the ID is not 26 characters")]
    public async Task Get_ShouldReturn400_WhenIdIsTooShort();      // GET "/tournaments/short/eligible-bowlers" -> BadRequest

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 404 when the handler reports a missing tournament")]
    public async Task Get_ShouldReturn404_WhenTournamentIsMissing();   // ReturnsAsync(TournamentErrors.TournamentNotFound(id)) -> NotFound
}
```

#### Query and cache descriptor (unit)

`ListEligibleBowlersQueryTests.cs`:

```csharp
[UnitTest] [Component("Tournaments")]
public sealed class ListEligibleBowlersQueryTests
{
    [Fact(DisplayName = "Expiry should be 1 day")]
    public void Expiry_ShouldBe1Day();                     // query.Expiry.ShouldBe(TimeSpan.FromDays(1))

    [Fact(DisplayName = "Cache key should be neba:tournaments:{id}:eligible-bowlers with no caller scope")]
    public void Cache_Key_ShouldFollowExpectedFormat();    // Key.ShouldBe($"neba:tournaments:{id}:eligible-bowlers")

    [Fact(DisplayName = "Cache key should be specific to the tournament")]
    public void Cache_Key_ShouldBeSpecificToTournament();  // two IDs -> keys differ

    [Fact(DisplayName = "Cache tags should contain neba, neba:tournaments, the tournament tag and the eligible-bowlers tag")]
    public void Cache_Tags_ShouldContainExpectedTags();

    [Fact(DisplayName = "Cache tags should contain exactly 4 tags")]
    public void Cache_Tags_ShouldContainExactly4Tags();    // Tags.Count.ShouldBe(4)
}
```

Add to `tests/Neba.Api.Tests/Features/Tournaments/TournamentCacheTagTests.cs`:

```csharp
[Fact(DisplayName = "The eligible-bowlers descriptor should carry the tag the results sync, delete and edit evictions clear")]
public void EligibleBowlers_ShouldCarryTheEvictionTag()
{
    // Arrange
    var id = TournamentId.New();

    // Act
    var descriptor = CacheDescriptors.Tournaments.EligibleBowlers(id);

    // Assert
    CacheDescriptors.Tournaments.EligibleBowlersTag.ShouldBe("neba:tournaments:eligible-bowlers");
    descriptor.Key.ShouldBe($"neba:tournaments:{id}:eligible-bowlers");
    descriptor.Tags.ShouldBe(
        ["neba", CacheDescriptors.Tournaments.Tag, $"neba:tournaments:{id}", CacheDescriptors.Tournaments.EligibleBowlersTag],
        ignoreOrder: true);
}
```

#### Summary (unit)

`ListEligibleBowlersSummaryTests.cs`: follow `ListChampionsSummaryTests` (facts for Summary, Description and the 200 response) and add facts for 400 and 404 if `CheckTeamEligibilitySummaryTests` asserts them. At minimum assert `Description.ShouldNotBeNullOrWhiteSpace()`.

#### Eviction (each test added to an existing class)

Each test caches a value under the tag, runs the code, then asserts that a second `GetOrSetAsync` calls its factory. The pattern is at `SyncTournamentResultsJobTests` near line 336.

```csharp
// tests/Neba.Api.Tests/Legacy/Tournaments/Complete/SyncTournamentResultsJobTests.cs
[Fact(DisplayName = "SyncAsync should evict the eligible-bowlers cache tag after saving")]
public async Task SyncAsync_ShouldEvictEligibleBowlersCacheTag_AfterSaving();
// Arrange: CreateTournamentAsync(42, TournamentType.Singles, complete: true, ct); CreateBowlerAsync(100, ct); stats + results rows as near line 336.
//          cache.GetOrSetAsync("eligible-bowlers-cache-test", _ => Task.FromResult("stale-cached-value"), tags: [CacheDescriptors.Tournaments.EligibleBowlersTag], token: ct)
// Act: await CreateJob().SyncAsync(42, ct)
// Assert: GetOrSetAsync(same key, _ => Task.FromResult("fresh-value")) returns "fresh-value"

// tests/Neba.Api.Tests/Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandlerTests.cs
[Fact(DisplayName = "HandleAsync evicts the eligible-bowlers cache tag when the tournament is deleted")]
public async Task HandleAsync_ShouldEvictEligibleBowlersTag_WhenTournamentIsDeleted();

// tests/Neba.Api.Tests/Features/Tournaments/EditTournament/EditTournamentCommandHandlerTests.cs
[Fact(DisplayName = "HandleAsync evicts the eligible-bowlers cache tag when the edit moves the tournament to another season")]
public async Task HandleAsync_ShouldEvictEligibleBowlersTag_WhenSeasonChanges();
// Arrange: two seasons via SeedSeasonAsync; tournament in the first; command dates fall in the second; stale value under the tag.

[Fact(DisplayName = "HandleAsync keeps the eligible-bowlers cache tag when the edit stays in the same season")]
public async Task HandleAsync_ShouldKeepEligibleBowlersTag_WhenSeasonUnchanged();
// Assert: the stale value is still returned (the GetOrSetAsync factory is not called).
```

### Notes and not checked

- The e2e mock `tests/e2e/mock-api/mock-api-server.ts` needs a `GET /tournaments/:id/eligible-bowlers` route with its own pattern before the generic tournament-detail match. It lands with the Phase 2 page. For `MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID` it returns `requirements.minTournamentsCount` 3 and a small list (one row without a USBC ID); for any other tournament it returns `requirements` null and no bowlers.
- Nothing here was compiled or run.
- Assumed and unverified: `TournamentType.FromName` (SmartEnum; `TournamentStatus.FromName` has precedent), `TheoryData<string>` built from a collection expression under the repo's xunit v3, the `CheckTeamEligibility*` test shapes the drafts copy, and that a 2005 to 2009 season passes any season-overlap check.
- Unverified in production: the SQL EF writes for the `SelectMany`, `GroupBy` and subquery (an integration test proves one statement); whether `DeleteTournament` or `EditTournament` allow changes to a finalized tournament; whether anything other than `SyncTournamentResultsJob` writes `TournamentResult` rows.

## Phase 2: UI

Code draft. Paths are under `src/Neba.Website.Server/` unless they start with `tests/`. Nothing here has been compiled, and it cannot compile until Phase 1 adds the contracts.

### Where it goes

In `Tournaments/Detail/TournamentDetail.razor`, inside `td-body__main`, after the Articles block and before the Results block. A `Scheduled` Invitational has no results, so the two never both show. The section sits in main content, not the rail, because a long list does not fit the rail.

### Behavior

The code below implements these rules.

- Heading "Eligible bowlers". Subtitle "Bowlers with a result in {n} or more tournaments this season.", with {n} from `Requirements.MinTournamentsCount`.
- Loading: the `neba-spinner` the detail page uses, under the heading. The rest of the page is not blocked.
- Error: "Could not load the eligible bowlers." with a "Try again" button. The heading stays.
- `Requirements` null (the API says the list does not apply, for example the status changed since the page loaded): render nothing.
- Empty list: "No bowlers have reached {n} tournaments yet this season." No search box. Use Humanizer `ToQuantity` for counted nouns (ADR-0010).
- Count line when not empty, such as "42 bowlers"; while filtering, "3 of 42 bowlers".
- No match: "No bowlers match “{term}”."
- Search box: a plain `<input>` with a label, `autocomplete="off"`, bound on `oninput` and filtered in C# (the `NebaAutocomplete` pattern; `docs/learnings/blazor-ui.md` allows it for free-text boxes). No `@onkeydown`, no JavaScript. Check for typing lag on a phone.
- Match rules: ignore case; every word of the term must appear in the name, in any order; ignore dashes and spaces when matching a USBC ID, and only try the ID when the term has a digit; trim the term; an empty term shows everyone; a curly apostrophe matches a straight one, because phones type the curly one.
- List: a two-column table (Name, USBC ID) with `<th scope="col">` headers inside a box of fixed maximum height (about 28rem) with `overflow-y: auto` and a sticky header. A bowler with no ID shows "No USBC ID". Two columns, so no `neba-data-table` class. Row `@key` is the index. The scroll box takes `tabindex="0"` and an accessible name so keyboard users can scroll it.
- No `DirtyFormGuard` (no data entry). The page's `PageTitle` and render mode do not change. Use `[Parameter, EditorRequired]`, `@` on component attribute values, and no `$"..."` in `@code`.

### Inspiration

`web/src/features/registrations/BowlerSearch.tsx` in `bowlingmegabucks/tournament-director` shows a bold name over a muted USBC ID line, a "No USBC ID" fallback, a count heading, and distinct empty, loading and error states. It searches on the server on submit, so only the look carries over.

### Mockups

Both show the new section only, with 24 sample bowlers, a working filter and a control to switch between list, empty, loading and error states. **Option 1 is picked (2026-11-25).** Take layout and structure from it; use the site's theme variables and existing classes in place of its inline values. Its review control is not part of the page.

- `docs/plans/mockups/invitational-eligible-bowlers/option-1.html`: matches the page's other sections (section rule, gold-topped table like Results), search box beside the heading.
- `docs/plans/mockups/invitational-eligible-bowlers/option-2.html`: a search-first card that asks "Are you on the list?", with a full-width search box, striped rows and the count in a footer.

### Code

#### `Tournaments/Detail/EligibleBowlersViewModel.cs` (new)

```csharp
namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>The bowlers eligible for a tournament and the rule the list was built from.</summary>
public sealed record EligibleBowlersViewModel
{
    /// <summary>False when the tournament has no eligibility list.</summary>
    public required bool ListApplies { get; init; }

    /// <summary>Tournaments a bowler must have a result in; null when no such rule applies.</summary>
    public int? MinTournamentsCount { get; init; }

    /// <summary>The eligible bowlers, in the order the API returned them.</summary>
    public required IReadOnlyList<EligibleBowlerViewModel> Bowlers { get; init; }
}
```

#### `Tournaments/Detail/EligibleBowlerViewModel.cs` (new)

The keys are set once, in the `init` accessors, so `with` and record equality stay correct.

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>A bowler on an eligibility list.</summary>
public sealed record EligibleBowlerViewModel
{
    /// <summary>The bowler's display name.</summary>
    public required string Name
    {
        get;
        init
        {
            field = value;
            NameKey = NormalizeName(value);
        }
    }

    /// <summary>The bowler's USBC ID; null when none is on file.</summary>
    public string? UsbcId
    {
        get;
        init
        {
            field = value;
            UsbcKey = value is null ? string.Empty : NormalizeUsbcId(value);
        }
    }

    /// <summary>The name as searched: lower case, straight apostrophes.</summary>
    public string NameKey { get; private init; } = string.Empty;

    /// <summary>The USBC ID as searched: no dashes or spaces; empty when there is no ID.</summary>
    public string UsbcKey { get; private init; } = string.Empty;

    // Every word must be in the name, in any order. The ID is tried only when the term has a digit.
    internal bool Matches(string term)
    {
        var trimmed = term.Trim();

        if (trimmed.Length == 0)
        {
            return true;
        }

        var words = NormalizeName(trimmed).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (words.All(word => NameKey.Contains(word, StringComparison.Ordinal)))
        {
            return true;
        }

        return UsbcKey.Length > 0
            && trimmed.Any(char.IsAsciiDigit)
            && UsbcKey.Contains(NormalizeUsbcId(trimmed), StringComparison.OrdinalIgnoreCase);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "A lower-case search key, not a security comparison.")]
    private static string NormalizeName(string value)
        => value.ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'');

    private static string NormalizeUsbcId(string value)
        => string.Concat(value.Where(character => character != '-' && !char.IsWhiteSpace(character)));
}
```

#### `Tournaments/Detail/EligibleBowlerMappingExtensions.cs` (new)

```csharp
using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.Website.Server.Tournaments.Detail;

internal static class EligibleBowlerMappingExtensions
{
    extension(EligibleBowlersResponse response)
    {
        public EligibleBowlersViewModel ToViewModel() => new()
        {
            ListApplies = response.Requirements is not null,
            MinTournamentsCount = response.Requirements?.MinTournamentsCount,
            Bowlers = [.. response.Bowlers.Select(bowler => bowler.ToViewModel())]
        };
    }

    extension(EligibleBowlerSummaryResponse response)
    {
        public EligibleBowlerViewModel ToViewModel() => new()
        {
            Name = response.Name,
            UsbcId = response.UsbcId
        };
    }
}
```

#### `Tournaments/ITournamentApiService.cs`

Add `using Neba.Website.Server.Tournaments.Detail;` (before `...Tournaments.Schedule`), and after `GetChampionsDataAsync`:

```csharp
    Task<ErrorOr<EligibleBowlersViewModel>> GetEligibleBowlersAsync(string tournamentId, CancellationToken ct);
```

#### `Tournaments/TournamentApiService.cs`

Add `using Neba.Website.Server.Tournaments.Detail;` (before `...Tournaments.Schedule`), and after `GetChampionsDataAsync`, before the first `private static ... MapToViewModel`:

```csharp
    public async Task<ErrorOr<EligibleBowlersViewModel>> GetEligibleBowlersAsync(
        string tournamentId, CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(
            "TournamentsApi",
            nameof(GetEligibleBowlersAsync),
            token => tournamentsApi.ListEligibleBowlersAsync(tournamentId, token),
            ct);

        return result.IsError
            ? result.Errors
            : result.Value.ToViewModel();
    }
```

DI needs no change (`Program.cs:45` already registers the service).

#### `Tournaments/Detail/EligibleBowlers.razor` (new)

`_Imports.razor` already covers `Humanizer` and `Neba.Website.Server.Tournaments`. `ApiExecutor` does not throw on a cancelled call (it returns an error), so the component checks the token after the await.

```razor
@implements IDisposable
@inject ITournamentApiService TournamentApiService

@if (_isLoading || _loadFailed || _list is { ListApplies: true })
{
    <div class="eligible-bowlers">
        <div class="eligible-bowlers__head">
            <div>
                <h2 class="td-section-title">Eligible bowlers</h2>
                @if (_list is not null)
                {
                    <span class="td-section-subtitle">@Subtitle</span>
                }
            </div>
            @if (_list is { Bowlers.Count: > 0 })
            {
                <div class="eligible-bowlers__tools">
                    <label class="sr-only" for="eligible-bowlers-search">Search eligible bowlers by name or USBC ID</label>
                    <input id="eligible-bowlers-search" type="search" class="neba-input eligible-bowlers__search"
                           placeholder="Search by name or USBC ID" autocomplete="off"
                           value="@_term" @oninput="HandleSearchInput" />
                </div>
            }
        </div>

        @if (_isLoading)
        {
            <div class="eligible-bowlers__loading" role="status">
                <div class="neba-spinner" aria-hidden="true"></div>
                <span class="sr-only">Loading eligible bowlers…</span>
            </div>
        }
        else if (_loadFailed)
        {
            <div class="eligible-bowlers__error" role="alert">
                <p>Could not load the eligible bowlers.</p>
                <button type="button" class="neba-btn neba-btn-secondary neba-btn-sm eligible-bowlers__retry"
                        @onclick="LoadAsync">
                    Try again
                </button>
            </div>
        }
        else if (_list!.Bowlers.Count == 0)
        {
            <p class="eligible-bowlers__empty">@EmptyText</p>
        }
        else
        {
            <div class="eligible-bowlers__panel">
                @if (_shown.Count > 0)
                {
                    <div class="eligible-bowlers__scroll" tabindex="0" role="region" aria-label="Eligible bowlers list">
                        <table class="eligible-bowlers__table">
                            <thead>
                                <tr>
                                    <th scope="col">Name</th>
                                    <th scope="col">USBC ID</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var (index, bowler) in _shown.Index())
                                {
                                    <tr @key="index">
                                        <td>@bowler.Name</td>
                                        <td>
                                            @if (bowler.UsbcId is null)
                                            {
                                                <span class="eligible-bowlers__no-id">No USBC ID</span>
                                            }
                                            else
                                            {
                                                @bowler.UsbcId
                                            }
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <p class="eligible-bowlers__no-match">No bowlers match “@(_term.Trim())”.</p>
                }
            </div>

            <p class="eligible-bowlers__count" role="status">@CountText</p>
        }
    </div>
}

@code {
    private readonly CancellationTokenSource _disposed = new();

    private EligibleBowlersViewModel? _list;
    private IReadOnlyList<EligibleBowlerViewModel> _shown = [];
    private string _term = string.Empty;
    private bool _isLoading = true;
    private bool _loadFailed;

    [Parameter, EditorRequired]
    public string TournamentId { get; set; } = default!;

    private string Subtitle
        => _list?.MinTournamentsCount is { } minimum
            ? "Bowlers with a result in " + minimum + " or more "
                + "tournament".ToQuantity(minimum, ShowQuantityAs.None) + " this season."
            : "Bowlers who have qualified this season.";

    private string EmptyText
        => _list?.MinTournamentsCount is { } minimum
            ? "No bowlers have reached "
                + "tournament".ToQuantity(minimum, formatProvider: DisplayCulture.English) + " yet this season."
            : "No bowlers have qualified yet this season.";

    private string CountText
    {
        get
        {
            var total = "bowler".ToQuantity(_list!.Bowlers.Count, formatProvider: DisplayCulture.English);

            return string.IsNullOrWhiteSpace(_term)
                ? total
                : _shown.Count + " of " + total;
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    public void Dispose()
    {
        _disposed.Cancel();
        _disposed.Dispose();
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        _loadFailed = false;

        var result = await TournamentApiService.GetEligibleBowlersAsync(TournamentId, _disposed.Token);

        // The visitor left, or the status changed and the page dropped this section.
        if (_disposed.IsCancellationRequested)
        {
            return;
        }

        _isLoading = false;

        if (result.IsError)
        {
            _loadFailed = true;
            return;
        }

        _list = result.Value;
        _shown = _list.Bowlers;
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        _term = e.Value?.ToString() ?? string.Empty;
        _shown = [.. _list!.Bowlers.Where(bowler => bowler.Matches(_term))];
    }
}
```

Behaviour notes:
- When `MinTournamentsCount` is null but the list applies, the text says "have qualified" with no number.
- The subtitle appears only after load, because the number is unknown before then.
- With `ListApplies` false, nothing renders.
- The count line is a `role="status"` region, so screen readers hear "0 of 42 bowlers" when nothing matches.

#### `Tournaments/Detail/EligibleBowlers.razor.css` (new)

```css
.eligible-bowlers__head {
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: flex-end;
    gap: 1rem;
    margin-bottom: 0.75rem;
    padding-top: 0.75rem;
    border-top: 2px solid var(--neba-gray-200);
}

/* Same look as the page's section heading, whose rules are scoped to TournamentDetail. */
.td-section-title {
    margin: 0;
    font-family: var(--neba-font-display);
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--neba-text);
}

.td-section-subtitle {
    font-size: 0.8125rem;
    color: var(--neba-gray-500);
}

.eligible-bowlers__tools {
    width: 16.25rem;
}

.eligible-bowlers__panel {
    background: var(--neba-bg-panel);
    border-top: 2px solid var(--tournaments-gold);
}

.eligible-bowlers__scroll {
    max-height: 28rem;
    overflow-y: auto;
}

.eligible-bowlers__scroll:focus-visible {
    outline: 2px solid var(--neba-blue-500);
    outline-offset: 2px;
}

.eligible-bowlers__table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9375rem;
    color: var(--neba-text);
}

.eligible-bowlers__table th {
    position: sticky;
    top: 0;
    padding: 0.625rem 1rem;
    background: var(--neba-bg-panel);
    box-shadow: inset 0 -1px var(--neba-surface-highest);
    text-align: left;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--neba-gray-500);
}

.eligible-bowlers__table td {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--neba-surface-high);
    overflow-wrap: anywhere;
}

.eligible-bowlers__table th:last-child,
.eligible-bowlers__table td:last-child {
    text-align: right;
    white-space: nowrap;
    font-variant-numeric: tabular-nums;
}

.eligible-bowlers__table tbody tr:hover {
    background: color-mix(in srgb, var(--neba-blue-700) 4%, transparent);
}

.eligible-bowlers__no-id,
.eligible-bowlers__count {
    font-size: 0.8125rem;
    color: var(--neba-gray-500);
}

.eligible-bowlers__count {
    margin: 0.5rem 0 0;
}

.eligible-bowlers__loading {
    display: flex;
    justify-content: center;
    padding: 1.75rem 1rem;
}

.eligible-bowlers__empty,
.eligible-bowlers__no-match,
.eligible-bowlers__error {
    margin: 0;
    padding: 1.75rem 1rem;
    text-align: center;
    color: var(--neba-gray-500);
    overflow-wrap: anywhere;
}

.eligible-bowlers__retry {
    margin-top: 0.625rem;
}

@media (max-width: 767px) {
    .eligible-bowlers__tools {
        width: 100%;
    }

    /* Leaves room around the box so a swipe can still scroll the page. */
    .eligible-bowlers__scroll {
        max-height: min(28rem, 60vh);
    }

    .eligible-bowlers__table th,
    .eligible-bowlers__table td {
        padding-inline: 0.75rem;
    }
}
```

The table has two columns, so it does not take `neba-data-table`.

#### `Tournaments/Detail/TournamentDetailViewModel.cs`

Insert after `ShowTeamEligibilityLink`:

```csharp
    /// <summary>
    /// A scheduled Invitational lists the bowlers who qualify to bowl it.
    /// </summary>
    public bool ShowEligibleBowlers => IsScheduled && TournamentType == "Invitational";
```

#### `Tournaments/Detail/TournamentDetail.razor`

Insert between the Articles block (closes at line 386) and `@if (_model.HasResults)` (line 388):

```razor
                    @if (_model.HasArticles)
                    {
                        <section class="tournament-detail__section">
                            ...
                        </section>
                    }

                    @if (_model.ShowEligibleBowlers)
                    {
                        <section class="tournament-detail__section">
                            <EligibleBowlers TournamentId="@Id" />
                        </section>
                    }

                    @if (_model.HasResults)
                    {
                        <section class="tournament-detail__section tournament-detail__results">
```

Truncate or Cancel reloads the model, which removes and disposes the component.

#### `Tournaments/Detail/TournamentDetail.razor.css`

Add under "Section chrome". Without it, a "does not apply" answer leaves an empty section that still takes a 2.5rem flex gap.

```css
.tournament-detail__section:empty {
    display: none;
}
```

### Tests

Adding the interface method breaks the test build until the two hand-written fakes below gain it. The mapping and service tests use the Phase 1 response factories.

#### Factories (`tests/Neba.TestFactory/Tournaments/`)

`EligibleBowlerViewModelFactory.cs` (assumes public records and computed keys; if the keys are `required init`, the factory must set them):

```csharp
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlerViewModelFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    public static EligibleBowlerViewModel Create(
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyList<EligibleBowlerViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        // Every third row has no USBC ID, so any collection of 3 or more holds both shapes.
        return [.. Enumerable.Range(0, count).Select(index => Create(
            name: faker.Name.FullName(),
            usbcId: index % 3 == 2 ? null : faker.Random.Replace("####-#####")))];
    }

    public static IReadOnlyList<EligibleBowlerViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
```

`EligibleBowlersViewModelFactory.cs`:

```csharp
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlersViewModelFactory
{
    public const bool ValidListApplies = true;
    public const int ValidMinTournamentsCount = 3;

    public static EligibleBowlersViewModel Create(
        bool? listApplies = null,
        int? minTournamentsCount = ValidMinTournamentsCount,
        IReadOnlyList<EligibleBowlerViewModel>? bowlers = null)
        => new()
        {
            ListApplies = listApplies ?? ValidListApplies,
            MinTournamentsCount = minTournamentsCount,
            Bowlers = bowlers ?? [EligibleBowlerViewModelFactory.Create()]
        };

    public static EligibleBowlersViewModel CreateNotApplicable()
        => Create(listApplies: false, minTournamentsCount: null, bowlers: []);

    internal static IReadOnlyCollection<EligibleBowlersViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlers: EligibleBowlerViewModelFactory.Bogus(faker.Random.Int(1, 5), faker)))];
    }

    public static IReadOnlyCollection<EligibleBowlersViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
```

#### `tests/Neba.Website.Tests/Tournaments/Detail/EligibleBowlersTests.cs`

Markup assumptions: rows are `__table tbody tr` (name first cell, ID second); `__no-id` is inside the ID cell; `__scroll` carries `tabindex` and `aria-label`; the input has a `<label for>`.

```csharp
using Bunit;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EligibleBowlers")]
public sealed class EligibleBowlersTests : IDisposable
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private readonly BunitContext _ctx = new();
    private readonly Mock<ITournamentApiService> _service = new(MockBehavior.Strict);

    public EligibleBowlersTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton(_service.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show the heading and loading state while the list is pending")]
    public void Render_ShouldShowLoading_WhilePending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<EligibleBowlersViewModel>>();
        _service
            .Setup(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find("h2").TextContent.Trim().ShouldBe("Eligible bowlers");
        cut.Find(".eligible-bowlers__loading").ShouldNotBeNull();
        cut.FindAll(".eligible-bowlers__table, .eligible-bowlers__search, .eligible-bowlers__empty, .eligible-bowlers__error").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should load the list for the TournamentId parameter once")]
    public void OnInit_ShouldLoadOnce_ForTournamentId()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create());

        // Act
        RenderComponent();

        // Assert
        _service.Verify(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should show names and USBC IDs in the order returned")]
    public void Render_ShouldShowRows_InReturnedOrder()
    {
        // Arrange: not alphabetical, so a re-sort in the component would fail.
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            EligibleBowlerViewModelFactory.Create(name: "Zed Young", usbcId: "2222-22222"),
            EligibleBowlerViewModelFactory.Create(name: "Ann Adams", usbcId: "1111-11111")
        ]));

        // Act
        var cut = RenderComponent();

        // Assert
        Names(cut).ShouldBe(["Zed Young", "Ann Adams"]);
        Ids(cut).ShouldBe(["2222-22222", "1111-11111"]);
        cut.FindAll(".eligible-bowlers__table th[scope=col]").Select(th => th.TextContent.Trim()).ShouldBe(["Name", "USBC ID"]);
    }

    [Fact(DisplayName = "Should show No USBC ID for a bowler without one")]
    public void Render_ShouldShowNoUsbcId_WhenBowlerHasNone()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers: [EligibleBowlerViewModelFactory.Create(usbcId: null)]));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__no-id").TextContent.Trim().ShouldBe("No USBC ID");
    }

    [Fact(DisplayName = "Should take the number in the subtitle from the response")]
    public void Render_ShouldShowThresholdInSubtitle()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: 5));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Markup.ShouldContain("Bowlers with a result in 5 or more tournaments this season.");
    }

    [Fact(DisplayName = "Should give the scroll box keyboard focus and a name, and label the search box")]
    public void Render_ShouldBeAccessible()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create());

        // Act
        var cut = RenderComponent();

        // Assert
        var scroll = cut.Find(".eligible-bowlers__scroll");
        scroll.GetAttribute("tabindex").ShouldBe("0");
        scroll.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        var search = cut.Find("input.eligible-bowlers__search");
        search.GetAttribute("autocomplete").ShouldBe("off");
        cut.Find($"label[for='{search.Id}']").TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "Should show the empty text with the response's number and no search box")]
    [InlineData(3, "No bowlers have reached 3 tournaments yet this season.", TestDisplayName = "Three")]
    [InlineData(5, "No bowlers have reached 5 tournaments yet this season.", TestDisplayName = "Five")]
    public void Render_ShouldShowEmptyText_WhenNobodyQualifies(int minimum, string expected)
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: minimum, bowlers: []));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__empty").TextContent.Trim().ShouldBe(expected);
        cut.FindAll(".eligible-bowlers__search, .eligible-bowlers__table, .eligible-bowlers__count").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render nothing when the list does not apply")]
    public void Render_ShouldRenderNothing_WhenListDoesNotApply()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.CreateNotApplicable());

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Markup.ShouldBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Should show the error text and a Try again button, keeping the heading")]
    public void Render_ShouldShowError_WhenLoadFails()
    {
        // Arrange
        SetupResult(Error.Failure());

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find("h2").TextContent.Trim().ShouldBe("Eligible bowlers");
        cut.Find(".eligible-bowlers__error").TextContent.ShouldContain("Could not load the eligible bowlers.");
        cut.Find("button.eligible-bowlers__retry").TextContent.Trim().ShouldBe("Try again");
        cut.FindAll(".eligible-bowlers__table, .eligible-bowlers__empty").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should reload and show the list when Try again succeeds")]
    public void Retry_ShouldShowList_WhenSecondLoadSucceeds()
    {
        // Arrange
        _service
            .SetupSequence(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)Error.Failure())
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)EligibleBowlersViewModelFactory.Create());
        var cut = RenderComponent();

        // Act
        cut.Find("button.eligible-bowlers__retry").Click();

        // Assert
        Names(cut).ShouldBe([EligibleBowlerViewModelFactory.ValidName]);
        cut.FindAll(".eligible-bowlers__error").ShouldBeEmpty();
        _service.Verify(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "Should show loading and hide the error while a retry is pending")]
    public void Retry_ShouldShowLoading_WhilePending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<EligibleBowlersViewModel>>();
        _service
            .SetupSequence(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorOr<EligibleBowlersViewModel>)Error.Failure())
            .Returns(pending.Task);
        var cut = RenderComponent();

        // Act
        cut.Find("button.eligible-bowlers__retry").Click();

        // Assert
        cut.Find(".eligible-bowlers__loading").ShouldNotBeNull();
        cut.FindAll(".eligible-bowlers__error").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should count the bowlers with a singular or plural noun")]
    [InlineData(1, "1 bowler", TestDisplayName = "One")]
    [InlineData(24, "24 bowlers", TestDisplayName = "Many")]
    public void Render_ShouldShowCount(int count, string expected)
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers: EligibleBowlerViewModelFactory.Bogus(count)));

        // Act
        var cut = RenderComponent();

        // Assert
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe(expected);
    }

    [Fact(DisplayName = "Should show the filtered and total counts while filtering")]
    public void Search_ShouldShowFilteredCount()
    {
        // Arrange
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            .. Enumerable.Range(0, 21).Select(_ => EligibleBowlerViewModelFactory.Create()),
            .. Enumerable.Range(0, 3).Select(_ => EligibleBowlerViewModelFactory.Create(name: "Zed Quill"))
        ]));
        var cut = RenderComponent();

        // Act
        cut.Find("input.eligible-bowlers__search").Input("quill");

        // Assert
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe("3 of 24 bowlers");
    }

    [Theory(DisplayName = "Should filter by name or USBC ID")]
    [InlineData("smith", "Dave Smith", TestDisplayName = "Part of a name")]
    [InlineData("SMITH", "Dave Smith", TestDisplayName = "Ignores case")]
    [InlineData("smith dave", "Dave Smith", TestDisplayName = "Words in any order")]
    [InlineData("  jones  ", "Mary Ann Jones", TestDisplayName = "Trims the term")]
    [InlineData("o'bri", "Pat O'Brien", TestDisplayName = "Apostrophe")]
    [InlineData("4821-09317", "Dave Smith", TestDisplayName = "ID with dash")]
    [InlineData("482109317", "Dave Smith", TestDisplayName = "ID without dash")]
    [InlineData("4821 09317", "Dave Smith", TestDisplayName = "ID with a space")]
    [InlineData("11-112", "Pat O'Brien", TestDisplayName = "Part of an ID with a misplaced dash")]
    public void Search_ShouldFilter(string term, string expected)
    {
        // Arrange
        var cut = RenderSearchableList();

        // Act
        cut.Find("input.eligible-bowlers__search").Input(term);

        // Assert
        Names(cut).ShouldBe([expected]);
    }

    [Theory(DisplayName = "Should show the no-match message with the term")]
    [InlineData("zzz", TestDisplayName = "Unknown name")]
    [InlineData("dave jones", TestDisplayName = "Every word must match the same bowler")]
    [InlineData("-", TestDisplayName = "A term with no digit does not search IDs")]
    public void Search_ShouldShowNoMatch(string term)
    {
        // Arrange
        var cut = RenderSearchableList();

        // Act
        cut.Find("input.eligible-bowlers__search").Input(term);

        // Assert
        cut.Find(".eligible-bowlers__no-match").TextContent.Trim().ShouldBe($"No bowlers match “{term}”.");
        Names(cut).ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should restore the full list when the term is cleared")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Search_ShouldRestoreList_WhenCleared(string cleared)
    {
        // Arrange
        var cut = RenderSearchableList();
        cut.Find("input.eligible-bowlers__search").Input("zzz");

        // Act
        cut.Find("input.eligible-bowlers__search").Input(cleared);

        // Assert
        Names(cut).ShouldBe(["Dave Smith", "Pat O'Brien", "Mary Ann Jones"]);
        cut.FindAll(".eligible-bowlers__no-match").ShouldBeEmpty();
        cut.Find(".eligible-bowlers__count").TextContent.Trim().ShouldBe("3 bowlers");
    }

    private void SetupResult(ErrorOr<EligibleBowlersViewModel> result)
        => _service
            .Setup(x => x.GetEligibleBowlersAsync(TournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private IRenderedComponent<EligibleBowlers> RenderComponent()
        => _ctx.Render<EligibleBowlers>(p => p.Add(x => x.TournamentId, TournamentId));

    private IRenderedComponent<EligibleBowlers> RenderSearchableList()
    {
        SetupResult(EligibleBowlersViewModelFactory.Create(bowlers:
        [
            EligibleBowlerViewModelFactory.Create(name: "Dave Smith", usbcId: "4821-09317"),
            EligibleBowlerViewModelFactory.Create(name: "Pat O'Brien", usbcId: "1111-22222"),
            EligibleBowlerViewModelFactory.Create(name: "Mary Ann Jones", usbcId: null)
        ]));
        return RenderComponent();
    }

    private static string[] Names(IRenderedComponent<EligibleBowlers> cut)
        => [.. cut.FindAll(".eligible-bowlers__table tbody tr > :first-child").Select(cell => cell.TextContent.Trim())];

    private static string[] Ids(IRenderedComponent<EligibleBowlers> cut)
        => [.. cut.FindAll(".eligible-bowlers__table tbody tr > :nth-child(2)").Select(cell => cell.TextContent.Trim())];
}
```

#### View model test (add to `TournamentDetailViewModelTests.cs`)

```csharp
[Theory(DisplayName = "ShowEligibleBowlers should be true only for a scheduled Invitational")]
[InlineData("Invitational", "Scheduled", true)]
[InlineData("Invitational", "Completed", false)]
[InlineData("Invitational", "Truncated", false)]
[InlineData("Invitational", "Cancelled", false)]
[InlineData("Singles", "Scheduled", false)]
[InlineData("Doubles", "Scheduled", false)]
public void ShowEligibleBowlers_ShouldRequireScheduledInvitational(string tournamentType, string status, bool expected)
{
    // Arrange
    var model = TournamentDetailViewModelFactory.Create(tournamentType: tournamentType, status: status);

    // Act
    var show = model.ShowEligibleBowlers;

    // Assert
    show.ShouldBe(expected);
}
```

#### Page tests (`TournamentDetailTests.cs`)

What the existing tests need:
- The constructor registers `ITournamentsApi`, `ISponsorsApi`, `IRulesSectionService`, `IClientTimeZoneService`, `ApiExecutor`, `ToastService` and `HelpDocumentService`. It does **not** register `ITournamentApiService`.
- The factory default is `Singles`, so only one existing test renders a Scheduled Invitational: `Render_ShouldShowRulesLink_WhenScheduledInvitational`. It fails without both the registration and a setup. The completed-Invitational rules test does not render the component.
- Add `using Neba.Website.Server.Tournaments;`, the field, the registration, and the helper. Call the helper in that one test's Arrange. Keep the setup out of the constructor so the "no call" tests stay strict.

```csharp
private readonly Mock<ITournamentApiService> _mockTournamentService;

// constructor
_mockTournamentService = new Mock<ITournamentApiService>(MockBehavior.Strict);
_ctx.Services.AddSingleton(_mockTournamentService.Object);

private void SetupEligibleBowlers()
    => _mockTournamentService
        .Setup(x => x.GetEligibleBowlersAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(EligibleBowlersViewModelFactory.Create());
```

New tests:

```csharp
[Fact(DisplayName = "Should show the eligible bowlers in the main column for a scheduled Invitational")]
public void Render_ShouldShowEligibleBowlers_WhenScheduledInvitational()
{
    // Arrange
    SetupSuccessResponse(TournamentDetailResponseFactory.Create(
        status: TournamentStatus.Scheduled, tournamentType: TournamentType.Invitational));
    SetupEligibleBowlers();

    // Act
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

    // Assert
    cut.Find(".td-body__main .tournament-detail__section .eligible-bowlers")
        .TextContent.ShouldContain(EligibleBowlerViewModelFactory.ValidName);
    _mockTournamentService.Verify(
        x => x.GetEligibleBowlersAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()),
        Times.Once);
}

[Theory(DisplayName = "Should not load or show eligible bowlers unless the tournament is a scheduled Invitational")]
[InlineData("Invitational", "Completed", TestDisplayName = "Completed Invitational")]
[InlineData("Invitational", "Truncated", TestDisplayName = "Truncated Invitational")]
[InlineData("Invitational", "Cancelled", TestDisplayName = "Cancelled Invitational")]
[InlineData("Singles", "Scheduled", TestDisplayName = "Scheduled Singles")]
[InlineData("Doubles", "Scheduled", TestDisplayName = "Scheduled Doubles")]
public void Render_ShouldNotShowEligibleBowlers_WhenNotScheduledInvitational(string typeName, string statusName)
{
    // Arrange
    SetupSuccessResponse(TournamentDetailResponseFactory.Create(
        status: TournamentStatus.FromName(statusName),
        tournamentType: TournamentType.FromName(typeName)));

    // Act
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

    // Assert
    cut.FindAll(".eligible-bowlers").ShouldBeEmpty();
    _mockTournamentService.Verify(
        x => x.GetEligibleBowlersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
        Times.Never);
}

[Fact(DisplayName = "Should remove the eligible bowlers once the Invitational is cancelled")]
public void ConfirmCancel_ShouldRemoveEligibleBowlers_WhenCancelSucceeds()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.ManageTournamentStatus.PolicyName);
    SetupSuccessResponseSequence(
        TournamentDetailResponseFactory.Create(status: TournamentStatus.Scheduled, tournamentType: TournamentType.Invitational),
        TournamentDetailResponseFactory.Create(status: TournamentStatus.Cancelled, tournamentType: TournamentType.Invitational));
    SetupEligibleBowlers();

    using var cancelResponse = new StubApiResponse<object>
    {
        IsSuccessStatusCode = true,
        StatusCode = System.Net.HttpStatusCode.NoContent
    };
    _mockApi
        .Setup(x => x.CancelTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(cancelResponse);

    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
    cut.Find("button.td-hero__cancel-btn").Click();

    // Act
    cut.Find("button.confirm-action-modal-confirm").Click();

    // Assert
    cut.FindAll(".eligible-bowlers").ShouldBeEmpty();
}
```

#### Mapping and service

These need the Phase 1 response factories (`EligibleBowlersResponseFactory`, `EligibleBowlerSummaryResponseFactory`, `EligibilityRequirementsResponseFactory`). They are only drafted in the plan and do not exist yet.

`Tournaments/Detail/EligibleBowlerMappingExtensionsTests.cs`:

```csharp
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EligibleBowlerMappingExtensions")]
public sealed class EligibleBowlerMappingExtensionsTests
{
    [Fact(DisplayName = "Maps the requirement and the bowlers in order")]
    public void ToViewModel_ShouldMapResponse()
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.Create(
            requirements: EligibilityRequirementsResponseFactory.Create(minTournamentsCount: 5),
            bowlers:
            [
                EligibleBowlerSummaryResponseFactory.Create(name: "Zed Young", usbcId: "2222-22222"),
                EligibleBowlerSummaryResponseFactory.Create(name: "Ann Adams", usbcId: null)
            ]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.ListApplies.ShouldBeTrue();
        viewModel.MinTournamentsCount.ShouldBe(5);
        viewModel.Bowlers.Select(b => b.Name).ShouldBe(["Zed Young", "Ann Adams"]);
        viewModel.Bowlers.Select(b => b.UsbcId).ShouldBe(["2222-22222", null]);
    }

    [Fact(DisplayName = "Maps null requirements to a list that does not apply")]
    public void ToViewModel_ShouldNotApply_WhenRequirementsAreNull()
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.CreateNotApplicable();

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.ListApplies.ShouldBeFalse();
        viewModel.MinTournamentsCount.ShouldBeNull();
        viewModel.Bowlers.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Builds a lower-case name key and a USBC ID key without dashes or spaces")]
    [InlineData("4821-09317", "482109317", TestDisplayName = "Dash")]
    [InlineData("48 21-09317", "482109317", TestDisplayName = "Space and dash")]
    [InlineData(null, "", TestDisplayName = "No ID")]
    public void ToViewModel_ShouldBuildSearchKeys(string? usbcId, string expectedKey)
    {
        // Arrange
        var response = EligibleBowlersResponseFactory.Create(
            bowlers: [EligibleBowlerSummaryResponseFactory.Create(name: "Pat O'Brien", usbcId: usbcId)]);

        // Act
        var bowler = response.ToViewModel().Bowlers.ShouldHaveSingleItem();

        // Assert
        bowler.NameKey.ShouldBe("pat o'brien");
        bowler.UsbcKey.ShouldBe(expectedKey);
    }
}
```

Add to `Tournaments/TournamentApiServiceTests.cs` (usings: `Neba.Api.Contracts.Tournaments.ListEligibleBowlers`):

```csharp
[Fact(DisplayName = "GetEligibleBowlersAsync should return the mapped list when API succeeds")]
public async Task GetEligibleBowlersAsync_ShouldReturnMappedList_WhenApiSucceeds()
{
    // Arrange
    SetupEligibleBowlers(EligibleBowlersResponseFactory.Create());

    // Act
    var result = await _service.GetEligibleBowlersAsync(EligibleTournamentId, TestContext.Current.CancellationToken);

    // Assert
    result.IsError.ShouldBeFalse();
    result.Value.ListApplies.ShouldBeTrue();
    result.Value.Bowlers.ShouldHaveSingleItem().Name.ShouldBe(EligibleBowlerSummaryResponseFactory.ValidName);
}

[Fact(DisplayName = "GetEligibleBowlersAsync should report no list when requirements are null")]
public async Task GetEligibleBowlersAsync_ShouldNotApply_WhenRequirementsAreNull()
{
    // Arrange
    SetupEligibleBowlers(EligibleBowlersResponseFactory.CreateNotApplicable());

    // Act
    var result = await _service.GetEligibleBowlersAsync(EligibleTournamentId, TestContext.Current.CancellationToken);

    // Assert
    result.IsError.ShouldBeFalse();
    result.Value.ListApplies.ShouldBeFalse();
}

[Theory(DisplayName = "GetEligibleBowlersAsync should pass the API error through")]
[InlineData(System.Net.HttpStatusCode.NotFound, ErrorType.NotFound, TestDisplayName = "Not found")]
[InlineData(System.Net.HttpStatusCode.InternalServerError, ErrorType.Failure, TestDisplayName = "Server error")]
public async Task GetEligibleBowlersAsync_ShouldReturnError_WhenApiFails(System.Net.HttpStatusCode status, ErrorType expected)
{
    // Arrange
    SetupEligibleBowlers(null, status);

    // Act
    var result = await _service.GetEligibleBowlersAsync(EligibleTournamentId, TestContext.Current.CancellationToken);

    // Assert
    result.IsError.ShouldBeTrue();
    result.FirstError.Type.ShouldBe(expected);
}

private const string EligibleTournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

private void SetupEligibleBowlers(
    EligibleBowlersResponse? content,
    System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK)
{
    using var response = new StubApiResponse<EligibleBowlersResponse>
    {
        IsSuccessStatusCode = status == System.Net.HttpStatusCode.OK,
        StatusCode = status,
        Content = content
    };

    _mockTournamentsApi
        .Setup(x => x.ListEligibleBowlersAsync(EligibleTournamentId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);
}
```

The theory also needs `using ErrorOr;`.

#### Required edits to the two fakes

Both need `using Neba.Website.Server.Tournaments.Detail;`.

`History/Champions/ChampionsPageTests.cs` (`FakeChampionsService`) and `Tournaments/Schedule/TournamentsPageTests.cs` (`FakeTournamentDataService`), same member in each:

```csharp
Task<ErrorOr<EligibleBowlersViewModel>>
    ITournamentApiService.GetEligibleBowlersAsync(string tournamentId, CancellationToken ct)
    => throw new NotSupportedException();
```

The other users of the interface are Moq mocks and need no change.

#### Extra cases (add to `EligibleBowlersTests`)

```csharp
[Fact(DisplayName = "Should word the subtitle and empty text without a number when no minimum applies")]
public void Render_ShouldOmitNumber_WhenMinimumIsNull()
{
    // Arrange
    SetupResult(EligibleBowlersViewModelFactory.Create(minTournamentsCount: null, bowlers: []));

    // Act
    var cut = RenderComponent();

    // Assert
    cut.Markup.ShouldContain("Bowlers who have qualified this season.");
    cut.Find(".eligible-bowlers__empty").TextContent.Trim().ShouldBe("No bowlers have qualified yet this season.");
}

[Fact(DisplayName = "Should match a straight apostrophe when the term has a curly one")]
public void Search_ShouldTreatCurlyApostropheAsStraight()
{
    // Arrange
    var cut = RenderSearchableList();

    // Act
    cut.Find("input.eligible-bowlers__search").Input("O\u2019Brien");

    // Assert
    Names(cut).ShouldBe(["Pat O'Brien"]);
}
```

Also test that disposing the component while the load is pending raises no error.

#### Playwright spec and mock

**1. `tests/e2e/mock-api/mock-api-server.ts`**

Put the function after the `MOCK_TOURNAMENT_INVITATIONAL_*` fixtures, so the constant is declared before use:

```ts
// Invitational eligible bowlers (TournamentDetail.spec.ts): only the upcoming Invitational has a
// list. One bowler has no USBC ID.
function resolveEligibleBowlersRoute(pathname: string): object | null {
  const match = /^\/tournaments\/([^/]+)\/eligible-bowlers$/.exec(pathname);
  if (!match) return null;

  if (match[1] !== MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID) {
    return { requirements: null, bowlers: [] };
  }

  return {
    requirements: { minTournamentsCount: 3 },
    bowlers: [
      { name: 'Alice Anders', usbcId: '2222-00001' },
      { name: 'Ben Baxter', usbcId: null },
      { name: 'Carla Cole', usbcId: '2222-00003' },
    ],
  };
}
```

In `resolveGetRoute`, add it before `resolveTournamentDetailRoute`:

```ts
    resolveTeamEligibilityRoute(pathname, searchParams) ??
    resolveEligibleBowlersRoute(pathname) ??
    resolveBowlerSearchRoute(pathname, searchParams) ??
```

**2. `tests/e2e/TournamentDetail.spec.ts`**

Add to the existing import:

```ts
import {
  MOCK_TOURNAMENT_INVITATIONAL_PAST_ID,
  MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID,
  MOCK_TOURNAMENT_TEAM_ID,
  perProjectTournamentId,
} from './mock-api/mock-api-server';
```

Append:

```ts
test.describe('Tournament Detail page — eligible bowlers', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('a scheduled Invitational lists and filters eligible bowlers, and a past one has no section', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID}`);
    await page.waitForSelector('.eligible-bowlers__table tbody tr');

    const rows = page.locator('.eligible-bowlers__table tbody tr');
    await expect(rows).toHaveCount(3);

    // The page is server-rendered until its Blazor circuit attaches, and text typed before then is
    // lost, so refill until the list narrows.
    await expect(async () => {
      await page.locator('.eligible-bowlers__search').fill('baxter');
      await expect(rows).toHaveCount(1, { timeout: 1000 });
    }).toPass({ timeout: 20000 });
    await expect(rows).toContainText('Ben Baxter');

    await page.goto(`/tournaments/${MOCK_TOURNAMENT_INVITATIONAL_PAST_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('.eligible-bowlers')).toHaveCount(0);
  });
});
```

The retry follows `ChangePassword.spec.ts`.

### Docs

A user-facing page changes, so add or update the help doc under `docs/help/` (ADR-0007) as its own docs commit.

### Notes and not checked

- `.td-section-title` and `.td-section-subtitle` are scoped to `TournamentDetail.razor.css`, so `EligibleBowlers.razor.css` restates them. Moving them to `neba_theme.css` would restyle the "Manage Sponsors" heading, so this plan does not.
- The loading state uses `neba-spinner`, so it skips the delay and minimum display time that `docs/architecture/blazor.md` describes under "Loading States".
- Check at build time: the `field` keyword accessors and the `SuppressMessage` placement under `AnalysisMode All` with warnings as errors; the Razor `Index()` loop; whether `.tournament-detail__section:empty` matches the markup Blazor produces.
- Check in a browser: phone layout, the nested scroll box, typing lag on a phone (each keystroke is a round trip), dark mode, and keyboard focus after "Try again" (the button leaves the page).
- Not handled: accented names ("Chloe" does not match "Chloé"). The component loads only in `OnInitializedAsync`, so direct navigation between two tournament pages would not reload it.
- The e2e mock returns 200 with null requirements for any unknown ID, where the real API returns 404. The e2e check on the past Invitational proves no section renders, not that no call was made; bUnit proves that.
- Not run: the unit, bUnit and Playwright tests.

## Commit plan

Implement in this order, in one session, with no subagents. Each commit builds and passes its own tests, and holds the code and tests for it. All commit messages end with the attribution lines the session gives for commits.

1. `Add invitational eligible bowlers plan`
   - Files: `docs/plans/invitational-eligible-bowlers.md`, `docs/plans/mockups/invitational-eligible-bowlers/option-1.html`, `option-2.html`
   - Why: the plan is on record before code changes.
2. `Keep feature-plan agents to planning`
   - Files: `.claude/skills/feature-plan/SKILL.md`, `.claude/skills/feature-plan/references/implementation-guide.md`
   - Why: an unrelated skill change; implementation now runs without subagents to save usage.
3. `Define Invitational Eligibility`
   - Files: `docs/ubiquitous-language.md`
   - Why: the term is settled before types use it.
4. `Add eligible bowlers cache tag and eviction`
   - Files: `Features/Tournaments/Domain/Tournament.cs` (constant), `Caching/CacheDescriptors.cs`, `Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs`, `DeleteTournamentCommandHandler.cs`, `EditTournamentCommandHandler.cs`; tests: `TournamentCacheTagTests`, `SyncTournamentResultsJobTests`, delete and edit handler tests
   - Why: the list must never be cached without a way to clear it.
5. `Add eligible bowlers query`
   - Files: `Features/Tournaments/ListEligibleBowlers/` (query, handler, three DTOs); DTO factories; handler and query tests
   - Why: the rule is reviewable on its own, against a real database.
6. `Add eligible bowlers endpoint`
   - Files: the same folder (request, validator, endpoint, summary), `Neba.Api.Contracts/Tournaments/ListEligibleBowlers/` (three responses), `ITournamentsApi.cs`; response factories; validator, endpoint (with the accepted Verify snapshot), pipeline and summary tests
   - Why: exposes the query and nothing more than names and USBC IDs.
7. `Mock the eligible bowlers route`
   - Files: `tests/e2e/mock-api/mock-api-server.ts`
   - Why: the mock exists before any page calls the route.
8. `Load eligible bowlers in the tournament service`
   - Files: the three new files in `Tournaments/Detail/` (two view models, mapping), `ITournamentApiService.cs`, `TournamentApiService.cs`; the two fakes in `ChampionsPageTests` and `TournamentsPageTests`; view-model factories; mapping and service tests
   - Why: the data path lands without any visible change.
9. `Add eligible bowlers component`
   - Files: `EligibleBowlers.razor`, `EligibleBowlers.razor.css`, `EligibleBowlersTests`
   - Why: search, states and accessibility are reviewed apart from the page.
10. `Show eligible bowlers on Invitational details`
   - Files: `TournamentDetailViewModel.cs`, `TournamentDetail.razor`, `TournamentDetail.razor.css`; view-model and page tests, including the mock for the existing scheduled Invitational rules test
   - Why: the one commit that makes the feature visible.
11. `Cover the eligible bowlers section`
    - Files: `tests/e2e/TournamentDetail.spec.ts`
    - Why: proves the page, service and mock together in a browser.
12. `Document the eligible bowlers list`
    - Files: new `docs/help/invitational-eligibility.md`
    - Why: a public page changed (ADR-0007). Use the `help-documentation` skill, which takes screenshots from the running page.
