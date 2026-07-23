# Oil Pattern Reveal Date

Adds a nullable `OilPatternRevealDateTime` to tournaments that gates how much oil pattern detail is shown on tournament list/detail views: authenticated users always see full details, unauthenticated (public) users see only length/ratio categories until the reveal date/time passes, after which everyone sees full details.

## Decisions locked in during scoping

- **Field naming**: domain/DTO property `OilPatternRevealDateTime` (`DateTimeOffset?`); database column `oil_pattern_reveal_date_utc` (explicit `.HasColumnName(...)`, not the snake_case default of the property name).
- **"Full details" vs "reduced info"**: full = pattern `Name`, `KegelId`, `Length`, `Volume`, `LeftRatio`/`RightRatio` (Kegel link, exact numbers); reduced = `PatternLengthCategory`/`PatternRatioCategory` only (categories, no exact numbers or pattern identity). This reduced/full split does not exist in the read model today — `Volume`/`LeftRatio`/`RightRatio` aren't projected into any DTO yet, so this feature also adds them (guarded by the new gating logic).
- **Who sees full details before the reveal date passes**: any **authenticated** user (not gated by a specific permission) — "a benefit of being a registered user," per the site's model. Anonymous/public visitors see reduced info until the reveal date/time passes; after it passes, everyone (including anonymous) sees full details.
- **A distinct, narrower "management" permission collection is still being added in this feature** (`TournamentManagementPermissions = [CreateTournament]`, mirroring `ArticleManagementPermissions`/`SponsorManagementPermissions`) for **general tournament-management authorization purposes** (e.g. a future edit-tournament policy) — it is *not* what gates oil-pattern visibility (that's just "is the caller authenticated"), but this feature is the natural point to introduce it since it's touching the same permission-shaping pattern (`GetArticleQueryHandler`'s `CallerHasArticleManagementPermission` style) for the first time in Tournaments. Expected to grow to include `EditTournament` once that endpoint exists.
- **Reveal date visibility**: the `OilPatternRevealDateTime` value itself (e.g. "Reveals Jul 30, 2026") is shown on the tournament detail page to any **authenticated** user, regardless of whether it's in the past or future — same authenticated-vs-anonymous split as the pattern details themselves.
- **Cache invalidation**: a one-shot Hangfire job scheduled via `IBackgroundJobScheduler.Schedule(job, revealDateTimeOffset)` at tournament create time (only when `OilPatternRevealDateTime` is set and in the future) evicts the tournament's detail + season-list cache tags at the exact reveal moment. Cache keys are also split by authenticated/anonymous scope (mirroring `CacheDescriptors.News.Article(slug, callerHasManagementPermission)`) so a stale anonymous-scoped entry can't leak reduced info to an authenticated caller or vice versa before the job fires.
- **UI placement**: the reveal date/time input lives in `CreateTournament.razor`'s Oil Pattern `<section>`, above/outside `OilPatternPicker`, so it's present regardless of which of the 3 oil-pattern modes (No Pattern / Pick Existing / Create New) is selected.
- **Time zone**: the reveal date/time is entered and displayed in **the viewer's own browser-local time**, not a fixed NEBA time zone. Example: a staff member on the West Coast enters 5:00 PM on 8/15/26; it's stored as the equivalent UTC instant; a viewer on the East Coast sees it take effect at 8:00 PM their time; if an East Coast staff member later edits that same tournament, the field should redisplay as 8:00 PM (their local time), not 5:00 PM. **You asked that this apply to every datetime field in the application, not just this one** — a repo review turned up 4 places already doing local-time conversion independently (`NewsList.razor`, `NewsDetail.razor`, `CreateArticle.razor`, `EditArticle.razor`, all around `Article.PublishDateUtc`, each reimplementing the same JS-interop-offset logic) and one place displaying a `DateTimeOffset` with **no** conversion at all (`Documents/LastUpdated`, shown across `NebaDocument.razor`/`TournamentRules.razor`/`Bylaws.razor`). Rather than add a 6th ad hoc implementation, this plan extracts one shared `IClientTimeZoneService` (Phase 2) and migrates all of the above onto it — this is a real, if modest, scope increase beyond "just the reveal date field," done because you asked for the behavior consistently, not as scope creep on my part.
- **Time zone correctness upgrade, not just consolidation**: the existing News implementation caches the browser's raw UTC offset-in-minutes (`browser-time.js`'s `getTimezoneOffsetMinutes()`) at first render and reuses that fixed offset for every conversion afterward. That's wrong for any date whose DST status differs from "right now" — for a near-term article publish date this rarely bites, but a tournament's oil pattern reveal date is often set months ahead and very plausibly straddles a DST boundary (e.g. a winter announcement with a spring reveal). The shared service instead captures the browser's **IANA time zone ID** (`Intl.DateTimeFormat().resolvedOptions().timeZone`) and does the conversion server-side via `TimeZoneInfo`, which is correct for the DST rules in effect at the *specific* date being converted, not just "now."
- **No "sign in to see more" nudge** — the reduced (anonymous, pre-reveal) view shows only the category chip(s) and nothing else. No call-to-action, no mention that more detail exists.
- **No Edit Tournament page exists yet** — this feature only touches Create + read views; editing the reveal date after creation is out of scope until an Edit Tournament feature exists. Two follow-ups noted for that future work: (1) the scheduled cache-eviction job will need to be rescheduled/cancelled on edit; (2) the edit form will need to redisplay the stored UTC value converted into *that* viewer's own local time (same JS-interop time-zone-capture approach as Create, reused for display instead of just input).

## Phase 1: API

### Domain (`src/Neba.Api/Features/Tournaments/Domain/`)

**`Tournament.cs`** (edit)

```csharp
/// <summary>
/// Gets the date/time at which full oil pattern details become visible to unauthenticated
/// visitors, or <see langword="null"/> if there is no reveal restriction (full details are
/// always visible). Authenticated users always see full details regardless of this value.
/// </summary>
public DateTimeOffset? OilPatternRevealDateTime { get; init; }
```

`Create(...)` gains one more optional parameter (kept last, after the existing optional params):

```csharp
public static ErrorOr<Tournament> Create(
    string name,
    TournamentType tournamentType,
    DateOnly startDate,
    DateOnly endDate,
    SeasonId seasonId,
    bool statsEligible,
    decimal entryFee,
    CertificationNumber? bowlingCenterId = null,
    Uri? externalRegistrationUrl = null,
    StoredFile? logo = null,
    PatternLengthCategory? patternLengthCategory = null,
    PatternRatioCategory? patternRatioCategory = null,
    DateTimeOffset? oilPatternRevealDateTime = null)
{
    // ...existing validation unchanged...

    var tournament = new Tournament
    {
        // ...existing initializers unchanged...
        OilPatternRevealDateTime = oilPatternRevealDateTime
    };

    return tournament;
}
```

**`OilPatternRevealPolicy.cs`** (new file, same folder)

```csharp
namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Determines whether full oil pattern details should be visible for a given tournament
/// and caller, based on the tournament's <see cref="Tournament.OilPatternRevealDateTime"/>.
/// </summary>
internal static class OilPatternRevealPolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when full oil pattern details should be shown: the caller
    /// is authenticated, there is no reveal date set, or the reveal date has already passed.
    /// </summary>
    public static bool IsRevealed(DateTimeOffset? revealDateTime, bool callerIsAuthenticated, DateTimeOffset now) =>
        callerIsAuthenticated || revealDateTime is null || revealDateTime <= now;
}
```

### Database (`src/Neba.Api/Database/Configurations/TournamentConfiguration.cs`)

```csharp
builder.Property(tournament => tournament.OilPatternRevealDateTime)
    .HasColumnName("oil_pattern_reveal_date_utc");
```

(placed after the `PatternRatioCategory` property mapping, near the other oil-pattern-related columns)

Migration:

```bash
dotnet ef migrations add AddOilPatternRevealDateTimeToTournaments --project src/Neba.Api --startup-project src/Neba.Api
```

### Caching (`src/Neba.Api/Caching/CacheDescriptors.cs`)

```csharp
private const string ManagementScope = "management";
private const string PublicScope = "public";
private const string AuthenticatedScope = "authenticated";
```

```csharp
public static class Tournaments
{
    public static CacheDescriptor ListForSeason(SeasonId seasonId, bool callerIsAuthenticated)
        => new()
        {
            Key = $"neba:tournaments:{seasonId}:list:scope:{(callerIsAuthenticated ? AuthenticatedScope : PublicScope)}",
            Tags = ["neba", "neba:tournaments", $"neba:tournaments:{seasonId}"]
        };

    public static CacheDescriptor TournamentDetail(TournamentId id, bool callerIsAuthenticated)
        => new()
        {
            Key = $"neba:tournaments:{id}:scope:{(callerIsAuthenticated ? AuthenticatedScope : PublicScope)}",
            Tags = ["neba", "neba:tournaments", $"neba:tournaments:{id}"]
        };

    // ListChampions, Types unchanged
}
```

Tags are unchanged, so `CreateTournamentCommandHandler`'s existing `cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", ...)` call keeps working with no edit needed.

### Application — Commands (`src/Neba.Api/Features/Tournaments/CreateTournament/`)

**`CreateTournamentCommand.cs`** — add:

```csharp
public DateTimeOffset? OilPatternRevealDateTime { get; init; }
```

**`CreateTournamentCommandHandler.cs`**

```csharp
internal sealed class CreateTournamentCommandHandler(
    AppDbContext appDbContext,
    IFusionCache cache,
    IBackgroundJobScheduler jobScheduler,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTournamentCommand, TournamentId>
{
    public async Task<ErrorOr<TournamentId>> HandleAsync(CreateTournamentCommand command, CancellationToken cancellationToken)
    {
        // ...season lookup, bowling center check, oil pattern lookup unchanged...

        var tournamentResult = Tournament.Create(
            name: command.Name,
            tournamentType: command.TournamentType,
            startDate: command.StartDate,
            endDate: command.EndDate,
            seasonId: season.Id,
            statsEligible: command.StatsEligible,
            entryFee: command.EntryFee,
            bowlingCenterId: command.BowlingCenterId,
            externalRegistrationUrl: command.ExternalRegistrationUrl,
            logo: command.Logo,
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory,
            oilPatternRevealDateTime: command.OilPatternRevealDateTime);

        if (tournamentResult.IsError)
        {
            return tournamentResult.Errors;
        }

        var tournament = tournamentResult.Value;

        await appDbContext.Tournaments.AddAsync(tournament, cancellationToken);

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, tournament.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", token: cancellationToken);

        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            jobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = season.Id },
                revealAt);
        }

        return tournament.Id;
    }
}
```

### BackgroundJobs (`src/Neba.Api/Features/Tournaments/EvictOilPatternRevealCache/`, new folder)

**`EvictOilPatternRevealCacheJob.cs`**

```csharp
using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;

internal sealed record EvictOilPatternRevealCacheJob
    : IBackgroundJob
{
    public required TournamentId TournamentId { get; init; }

    public required SeasonId SeasonId { get; init; }

    public string JobName
        => $"Evict Oil Pattern Reveal Cache: {TournamentId}";
}
```

**`EvictOilPatternRevealCacheJobHandler.cs`**

```csharp
using Neba.Api.BackgroundJobs;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;

internal sealed class EvictOilPatternRevealCacheJobHandler(IFusionCache cache)
    : IBackgroundJobHandler<EvictOilPatternRevealCacheJob>
{
    public async Task ExecuteAsync(EvictOilPatternRevealCacheJob job, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync($"neba:tournaments:{job.TournamentId}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{job.SeasonId}", token: cancellationToken);
    }
}
```

No DI registration edit needed — picked up by the existing `IBackgroundJobHandler<>` Scrutor scan in `BackgroundJobConfiguration.cs`.

**Follow-up noted, not built now**: once an Edit Tournament feature exists, editing `OilPatternRevealDateTime` must call `IBackgroundJobScheduler.Delete(jobId)` on the old job (its ID would need to be persisted or looked up) before scheduling a new one — today a tournament can only be created once, so no reschedule path exists yet.

### Application — Queries

**`GetTournament/GetTournamentQuery.cs`**

```csharp
internal sealed record GetTournamentQuery
    : ICachedQuery<ErrorOr<TournamentDetailDto>>
{
    public required TournamentId Id { get; init; }

    public required bool CallerIsAuthenticated { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.TournamentDetail(Id, CallerIsAuthenticated);

    public TimeSpan Expiry
        => TimeSpan.FromDays(5);
}
```

**`GetTournament/GetTournamentQueryHandler.cs`** — inject `TimeProvider`; extend the `OilPatterns` projection and shape the result:

```csharp
internal sealed class GetTournamentQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService,
    TimeProvider timeProvider)
    : IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>
{
    // ...unchanged fields...

    public async Task<ErrorOr<TournamentDetailDto>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        var row = await _tournaments
            .Where(tournament => tournament.Id == query.Id)
            .Select(tournament => new
            {
                // ...existing projected fields unchanged...
                tournament.OilPatternRevealDateTime,
                OilPatterns = tournament.OilPatterns.Select(top => new
                {
                    top.OilPattern.Name,
                    top.OilPattern.Length,
                    top.OilPattern.Volume,
                    top.OilPattern.LeftRatio,
                    top.OilPattern.RightRatio,
                    top.TournamentRounds,
                    top.OilPattern.KegelId
                }).ToList(),
                // ...Articles etc. unchanged...
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return TournamentErrors.TournamentNotFound(query.Id);
        }

        // ...historicalWinners / historicalResults / historicalEntryCount / sponsors unchanged...

        var revealed = OilPatternRevealPolicy.IsRevealed(
            row.OilPatternRevealDateTime, query.CallerIsAuthenticated, timeProvider.GetUtcNow());

        return new TournamentDetailDto
        {
            // ...unchanged fields...
            PatternLengthCategory = row.PatternLengthCategory,
            PatternRatioCategory = row.PatternRatioCategory,
            OilPatternRevealDateTime = query.CallerIsAuthenticated ? row.OilPatternRevealDateTime : null,
            OilPatterns = revealed
                ? row.OilPatterns.ConvertAll(pattern => new TournamentDetailOilPatternDto
                {
                    Name = pattern.Name,
                    Length = pattern.Length,
                    Volume = pattern.Volume,
                    LeftRatio = pattern.LeftRatio,
                    RightRatio = pattern.RightRatio,
                    TournamentRounds = [.. pattern.TournamentRounds.Select(r => r.Name)],
                    KegelId = pattern.KegelId,
                })
                : [],
            // ...LogoUrl / Winners / Results / EntryCount / Articles unchanged...
        };
    }
}
```

**`GetTournament/TournamentDetailDto.cs`** — add:

```csharp
/// <summary>
/// Date/time at which full oil pattern details become public; null if there's no restriction
/// or the caller isn't authenticated (unauthenticated callers don't see that a reveal date exists).
/// </summary>
public DateTimeOffset? OilPatternRevealDateTime { get; init; }
```

**`GetTournament/TournamentDetailOilPatternDto.cs`** — add:

```csharp
/// <summary>
/// The oil volume applied, in milliliters.
/// </summary>
public required decimal Volume { get; init; }

/// <summary>
/// The forward (head) to reverse (tail) oil ratio on the pattern's left side.
/// </summary>
public required decimal LeftRatio { get; init; }

/// <summary>
/// The forward (head) to reverse (tail) oil ratio on the pattern's right side.
/// </summary>
public required decimal RightRatio { get; init; }
```

**`GetTournament/GetTournamentEndpoint.cs`** — build the query with the auth flag, map the new fields:

```csharp
public override async Task HandleAsync(GetTournamentRequest req, CancellationToken ct)
{
    var query = new GetTournamentQuery
    {
        Id = new TournamentId(req.TournamentId),
        CallerIsAuthenticated = User.Identity?.IsAuthenticated == true
    };
    var result = await _queryHandler.HandleAsync(query, ct);

    // ...error handling unchanged...

    var dto = result.Value;

    var response = new TournamentDetailResponse
    {
        // ...existing mappings unchanged...
        PatternLengthCategory = dto.PatternLengthCategory,
        PatternRatioCategory = dto.PatternRatioCategory,
        OilPatternRevealDateTime = dto.OilPatternRevealDateTime,
        OilPatterns = [.. dto.OilPatterns.Select(op => new TournamentDetailOilPatternResponse
        {
            Name = op.Name,
            Length = op.Length,
            Volume = op.Volume,
            LeftRatio = op.LeftRatio,
            RightRatio = op.RightRatio,
            Rounds = op.TournamentRounds,
            KegelId = op.KegelId,
        })],
        // ...remaining mappings unchanged...
    };

    // Stryker disable once Statement
    await Send.OkAsync(response, ct);
}
```

**`ListTournamentsInSeason/ListTournamentsInSeasonQuery.cs`**

```csharp
internal sealed record ListTournamentsInSeasonQuery
    : ICachedQuery<IReadOnlyCollection<SeasonTournamentDto>>
{
    public required SeasonId SeasonId { get; init; }

    public required bool CallerIsAuthenticated { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.ListForSeason(SeasonId, CallerIsAuthenticated);

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}
```

**`ListTournamentsInSeason/ListTournamentsInSeasonQueryHandler.cs`** — inject `TimeProvider`, extend the oil-pattern projection the same way as `GetTournament`, and gate `OilPatterns` per row:

```csharp
internal sealed class ListTournamentsInSeasonQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService,
    TimeProvider timeProvider)
    : IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>
{
    // ...unchanged fields...

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> HandleAsync(ListTournamentsInSeasonQuery query, CancellationToken cancellationToken)
    {
        var rows = await _tournaments
            .Where(tournament => tournament.SeasonId == query.SeasonId)
            .Select(tournament => new
            {
                // ...existing projected fields unchanged...
                tournament.OilPatternRevealDateTime,
                OilPatterns = tournament.OilPatterns.Select(top => new
                {
                    top.OilPattern.Name,
                    top.OilPattern.Length,
                    top.OilPattern.Volume,
                    top.OilPattern.LeftRatio,
                    top.OilPattern.RightRatio,
                    top.OilPattern.KegelId,
                    top.TournamentRounds
                }).ToList()
            }).ToListAsync(cancellationToken);

        // ...historicalWinners lookup unchanged...

        var now = timeProvider.GetUtcNow();

        return [.. rows.Select(row =>
        {
            // ...sponsors mapping unchanged...

            var revealed = OilPatternRevealPolicy.IsRevealed(row.OilPatternRevealDateTime, query.CallerIsAuthenticated, now);

            return new SeasonTournamentDto
            {
                // ...unchanged fields...
                PatternLengthCategory = row.PatternLengthCategory,
                PatternRatioCategory = row.PatternRatioCategory,
                OilPatternRevealDateTime = query.CallerIsAuthenticated ? row.OilPatternRevealDateTime : null,
                OilPatterns = revealed
                    ? row.OilPatterns.ConvertAll(pattern => new SeasonTournamentOilPatternDto
                    {
                        Name = pattern.Name,
                        Length = pattern.Length,
                        Volume = pattern.Volume,
                        LeftRatio = pattern.LeftRatio,
                        RightRatio = pattern.RightRatio,
                        KegelId = pattern.KegelId,
                        TournamentRounds = [.. pattern.TournamentRounds.Select(r => r.Name)]
                    })
                    : [],
                // ...LogoUrl / Winners unchanged...
            };
        })];
    }
}
```

**`ListTournamentsInSeason/SeasonTournamentDto.cs`** — add `public DateTimeOffset? OilPatternRevealDateTime { get; init; }`.

**`ListTournamentsInSeason/SeasonTournamentOilPatternDto.cs`** — add:

```csharp
public required decimal Volume { get; init; }
public required decimal LeftRatio { get; init; }
public required decimal RightRatio { get; init; }
public Guid? KegelId { get; init; }
```

**`ListTournamentsInSeason/ListTournamentsInSeasonEndpoint.cs`** — build query with the auth flag and map the new fields:

```csharp
var query = new ListTournamentsInSeasonQuery
{
    SeasonId = new SeasonId(req.SeasonId),
    CallerIsAuthenticated = User.Identity?.IsAuthenticated == true
};
var result = await _queryHandler.HandleAsync(query, ct);

var response = new CollectionResponse<SeasonTournamentResponse>
{
    Items = [.. result.Select(t => new SeasonTournamentResponse
    {
        // ...existing mappings unchanged...
        PatternLengthCategory = t.PatternLengthCategory,
        PatternRatioCategory = t.PatternRatioCategory,
        OilPatternRevealDateTime = t.OilPatternRevealDateTime,
        // ...BowlingCenter / Sponsors unchanged...
        OilPatterns = [.. t.OilPatterns.Select(op => new TournamentOilPatternResponse
        {
            Name = op.Name,
            Length = op.Length,
            Volume = op.Volume,
            LeftRatio = op.LeftRatio,
            RightRatio = op.RightRatio,
            KegelId = op.KegelId,
            Rounds = op.TournamentRounds,
        })],
    })],
};
```

### Contracts (`src/Neba.Api.Contracts/`)

**`Tournaments/CreateTournament/TournamentInput.cs`** — add:

```csharp
/// <summary>
/// Date/time at which full oil pattern details become visible to unauthenticated visitors;
/// null if there's no reveal restriction. Authenticated users always see full details.
/// </summary>
public DateTimeOffset? OilPatternRevealDateTime { get; init; }
```

**`Tournaments/GetTournament/TournamentDetailResponse.cs`** — add `public DateTimeOffset? OilPatternRevealDateTime { get; init; }`.

**`Tournaments/GetTournament/TournamentDetailOilPatternResponse.cs`** — add `Volume`/`LeftRatio`/`RightRatio` (`required decimal`, matching the DTO).

**`Seasons/ListTournamentsInSeason/SeasonTournamentResponse.cs`** — add `public DateTimeOffset? OilPatternRevealDateTime { get; init; }`.

**`Seasons/ListTournamentsInSeason/TournamentOilPatternResponse.cs`** — add `Volume`/`LeftRatio`/`RightRatio` (`required decimal`) and `Guid? KegelId`.

**`Tournaments/CreateTournament/CreateTournamentEndpoint.cs`** (API project, Contracts-facing) — add one line to the command mapping:

```csharp
var command = new CreateTournamentCommand
{
    // ...existing mappings unchanged...
    OilPatternRevealDateTime = input.OilPatternRevealDateTime
};
```

### Security (`src/Neba.Api.Contracts/Security/`)

**`Permission.cs`** — inside `#region Tournaments`:

```csharp
/// <summary>
/// Permission to add or remove sponsors on a tournament.
/// </summary>
public static readonly Permissions ManageTournamentSponsors = new("Tournaments.ManageSponsors", "Manage Tournament Sponsors");

/// <summary>
/// A collection of permissions related to tournament management.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> TournamentManagementPermissions =
[
    CreateTournament,
];

/// <summary>
/// Policy name satisfied when the caller holds any permission in <see cref="TournamentManagementPermissions"/>.
/// </summary>
public const string CanManageTournamentsPolicyName = "CanManageTournaments";
```

**`PolicyExtensions.cs`**

```csharp
public AuthorizationBuilder AddNebaPolicies()
{
    builder.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy
        .RequireAssertion(context => context.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));

    builder.AddPolicy(Permissions.CanManageSponsorsPolicyName, policy => policy
        .RequireAssertion(context => context.User.HasAnyPermission(Permissions.SponsorManagementPermissions)));

    builder.AddPolicy(Permissions.CanManageTournamentsPolicyName, policy => policy
        .RequireAssertion(context => context.User.HasAnyPermission(Permissions.TournamentManagementPermissions)));

    return builder;
}
```

### Test Factories (`tests/Neba.TestFactory/Tournaments/TournamentFactory.cs`)

`Create(...)` gains one more optional parameter, set directly on the object initializer (this factory bypasses `Tournament.Create(...)` and constructs the object literal directly, matching its existing style):

```csharp
public static Tournament Create(
    TournamentId? id = null,
    string? name = null,
    TournamentType? tournamentType = null,
    DateOnly? startDate = null,
    DateOnly? endDate = null,
    bool? statsEligible = null,
    CertificationNumber? bowlingCenterId = null,
    PatternRatioCategory? patternRatioCategory = null,
    PatternLengthCategory? patternLengthCategory = null,
    int? legacyId = null,
    SeasonId? seasonId = null,
    decimal? entryFee = null,
    Uri? externalRegistrationUrl = null,
    StoredFile? logo = null,
    IReadOnlyCollection<TournamentSponsor>? sponsors = null,
    DateTimeOffset? oilPatternRevealDateTime = null)
{
    var tournament = new Tournament
    {
        // ...existing initializers unchanged...
        OilPatternRevealDateTime = oilPatternRevealDateTime
    };

    // ...sponsor loop unchanged...

    return tournament;
}
```

`Bogus(...)` gets `OilPatternRevealDateTime = faker.Random.Bool() ? faker.Date.FutureOffset(1) : null` added to its object initializer.

### Tests

**`OilPatternRevealPolicyTests.cs`** (new, `tests/Neba.Api.Tests/Features/Tournaments/Domain/`)

```csharp
[Fact(DisplayName = "IsRevealed returns true when there is no reveal date")]
[UnitTest, Component("Tournaments")]
public void IsRevealed_ShouldReturnTrue_WhenRevealDateTimeIsNull()
{
    // Arrange
    var now = DateTimeOffset.UtcNow;

    // Act
    var result = OilPatternRevealPolicy.IsRevealed(null, callerIsAuthenticated: false, now);

    // Assert
    result.ShouldBeTrue();
}

[Fact(DisplayName = "IsRevealed returns true when the caller is authenticated, regardless of the reveal date")]
[UnitTest, Component("Tournaments")]
public void IsRevealed_ShouldReturnTrue_WhenCallerIsAuthenticated()
{
    // Arrange
    var now = DateTimeOffset.UtcNow;
    var futureRevealDate = now.AddDays(7);

    // Act
    var result = OilPatternRevealPolicy.IsRevealed(futureRevealDate, callerIsAuthenticated: true, now);

    // Assert
    result.ShouldBeTrue();
}

[Fact(DisplayName = "IsRevealed returns false when the reveal date is in the future and the caller is anonymous")]
[UnitTest, Component("Tournaments")]
public void IsRevealed_ShouldReturnFalse_WhenRevealDateTimeIsInFutureAndCallerIsAnonymous()
{
    // Arrange
    var now = DateTimeOffset.UtcNow;
    var futureRevealDate = now.AddDays(7);

    // Act
    var result = OilPatternRevealPolicy.IsRevealed(futureRevealDate, callerIsAuthenticated: false, now);

    // Assert
    result.ShouldBeFalse();
}

[Fact(DisplayName = "IsRevealed returns true when the reveal date has already passed and the caller is anonymous")]
[UnitTest, Component("Tournaments")]
public void IsRevealed_ShouldReturnTrue_WhenRevealDateTimeHasPassedAndCallerIsAnonymous()
{
    // Arrange
    var now = DateTimeOffset.UtcNow;
    var pastRevealDate = now.AddDays(-7);

    // Act
    var result = OilPatternRevealPolicy.IsRevealed(pastRevealDate, callerIsAuthenticated: false, now);

    // Assert
    result.ShouldBeTrue();
}
```

**`CreateTournamentCommandHandlerTests.cs`** (extend existing) — add cases using `Mock<IBackgroundJobScheduler>(MockBehavior.Strict)` and a fixed `TimeProvider` (e.g. `new FakeTimeProvider(fixedNow)` or the project's existing `TimeProvider` test double, whichever this test class already uses):

```csharp
[Fact(DisplayName = "HandleAsync schedules an oil pattern reveal cache eviction job when a future reveal date is provided")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldScheduleEvictionJob_WhenOilPatternRevealDateTimeIsInFuture()
{
    // Arrange
    var revealAt = _timeProvider.GetUtcNow().AddDays(3);
    var command = BuildValidCommand() with { OilPatternRevealDateTime = revealAt };

    _jobSchedulerMock
        .Setup(scheduler => scheduler.Schedule(
            It.Is<EvictOilPatternRevealCacheJob>(job => job.TournamentId == It.IsAny<TournamentId>()),
            revealAt))
        .Returns("job-id")
        .Verifiable();

    // Act
    var result = await _handler.HandleAsync(command, CancellationToken.None);

    // Assert
    result.IsError.ShouldBeFalse();
    _jobSchedulerMock.VerifyAll();
}

[Fact(DisplayName = "HandleAsync does not schedule an eviction job when no reveal date is provided")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldNotScheduleEvictionJob_WhenOilPatternRevealDateTimeIsNull()
{
    // Arrange
    var command = BuildValidCommand() with { OilPatternRevealDateTime = null };

    // Act
    var result = await _handler.HandleAsync(command, CancellationToken.None);

    // Assert
    result.IsError.ShouldBeFalse();
    _jobSchedulerMock.Verify(
        scheduler => scheduler.Schedule(It.IsAny<EvictOilPatternRevealCacheJob>(), It.IsAny<DateTimeOffset>()),
        Times.Never);
}

[Fact(DisplayName = "HandleAsync does not schedule an eviction job when the reveal date is already in the past")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldNotScheduleEvictionJob_WhenOilPatternRevealDateTimeIsInPast()
{
    // Arrange
    var command = BuildValidCommand() with { OilPatternRevealDateTime = _timeProvider.GetUtcNow().AddDays(-1) };

    // Act
    var result = await _handler.HandleAsync(command, CancellationToken.None);

    // Assert
    result.IsError.ShouldBeFalse();
    _jobSchedulerMock.Verify(
        scheduler => scheduler.Schedule(It.IsAny<EvictOilPatternRevealCacheJob>(), It.IsAny<DateTimeOffset>()),
        Times.Never);
}
```

**`GetTournamentQueryHandlerTests.cs`** (extend existing) — 4 new cases following the existing arrange/seed style:

```csharp
[Fact(DisplayName = "HandleAsync returns reduced oil pattern info when caller is anonymous and reveal date is in the future")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldReturnReducedOilPatternInfo_WhenCallerIsAnonymousAndRevealDateIsInFuture()
{
    // Arrange — seed a tournament with OilPatternRevealDateTime = _timeProvider.GetUtcNow().AddDays(5)
    //           and an attached OilPattern with a known Name/Volume/LeftRatio/RightRatio.
    // Act — HandleAsync(query with CallerIsAuthenticated = false)
    // Assert — result.Value.OilPatterns is empty; PatternLengthCategory/PatternRatioCategory still populated;
    //          result.Value.OilPatternRevealDateTime is null.
}

[Fact(DisplayName = "HandleAsync returns full oil pattern info when caller is anonymous and reveal date has passed")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerIsAnonymousAndRevealDateHasPassed()
{
    // Arrange — OilPatternRevealDateTime = _timeProvider.GetUtcNow().AddDays(-1).
    // Assert — result.Value.OilPatterns contains the pattern with Volume/LeftRatio/RightRatio populated.
}

[Fact(DisplayName = "HandleAsync returns full oil pattern info when caller is anonymous and no reveal date is set")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerIsAnonymousAndNoRevealDateIsSet()
{
    // Arrange — OilPatternRevealDateTime = null.
    // Assert — full OilPatterns returned.
}

[Fact(DisplayName = "HandleAsync returns full oil pattern info and the reveal date when caller is authenticated, regardless of reveal date")]
[UnitTest, Component("Tournaments")]
public async Task HandleAsync_ShouldReturnFullOilPatternInfoAndRevealDate_WhenCallerIsAuthenticated()
{
    // Arrange — OilPatternRevealDateTime = future date.
    // Act — HandleAsync(query with CallerIsAuthenticated = true)
    // Assert — full OilPatterns returned; result.Value.OilPatternRevealDateTime equals the seeded value.
}
```

**`ListTournamentsInSeasonQueryHandlerTests.cs`** (extend existing) — same 4-case matrix, seeded across a list with a mix of revealed/unrevealed tournaments to confirm the gate runs per-row rather than once for the whole result set.

**`EvictOilPatternRevealCacheJobHandlerTests.cs`** (new, `tests/Neba.Api.Tests/Features/Tournaments/EvictOilPatternRevealCache/`)

```csharp
[Fact(DisplayName = "ExecuteAsync evicts the tournament detail and season list cache tags")]
[UnitTest, Component("Tournaments")]
public async Task ExecuteAsync_ShouldEvictTournamentAndSeasonListCacheTags()
{
    // Arrange
    var job = new EvictOilPatternRevealCacheJob { TournamentId = TournamentId.New(), SeasonId = SeasonId.New() };
    var cache = new FusionCache(new FusionCacheOptions()); // or the project's existing IFusionCache test double
    var handler = new EvictOilPatternRevealCacheJobHandler(cache);

    await cache.SetAsync($"probe:{job.TournamentId}", "value", tags: [$"neba:tournaments:{job.TournamentId}"]);
    await cache.SetAsync($"probe:{job.SeasonId}", "value", tags: [$"neba:tournaments:{job.SeasonId}"]);

    // Act
    await handler.ExecuteAsync(job, CancellationToken.None);

    // Assert
    (await cache.TryGetAsync<string>($"probe:{job.TournamentId}")).HasValue.ShouldBeFalse();
    (await cache.TryGetAsync<string>($"probe:{job.SeasonId}")).HasValue.ShouldBeFalse();
}
```

**Integration** (`tests/Neba.Api.Tests/Features/Tournaments/CreateTournament/` or wherever the existing create→get round-trip integration test lives, per `CreateTournamentTests.cs` referenced in recent commits) — extend to assert: creating a tournament with a future `OilPatternRevealDateTime` and a picked oil pattern, then calling `GetTournament` unauthenticated returns empty `OilPatterns` with categories populated, and calling it authenticated returns the full pattern including `Volume`/`LeftRatio`/`RightRatio`.

## Phase 2: UI

A key finding from reviewing the existing UI: **most of the list/card views need zero code changes.** `SeasonTournamentViewModel.PatternDisplay` already falls back from `"{PatternName} · {PatternLength} ft"` to `PatternLengthCategory` when `PatternName`/`PatternLength` are null (`src/Neba.Website.Server/Tournaments/Schedule/SeasonTournamentViewModel.cs:221-229`), and `TournamentApiService.MapToViewModel` already derives those from `response.OilPatterns.FirstOrDefault()` (`TournamentApiService.cs:70`) — so once the API returns an empty `OilPatterns` collection pre-reveal (Phase 1), `TournamentHero.razor`/`TournamentUpcomingCard.razor` degrade to the category chip automatically. Confirmed no edits needed to either component.

### Pages (`src/Neba.Website.Server/Tournaments/`)

- **`CreateTournament.razor`** (edit) — add a reveal date/time field inside the existing "Oil Pattern" `<section>`, above `<OilPatternPicker>`, so it applies to all 3 picker modes. Uses a plain native `<input type="datetime-local">` (not wired through `EditContext`/`InputBase` — same "plain element + manual dirty tracking" pattern `OilPatternPicker` already uses for its own inputs), since no per-keystroke JS-driven behavior is needed here (a native date/time picker commits on blur/change, not per keystroke, so the SignalR-round-trip race documented in this repo's `NebaDateInput` postmortem doesn't apply). Converts to/from UTC via the new shared `IClientTimeZoneService` (see below), not a page-local JS module.
- **`Detail/TournamentDetail.razor`** (edit) — extend the existing "Oil Pattern(s)" section to show a reveal-date note and the new ratio/volume detail when present; no structural change to the section's visibility logic (`_model.HasOilPatterns` already governs whether the section renders at all, and already becomes `false` automatically for a pre-reveal anonymous visitor once the API returns an empty collection).
- **`TournamentHero.razor` / `TournamentUpcomingCard.razor`** — **no changes**, per the finding above.

### Shared Infrastructure (`src/Neba.Website.Server/Time/`, new folder) — repo-wide, not scoped to Tournaments

- **`IClientTimeZoneService.cs` / `ClientTimeZoneService.cs`** (new) — scoped service (one instance per circuit) wrapping a browser JS call to resolve the viewer's IANA time zone once, cached for the rest of that circuit; exposes `ToLocalAsync(DateTimeOffset)` and `ToUtcAsync(DateTime)`.
- **`wwwroot/js/browser-time.js`** (edit) — replace `getTimezoneOffsetMinutes()` with `getTimeZoneId()`.
- **`Program.cs`** (edit) — `builder.Services.AddScoped<IClientTimeZoneService, ClientTimeZoneService>();`, alongside the existing `ToastService`/`ITournamentApiService` scoped registrations.
- **`News/NewsList.razor`, `News/NewsDetail.razor`, `News/CreateArticle.razor`, `News/EditArticle.razor`** (edit) — remove each file's own `ToLocal`/`ToLocalDateTimeOffsetAsync` helper, `_jsModule`/`_localOffset` fields, and `OnAfterRenderAsync` JS-offset lookup; inject `IClientTimeZoneService` and call it instead. `ArticleCard.razor` needs no change (it already just renders whatever `LocalPublishDate` its parent passes in).
- **`Documents/NebaDocument.razor`, `Documents/TournamentRules.razor`, `Pages/Bylaws.razor`, `Documents/DocumentSlideoverHandler.cs`** (edit) — apply `IClientTimeZoneService.ToLocalAsync(...)` to `LastUpdated` before formatting, closing the previously-unconverted-raw-UTC gap.

### View Models / Mapping (`src/Neba.Website.Server/Tournaments/Detail/`)

- **`TournamentDetailViewModel.cs`** (edit) — add `public DateTimeOffset? OilPatternRevealDateTime { get; init; }` and a computed `HasOilPatternRevealDateTime` convenience flag (mirrors the existing `Has*` pattern on this record).
- **`TournamentDetailOilPatternViewModel.cs`** (edit) — add `Volume`, `LeftRatio`, `RightRatio` (`required decimal`, mirrors `Length` already being `required`) and a computed `RatioDisplay` string for the card's secondary line.
- **`TournamentDetailMappingExtensions.cs`** (edit) — map the new fields through in both `ToViewModel()` extensions.

### Time zone handling — shared, repo-wide

The API stores `OilPatternRevealDateTime` as `DateTimeOffset` (UTC on the wire); the Create Tournament form only has a plain HTML `datetime-local` input, which captures a **local wall-clock value with no time zone attached**. Per your decision, this must be the *viewer's own* local time everywhere in the app, not a per-field ad hoc conversion — so this plan introduces one shared `IClientTimeZoneService` (new, `src/Neba.Website.Server/Time/`) instead of a page-local JS module, and migrates the existing duplicated/missing conversions onto it:

- **`CreateTournament.razor`**'s new reveal date/time field (this feature) — uses the shared service for input.
- **`NewsList.razor` / `NewsDetail.razor`** — currently each have their own `ToLocal(DateTimeOffset)` helper + `OnAfterRenderAsync` JS-offset lookup for displaying `Article.PublishDateUtc`; migrated to call the shared service instead.
- **`CreateArticle.razor` / `EditArticle.razor`** — currently each have their own `ToLocalDateTimeOffsetAsync(DateTime)` helper for converting the `PublishDateLocal` input back to UTC on submit; migrated to call the shared service instead. `EditArticle.razor` also currently has a brief "flash of raw UTC" on load before its own JS offset resolves (line 306) — the shared service's caching (see below) removes that flash for any component that isn't the first one on the page to trigger the JS call.
- **`Documents/NebaDocument.razor`, `TournamentRules.razor`, `Pages/Bylaws.razor`, `DocumentSlideoverHandler.cs`** — currently display `LastUpdated` (`DateTimeOffset?`) with **zero** conversion (raw UTC formatted directly). This was a genuine gap, not a duplicated-but-working pattern like News — fixed by routing through the shared service too.
- **`wwwroot/js/browser-time.js`**'s `getTimezoneOffsetMinutes()` export is replaced with `getTimeZoneId()` (IANA ID, not a raw offset) — see the DST-correctness note above. Nothing else imports this file directly once the migration is done, so the old export is deleted rather than kept alongside the new one.

Everything below (Phase 2 code) reflects the shared-service version, not a page-local one.

### API Client

No new Refit methods needed — `ITournamentsApi.CreateTournamentAsync`/`GetTournamentAsync` and `ISeasonsApi.ListTournamentsInSeasonAsync` already exist; only their request/response shapes changed (Phase 1, `Neba.Api.Contracts`, shared by both projects — no separate contract work here).

### State / Dirty-Tracking

- The new reveal-date input isn't wired through `_editContext` (same reasoning as `OilPatternPicker`'s own inputs) — its `@bind:after` calls `MarkDirty()` directly, matching the existing `HandleOilPatternSelectionChanged` callback style already on this page.

### `<PageTitle>` / Render Mode

No change — `CreateTournament.razor` and `TournamentDetail.razor` already have correct `<PageTitle>`/render-mode setup from prior work; this feature doesn't add a new page.

### Tests

- **bUnit** (`tests/Neba.Website.Tests/Tournaments/CreateTournamentTests.cs`, extend) — new case: setting the reveal date/time input results in `TournamentInput.OilPatternRevealDateTime` being sent as the expected UTC-converted `DateTimeOffset` on submit; leaving it blank sends `null`.
- **bUnit** (`tests/Neba.Website.Tests/Tournaments/Detail/TournamentDetailTests.cs`, extend) — new cases: a response with `OilPatternRevealDateTime` set and full `OilPatterns` renders the reveal note and ratio/volume detail; a response with `OilPatternRevealDateTime` null and empty `OilPatterns` (the anonymous pre-reveal shape) hides the entire Oil Pattern(s) section, same as today's "no pattern set" case — confirming the reduced view needs no new component-level branching, only new-field rendering when the fields are present.
- **Playwright**: not warranted as a new end-to-end case — the existing `/tournaments/new` create flow and `/tournaments/{id}` detail-view Playwright coverage (if any) already exercises the page; this feature only adds one field/one display block within flows already covered. Skip unless the existing suite doesn't already have a create→view round trip.

### Mockups

- [`docs/plans/mockups/oil-pattern-reveal-date/oil-pattern-reveal-date.html`](mockups/oil-pattern-reveal-date/oil-pattern-reveal-date.html) — single mockup covering both changed surfaces (data-capture form field has one obvious layout, so one mockup per the mockup-scoping rule; the two detail-page states are shown via an inline-JS toggle in the same file rather than separate option files, since it's the same layout with different data, not a genuine layout decision to compare): (1) the new reveal date/time field in the Create Tournament Oil Pattern section; (2) the tournament detail page's oil pattern card in 3 states — authenticated/pre-reveal (full detail + pending-reveal note), anonymous/pre-reveal (category chip only, nothing else), and post-reveal (full detail + revealed note, same for any caller). Updated to drop the "sign in" nudge originally sketched in an earlier draft, per your call to show nothing beyond the chip(s) in the reduced view.

## Phase 2 code

### View Models / Mapping (`src/Neba.Website.Server/Tournaments/Detail/`)

**`TournamentDetailViewModel.cs`** — add near the other oil-pattern fields:

```csharp
/// <summary>
/// Date/time at which full oil pattern details become public; null when there's no
/// restriction, or when the current viewer isn't authenticated (in which case they simply
/// don't know a reveal date exists — only whether details are currently visible).
/// </summary>
public DateTimeOffset? OilPatternRevealDateTime { get; init; }

/// <summary>
/// True when a reveal date/time is known (always false for an anonymous viewer, even if one is set).
/// </summary>
public bool HasOilPatternRevealDateTime => OilPatternRevealDateTime is not null;

/// <summary>
/// True when the reveal date/time is known and still in the future.
/// </summary>
public bool OilPatternRevealIsPending => OilPatternRevealDateTime is { } revealAt && revealAt > DateTimeOffset.UtcNow;

/// <summary>
/// True when there's a category chip to show (length or ratio category known) but no full
/// pattern detail — the shape an anonymous, pre-reveal caller's response takes.
/// </summary>
public bool HasReducedOilPatternInfoOnly =>
    !HasOilPatterns && (PatternLengthCategory is not null || PatternRatioCategory is not null);
```

Also add the previously-missing `PatternRatioCategory` alongside `PatternLengthCategory` (both were already on the API response; only `PatternLengthCategory` had been mapped into this view model) — needed for the `HasReducedOilPatternInfoOnly` check above to be accurate:

```csharp
/// <summary>
/// Pattern ratio category label; null until set.
/// </summary>
public string? PatternRatioCategory { get; init; }
```

**`TournamentDetailOilPatternViewModel.cs`** — add:

```csharp
/// <summary>
/// The oil volume applied, in milliliters.
/// </summary>
public required decimal Volume { get; init; }

/// <summary>
/// The forward (head) to reverse (tail) oil ratio on the pattern's left side.
/// </summary>
public required decimal LeftRatio { get; init; }

/// <summary>
/// The forward (head) to reverse (tail) oil ratio on the pattern's right side.
/// </summary>
public required decimal RightRatio { get; init; }

/// <summary>
/// Volume and ratio formatted for the card's secondary line.
/// </summary>
public string RatioDisplay =>
    Volume.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " mL · ratio "
    + LeftRatio.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + "/"
    + RightRatio.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture);
```

**`TournamentDetailMappingExtensions.cs`** — extend both mappings:

```csharp
public TournamentDetailViewModel ToViewModel() => new()
{
    // ...existing mappings unchanged...
    PatternLengthCategory = response.PatternLengthCategory,
    PatternRatioCategory = response.PatternRatioCategory,
    OilPatternRevealDateTime = response.OilPatternRevealDateTime,
    // ...remaining mappings unchanged...
};
```

```csharp
public TournamentDetailOilPatternViewModel ToViewModel() => new()
{
    Name = response.Name,
    Length = response.Length,
    Volume = response.Volume,
    LeftRatio = response.LeftRatio,
    RightRatio = response.RightRatio,
    Rounds = response.Rounds,
    KegelId = response.KegelId,
};
```

### `Detail/TournamentDetail.razor`

Replace the existing "Oil Pattern(s)" block (currently gated only on `_model.HasOilPatterns`) with one that also handles the reduced-info and reveal-note cases:

```razor
@if (_model.HasOilPatterns)
{
    <section class="tournament-detail__section">
        <h2 class="td-section-title">@(_model.OilPatterns.Count == 1 ? "Oil Pattern" : "Oil Patterns")</h2>

        @if (_model.HasOilPatternRevealDateTime)
        {
            <p class="td-reveal-note @(_model.OilPatternRevealIsPending ? "td-reveal-note--pending" : "td-reveal-note--past")">
                <span class="material-symbols-outlined" aria-hidden="true">@(_model.OilPatternRevealIsPending ? "visibility_off" : "visibility")</span>
                @(_model.OilPatternRevealIsPending
                    ? "Full details reveal to the public on " + (_oilPatternRevealLocal ?? _model.OilPatternRevealDateTime!.Value).ToString("MMM d, yyyy, h:mm tt", CurrencyCulture)
                    : "Revealed to the public")
            </p>
        }

        <div class="td-pattern-list">
            @foreach (var pattern in _model.OilPatterns)
            {
                <div class="neba-card td-pattern-card">
                    <span class="td-pattern-card__dot @GetPatternClass(pattern.Length)" aria-hidden="true"></span>
                    <div class="td-pattern-card__body">
                        <p class="td-pattern-card__name">
                            @if (pattern.KegelLibraryUrl is not null)
                            {
                                <a href="@pattern.KegelLibraryUrl" target="_blank" rel="noopener noreferrer"
                                   class="neba-link">@pattern.Display</a>
                            }
                            else
                            {
                                @pattern.Display
                            }
                        </p>
                        <p class="td-pattern-card__sub">@pattern.RatioDisplay</p>
                        @if (_model.OilPatterns.Count > 1 && pattern.Rounds.Count > 0)
                        {
                            <p class="td-pattern-card__rounds">@string.Join(", ", pattern.Rounds)</p>
                        }
                    </div>
                </div>
            }
        </div>
    </section>
}
else if (_model.HasReducedOilPatternInfoOnly)
{
    <section class="tournament-detail__section">
        <h2 class="td-section-title">Oil Pattern</h2>
        <div class="td-hero__chips">
            @if (_model.PatternLengthCategory is not null)
            {
                <span class="td-hero__chip">@_model.PatternLengthCategory</span>
            }
            @if (_model.PatternRatioCategory is not null)
            {
                <span class="td-hero__chip">@_model.PatternRatioCategory</span>
            }
        </div>
    </section>
}
```

Nothing else renders in the reduced case — no call-to-action, no mention that more detail exists, just the category chip(s).

Code-behind — inject the shared time zone service and convert `OilPatternRevealDateTime` for display once it's known (the pending/past determination itself, `_model.OilPatternRevealIsPending`, is already correct without conversion — `DateTimeOffset` comparisons are timezone-invariant; only the *formatted string* needs the viewer's local time):

```csharp
@inject IClientTimeZoneService ClientTimeZoneService
```

```csharp
private DateTimeOffset? _oilPatternRevealLocal;

protected override async Task OnInitializedAsync()
{
    var result = await ApiExecutor.ExecuteAsync(
        "TournamentsApi",
        "GetTournamentDetail",
        ct => TournamentsApi.GetTournamentAsync(Id, ct));

    if (result.IsError)
    {
        // ...unchanged...
        return;
    }

    _model = result.Value.ToViewModel();

    if (_model.OilPatternRevealDateTime is { } revealUtc)
    {
        _oilPatternRevealLocal = await ClientTimeZoneService.ToLocalAsync(revealUtc);
    }

    _isLoading = false;
}
```

`ReloadTournamentAsync` (called after sponsor changes) gets the same two lines added after re-mapping `_model`, so the reveal note stays correctly localized after a reload.

New CSS classes (`wwwroot/neba_theme.css` or the tournament-detail-specific stylesheet, wherever `.td-pattern-card` etc. are already defined):

```css
.td-reveal-note {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.8rem;
    border-radius: 999px;
    padding: 0.3rem 0.75rem;
    margin-bottom: 0.75rem;
}
.td-reveal-note .material-symbols-outlined { font-size: 1rem; }
.td-reveal-note--pending { color: var(--neba-warning); background: color-mix(in srgb, var(--neba-warning) 12%, transparent); }
.td-reveal-note--past { color: var(--neba-success); background: color-mix(in srgb, var(--neba-success) 12%, transparent); }
.td-pattern-card__sub { margin: 0.15rem 0 0; font-size: 0.85rem; color: var(--neba-gray-600); }
```

### Shared Infrastructure (`src/Neba.Website.Server/Time/`)

**`wwwroot/js/browser-time.js`** — replace the offset-minutes export with an IANA time zone ID export:

```javascript
// Shared browser timezone helper for converting UTC values to/from the viewer's local time.
export function getTimeZoneId() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
```

**`Time/IClientTimeZoneService.cs`**

```csharp
namespace Neba.Website.Server.Time;

/// <summary>
/// Resolves the current viewer's browser-local time zone and converts between it and UTC.
/// Scoped per circuit — the underlying JS lookup happens at most once per session.
/// </summary>
public interface IClientTimeZoneService
{
    /// <summary>
    /// Converts a UTC instant to the viewer's local time zone.
    /// </summary>
    Task<DateTimeOffset> ToLocalAsync(DateTimeOffset utc);

    /// <summary>
    /// Converts a local wall-clock value (as captured from a plain <c>datetime-local</c> input,
    /// with no time zone of its own) to UTC, using the viewer's browser-local time zone.
    /// </summary>
    Task<DateTimeOffset> ToUtcAsync(DateTime local);
}
```

**`Time/ClientTimeZoneService.cs`**

```csharp
using Microsoft.JSInterop;

namespace Neba.Website.Server.Time;

internal sealed class ClientTimeZoneService(IJSRuntime jsRuntime, ILogger<ClientTimeZoneService> logger)
    : IClientTimeZoneService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private TimeZoneInfo? _timeZone;

    public async Task<DateTimeOffset> ToLocalAsync(DateTimeOffset utc)
    {
        var timeZone = await GetTimeZoneAsync();
        return TimeZoneInfo.ConvertTime(utc, timeZone);
    }

    public async Task<DateTimeOffset> ToUtcAsync(DateTime local)
    {
        var timeZone = await GetTimeZoneAsync();
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
        return new DateTimeOffset(utc);
    }

    private async Task<TimeZoneInfo> GetTimeZoneAsync()
    {
        if (_timeZone is not null)
        {
            return _timeZone;
        }

        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/browser-time.js");
            var timeZoneId = await _module.InvokeAsync<string>("getTimeZoneId");
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogClientTimeZoneResolutionFailed(ex);
            _timeZone = TimeZoneInfo.Utc;
        }

        return _timeZone;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone — nothing to clean up.
            }
        }
    }
}

internal static partial class ClientTimeZoneServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to resolve the client's browser time zone; falling back to UTC.")]
    public static partial void LogClientTimeZoneResolutionFailed(this ILogger<ClientTimeZoneService> logger, Exception exception);
}
```

**`Program.cs`** — add alongside the other scoped service registrations:

```csharp
builder.Services.AddScoped<IClientTimeZoneService, ClientTimeZoneService>();
```

### `CreateTournament.razor`

Markup — insert above `<OilPatternPicker>`, inside the existing Oil Pattern `<section>`:

```razor
<section class="neba-space-y-4">
    <h2 class="create-tournament-section-title">Oil Pattern</h2>
    <p class="text-sm text-[var(--neba-gray-500)]">Optional. Pick a pattern to auto-fill lane condition, create a new one, or set the condition manually.</p>

    <div>
        <label for="oil-pattern-reveal" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Reveal Date/Time (optional)</label>
        <input id="oil-pattern-reveal" type="datetime-local" class="neba-input" style="max-width: 20rem"
               @bind="_oilPatternRevealLocal" @bind:after="MarkDirty" />
        <p class="text-sm text-[var(--neba-gray-500)]">
            Full pattern details stay hidden from the public until this date/time, in your local time zone. Leave blank to make the pattern public immediately. Registered users always see full details.
        </p>
    </div>

    <OilPatternPicker SelectionChanged="HandleOilPatternSelectionChanged" />
</section>
```

Code-behind — inject the shared service and convert on submit (no page-local JS module, no `OnAfterRenderAsync` needed here — the service handles its own lazy JS lookup and per-circuit caching):

```csharp
@inject IClientTimeZoneService ClientTimeZoneService
```

```csharp
private DateTime? _oilPatternRevealLocal;
```

`HandleCreateAsync` builds the input asynchronously now (it already awaits the API call, so this doesn't change the method's shape):

```csharp
private async Task HandleCreateAsync()
{
    _isSubmitting = true;
    _errorMessage = null;

    var request = new CreateTournamentRequest { Tournament = await BuildTournamentInputAsync() };

    var result = await ApiExecutor.ExecuteAsync(
        "Tournaments",
        "CreateTournament",
        ct => TournamentsApi.CreateTournamentAsync(request, ct));

    // ...unchanged from here down...
}

private async Task<TournamentInput> BuildTournamentInputAsync() => new()
{
    Name = _model.Name,
    TournamentType = _model.TournamentType,
    StartDate = _model.StartDate!.Value,
    EndDate = _model.EndDate!.Value,
    StatsEligible = _model.StatsEligible,
    EntryFee = _model.EntryFee,
    BowlingCenterCertificationNumber = string.IsNullOrWhiteSpace(_model.BowlingCenterCertificationNumber)
        ? null
        : _model.BowlingCenterCertificationNumber,
    ExternalRegistrationUrl = ParseUri(_model.ExternalRegistrationUrl),
    Logo = BuildLogoInput(),
    OilPatternId = _oilPatternSelection.OilPatternId,
    PatternLengthCategory = _oilPatternSelection.PatternLengthCategory,
    PatternRatioCategory = _oilPatternSelection.PatternRatioCategory,
    OilPatternRevealDateTime = _oilPatternRevealLocal is { } local
        ? await ClientTimeZoneService.ToUtcAsync(local)
        : null
};
```

(renamed from the existing synchronous `BuildTournamentInput()` to `BuildTournamentInputAsync()` — its one call site in `HandleCreateAsync` is updated to match; no other behavior changes.)

### Tests

**`TournamentDetailTests.cs`** (extend) — the constructor's shared setup gains a `Mock<IClientTimeZoneService>(MockBehavior.Strict)` registered into `_ctx.Services` (same pattern as `CreateTournamentTests`); tests that seed an `OilPatternRevealDateTime` need `mockClientTimeZoneService.Setup(s => s.ToLocalAsync(theUtcValue)).ReturnsAsync(someLocalValue)`, tests that don't set one need no setup at all (Strict mock, `ToLocalAsync` is never called when `OilPatternRevealDateTime` is null). Representative new cases:

```csharp
[Fact(DisplayName = "Renders the pending reveal note and full pattern detail for an authenticated caller before the reveal date")]
[UnitTest, Component("Website.Tournaments.Detail")]
public void Render_ShouldShowPendingRevealNoteAndFullPatternDetail_WhenAuthenticatedBeforeReveal()
{
    // Arrange — response with OilPatternRevealDateTime = future, OilPatterns containing one full pattern
    //           (Name/Volume/LeftRatio/RightRatio populated); authorization context authenticated.
    // Act — render TournamentDetail.
    // Assert — markup contains "Full details reveal to the public on" and the pattern's RatioDisplay text.
}

[Fact(DisplayName = "Renders only the category chip and nothing else for an anonymous caller before the reveal date")]
[UnitTest, Component("Website.Tournaments.Detail")]
public void Render_ShouldShowCategoryChipOnly_WhenAnonymousBeforeReveal()
{
    // Arrange — response with OilPatternRevealDateTime = null (as the anonymous-shaped API response would send),
    //           OilPatterns empty, PatternLengthCategory/PatternRatioCategory populated; authorization context anonymous.
    // Act — render TournamentDetail.
    // Assert — markup contains the category chip text; does not render a "td-pattern-card"; does not render
    //          a reveal note; renders no other call-to-action or explanatory text in that section.
}

[Fact(DisplayName = "Renders the revealed note and full pattern detail once the reveal date has passed, regardless of authentication")]
[UnitTest, Component("Website.Tournaments.Detail")]
public void Render_ShouldShowRevealedNoteAndFullPatternDetail_WhenRevealDateHasPassed()
{
    // Arrange — response shaped as the API would return post-reveal to an anonymous caller: OilPatterns
    //           fully populated (API only empties OilPatterns pre-reveal-and-anonymous); OilPatternRevealDateTime
    //           still null here since only authenticated callers receive the value even post-reveal per the
    //           API's CallerIsAuthenticated gate — this test's real point is that "Revealed to the public" note
    //           is hidden anonymously post-reveal too, since the anonymous DTO never carries the date at all.
    // Assert — full pattern card renders; no reveal note at all (HasOilPatternRevealDateTime is false for this caller).
}

[Fact(DisplayName = "Renders nothing in the Oil Pattern section when there is no pattern and no category set")]
[UnitTest, Component("Website.Tournaments.Detail")]
public void Render_ShouldRenderNoOilPatternSection_WhenNoPatternOrCategoryIsSet()
{
    // Arrange — OilPatterns empty, PatternLengthCategory and PatternRatioCategory both null (today's existing case).
    // Assert — no "Oil Pattern" heading at all — confirms HasReducedOilPatternInfoOnly correctly excludes
    //          tournaments that simply never had a pattern, not just pre-reveal ones.
}
```

**`CreateTournamentTests.cs`** (extend) — the constructor's shared setup gains a `Mock<IClientTimeZoneService>(MockBehavior.Strict)` registered into `_ctx.Services`, matching how `_mockTournamentsApi`/`_mockBowlingCentersApi`/`_mockOilPatternsApi` are already set up. The component under test doesn't need to know or care how the service resolves the browser's time zone — that's `ClientTimeZoneServiceTests`' job (below) — so this test class mocks the service's contract directly rather than stubbing JS interop:

```csharp
[Fact(DisplayName = "Submitting with a reveal date/time converts it to UTC via the client time zone service")]
[UnitTest, Component("Website.Tournaments.CreateTournament")]
public async Task HandleCreateAsync_ShouldConvertOilPatternRevealDateTimeToUtc_WhenProvided()
{
    // Arrange — render the component, fill required fields, set the "oil-pattern-reveal" input to a known
    //           local value (e.g. new DateTime(2026, 8, 15, 17, 0, 0)); set up
    //           _mockClientTimeZoneService.Setup(s => s.ToUtcAsync(that value)).ReturnsAsync(a known UTC
    //           DateTimeOffset).Verifiable(); set up _mockTournamentsApi.CreateTournamentAsync to capture
    //           the request.
    // Act — submit the form.
    // Assert — captured request.Tournament.OilPatternRevealDateTime equals the UTC value the mock returned;
    //          _mockClientTimeZoneService.VerifyAll() confirms ToUtcAsync was actually called with the
    //          entered local value.
}

[Fact(DisplayName = "Submitting with no reveal date/time sends null and does not call the client time zone service")]
[UnitTest, Component("Website.Tournaments.CreateTournament")]
public async Task HandleCreateAsync_ShouldSendNullOilPatternRevealDateTime_WhenNotProvided()
{
    // Arrange — render, fill required fields, leave the reveal field untouched. _mockClientTimeZoneService
    //           is MockBehavior.Strict with no ToUtcAsync setup, so an unexpected call fails the test.
    // Act — submit.
    // Assert — captured request.Tournament.OilPatternRevealDateTime is null.
}
```

**`ClientTimeZoneServiceTests.cs`** (new, `tests/Neba.Website.Tests/Time/`) — covers the shared service's own JS interop and fallback behavior directly, using a mocked `IJSRuntime`/`IJSObjectReference` (or bUnit's `JSInterop` test double, whichever this test project already has precedent for with `Microsoft.JSInterop` mocking):

```csharp
[Fact(DisplayName = "ToLocalAsync converts a UTC instant using the browser's resolved IANA time zone")]
[UnitTest, Component("Website.Time")]
public async Task ToLocalAsync_ShouldConvertUsingBrowserTimeZone()
{
    // Arrange — JS interop stubbed to return "America/Los_Angeles"; a known UTC DateTimeOffset.
    // Act — service.ToLocalAsync(utc).
    // Assert — result equals utc converted via TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles")
    //          directly (not a hardcoded offset), and result.Offset reflects PDT/PST correctly for that date.
}

[Fact(DisplayName = "ToUtcAsync converts a local wall-clock value using the browser's resolved IANA time zone")]
[UnitTest, Component("Website.Time")]
public async Task ToUtcAsync_ShouldConvertUsingBrowserTimeZone()
{
    // Arrange — JS interop stubbed to return "America/Los_Angeles"; a local DateTime.
    // Act — service.ToUtcAsync(local).
    // Assert — result equals the expected UTC instant via TimeZoneInfo directly.
}

[Fact(DisplayName = "GetTimeZoneAsync falls back to UTC and logs a warning when the browser's time zone ID is unrecognized")]
[UnitTest, Component("Website.Time")]
public async Task ToLocalAsync_ShouldFallBackToUtcAndLogWarning_WhenTimeZoneIdIsUnrecognized()
{
    // Arrange — JS interop stubbed to return an invalid string (e.g. "Not/AZone"); FakeLogger<ClientTimeZoneService>.
    // Act — service.ToLocalAsync(utc).
    // Assert — result equals utc unchanged (UTC fallback, offset zero); FakeLogger snapshot contains a
    //          Warning-level entry (per this repo's FakeLogger convention for log-content assertions).
}

[Fact(DisplayName = "GetTimeZoneAsync only resolves the browser's time zone once per instance")]
[UnitTest, Component("Website.Time")]
public async Task GetTimeZoneAsync_ShouldOnlyCallJsInteropOnce_AcrossMultipleConversions()
{
    // Arrange — JS interop mock set up with .Verifiable(), called via ToLocalAsync/ToUtcAsync twice.
    // Act — call ToLocalAsync twice (or ToLocalAsync then ToUtcAsync).
    // Assert — the JS "import"/"getTimeZoneId" calls happened exactly once (Times.Once), confirming the
    //          per-circuit caching that's the whole point of extracting this as a Scoped service.
}
```

**News tests** (`tests/Neba.Website.Tests/News/NewsListTests.cs`, `NewsDetailTests.cs`, `CreateArticleTests.cs`, `EditArticleTests.cs` — extend, exact file names per whatever already exists) — replace each file's existing JSInterop setup for `browser-time.js`'s old `getTimezoneOffsetMinutes` with a mocked `IClientTimeZoneService` registration (same shape as `CreateTournamentTests` above), and update any assertions that depended on the old offset-based conversion to instead set up the mock's `ToLocalAsync`/`ToUtcAsync` return values directly. This is a mechanical migration of existing passing tests to the new seam, not new test cases — flag if you'd rather leave the News tests as-is and only add the new shared-service tests, since migrating passing tests carries its own small risk of introducing a mistake in otherwise-working coverage.

**Documents tests** — extend existing `NebaDocument`/`TournamentRules`/`Bylaws` component tests (if any assert on the exact "Last updated" text) with a case confirming the displayed date reflects `IClientTimeZoneService.ToLocalAsync(...)`'s result rather than the raw UTC value — this is new coverage, since the current unconverted behavior was never a deliberate, tested contract.
