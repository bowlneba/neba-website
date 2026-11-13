# Hall of Fame Points

A public page that lists every bowler not inducted for Superior Performance, with the Hall of Fame points they have earned, so members can see who is close to the 36-point mark. The API calculates the points from the title, Bowler of the Year and membership history we already hold; no legacy base-points table comes over.

## Decisions locked in during scoping

- **Not critical.** NFR checkpoint skipped: public, read-only, names already public.
- **Permission:** anonymous (`AllowAnonymous()`), no new policy, no new role.
- **Compute in memory, cache with `IFusionCache`.** No DB view, no points table, no job. The data is a few thousand rows and changes only when a tournament's results sync or a season's awards are assigned.
- **Who is listed:** every bowler with at least 1 point who is **not** inducted for Superior Performance. A bowler inducted only for Meritorious Service or Friend of NEBA still appears, with their Superior Performance points. The page says so.
- **Rule table travels in the response.** The page's hard-coded point list renders from it, so text and math cannot drift.
- **Lazy detail.** The list returns totals only. `GET /hall-of-fame/points/{bowlerId}` returns the point breakdown and the counted membership years when a row expands or the membership modal opens.
- **Membership years:** the list shows a count; the modal shows each year with the membership type behind it. Only types that count are used: a new `CountsTowardHallOfFame` flag on `MembershipType` (alongside `QualifiesAsPriorMembership`). Counts: New, Renewal, Military, Honorary, Courtesy, Time Honored. Does not count: Non-Member, Guest, Reservation. Years are calendar years of each counting membership's end date, matching the rollover in `MembershipRules`.
- **Membership data starts in 1999, so the disclaimer is per bowler.** The response flags `MembershipHistoryComplete = false` only when the bowler has counting (eligible) membership records and no `New` record. `New` is once per lifetime and a bowler's first yearly membership, so with it on file every later year is on file too. Counting records without a `New` (about 245 bowlers hold a Renewal but no New) mean the bowler joined before our records begin, so the count can understate and the page shows the disclaimer on that row. A bowler with no counting records has nothing to understate, so no disclaimer. The response also carries `MembershipDataStartYear` (1999) for the disclaimer text. The API never marks a bowler ineligible on membership.
- **No-membership winners:** a guest or non-member can win and earn points. They get the title points, a season count of 1, and 0 membership years (1 if they buy a membership later). Counting types only, so the Guest or Non-Member record every entrant holds adds no year. They show no disclaimer: they have no counting records, so the count of 0 is right. If they later buy a Renewal with no New on file, the count is 1 and the disclaimer appears.
- **No API paging.** The legacy base table had 347 bowlers, so the list is a few hundred rows (under 100 KB of JSON). The API returns all rows; search runs on the full list in the page, which renders every row (Phase 2 decided against virtualizing, so the JS search can see all rows). If the list ever grows past a few thousand rows, paging or virtualizing is an additive change.
- **"Different years" criterion** counts distinct seasons that have at least one point-earning title or award, as `HallOfFameBase.linq` does.
- **Detail route is reusable.** `GET /hall-of-fame/points/{bowlerId}` works for any bowler, inducted or not: name, induction categories (empty if none), total points, point breakdown (each title or Bowler of the Year win with date and points), and counted membership years. A later Hall of Fame name-click modal can call it. Nothing in this feature builds that modal.
- **Cache refresh:** 14-day expiry as a floor (data rarely changes). Evict by `PointsTag` when tournament results sync (`SyncTournamentResultsJob`, where titles arrive), when a Bowler of the Year award is assigned (each `Assign*` job in `Legacy/Seasons/Complete` that adds a point-earning award), and when a legacy induction syncs (`NewHallOfFameInductionSyncJob`). Membership purchases do not evict (they would churn the cache all day); a membership year can lag up to 14 days. A bowler-name fix needs the existing manual cache clear (`ClearCache`), or the per-bowler tag `neba:bowlers:{id}`.
- **Point rules come from `data-migration/HallOfFameBase.linq`:** per-type title points, Bowler of the Year points, Singles before 1991 = 12, legacy types (High-Roller 3, Over 40 and 40-49 = 2). Zero-point types are listed explicitly. Types with the same page label share one rule (Senior and Senior/Women; Over/Under 40 and 50; Over 40 and 40-49), so the rule table has no duplicate rows.
- **Reconciliation done:** the linq check against the legacy base table found the website data correct. Only name fixes remain (duplicate records Oeik/Oeinck, Mammolit/Mammolite). That is data cleanup, outside this plan.

## Phase 1: API

Paths relative to `src/`. All code is a draft to paste and adjust.

### Domain: point rules

`Neba.Api/Features/HallOfFame/Domain/HallOfFamePointRule.cs`

```csharp
namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// One line of the Hall of Fame point table: what earns points and how many.
/// </summary>
/// <param name="Group">The table section the rule is shown under.</param>
/// <param name="Label">The title or award name.</param>
/// <param name="Points">The points earned.</param>
internal sealed record HallOfFamePointRule(string Group, string Label, int Points);
```

`Neba.Api/Features/HallOfFame/Domain/HallOfFamePointRules.cs`

```csharp
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// The Hall of Fame Superior Performance point table and thresholds.
/// </summary>
internal static class HallOfFamePointRules
{
    /// <summary>Points needed for Superior Performance.</summary>
    public const int RequiredPoints = 36;

    /// <summary>Different seasons with a title or award needed.</summary>
    public const int RequiredSeasonsWithPoints = 3;

    /// <summary>Membership years needed.</summary>
    public const int RequiredMembershipYears = 10;

    /// <summary>First year with membership records; earlier years are not counted.</summary>
    public const int MembershipDataStartYear = 1999;

    private const int PreNinetyOneSinglesCutoffYear = 1991;

    private const string AwardGroup = "Bowler of the Year Awards";
    private const string TitleGroup = "Tournament Titles";
    private const string LegacyGroup = "Legacy Titles";

    // Rules are declared before the lookups below: static initializers run in text order.
    private static readonly HallOfFamePointRule BowlerOfTheYear = new(AwardGroup, "Bowler of the Year", 12);
    private static readonly HallOfFamePointRule SeniorBowlerOfTheYear = new(AwardGroup, "Senior Bowler of the Year", 2);
    private static readonly HallOfFamePointRule SuperSeniorBowlerOfTheYear = new(AwardGroup, "Super Senior Bowler of the Year", 2);
    private static readonly HallOfFamePointRule WomanBowlerOfTheYear = new(AwardGroup, "Woman Bowler of the Year", 2);
    private static readonly HallOfFamePointRule RookieOfTheYear = new(AwardGroup, "Rookie of the Year", 2);

    private static readonly HallOfFamePointRule Singles = new(TitleGroup, "Singles Title", 6);
    private static readonly HallOfFamePointRule TournamentOfChampions = new(TitleGroup, "TOC Title", 6);
    private static readonly HallOfFamePointRule Doubles = new(TitleGroup, "Doubles Title", 6);
    private static readonly HallOfFamePointRule Invitational = new(TitleGroup, "Points Leader/Invitational Title", 6);
    private static readonly HallOfFamePointRule Masters = new(TitleGroup, "NEBA Masters Title", 6);
    private static readonly HallOfFamePointRule OverUnder = new(TitleGroup, "Over/Under Title", 3);
    private static readonly HallOfFamePointRule NonChampions = new(TitleGroup, "Non-Champions Title", 2);
    private static readonly HallOfFamePointRule Senior = new(TitleGroup, "Senior Title", 2);
    private static readonly HallOfFamePointRule Trios = new(TitleGroup, "NEBA Trios Title", 2);

    private static readonly HallOfFamePointRule SinglesBeforeNinetyOne = new(LegacyGroup, "NEBA Singles Title (prior to 1991)", 12);
    private static readonly HallOfFamePointRule HighRoller = new(LegacyGroup, "NEBA High-Roller Title", 3);
    private static readonly HallOfFamePointRule OverForty = new(LegacyGroup, "NEBA Over 40 Title", 2);

    /// <summary>Every rule, in display order. Sent to the page so its table cannot drift from the math.</summary>
    public static IReadOnlyList<HallOfFamePointRule> All { get; } =
    [
        BowlerOfTheYear, SeniorBowlerOfTheYear, SuperSeniorBowlerOfTheYear, WomanBowlerOfTheYear, RookieOfTheYear,
        Singles, TournamentOfChampions, Doubles, Invitational, Masters, OverUnder, NonChampions, Senior, Trios,
        SinglesBeforeNinetyOne, HighRoller, OverForty
    ];

    // A null value means the type is known and earns no points. A type missing from the map is a bug;
    // HallOfFamePointRulesTests fails when a new TournamentType or category has no entry.
    private static readonly Dictionary<TournamentType, HallOfFamePointRule?> TitleRules = new()
    {
        [TournamentType.Singles] = Singles,
        [TournamentType.Doubles] = Doubles,
        [TournamentType.TournamentOfChampions] = TournamentOfChampions,
        [TournamentType.Invitational] = Invitational,
        [TournamentType.Masters] = Masters,
        [TournamentType.OverUnderFiftyDoubles] = OverUnder,
        [TournamentType.OverUnderFortyDoubles] = OverUnder,
        [TournamentType.NonChampions] = NonChampions,
        [TournamentType.Senior] = Senior,
        [TournamentType.SeniorAndWomen] = Senior,
        [TournamentType.Trios] = Trios,
        [TournamentType.HighRoller] = HighRoller,
        [TournamentType.OverForty] = OverForty,
        [TournamentType.FortyToFortyNine] = OverForty,
        [TournamentType.Women] = null,
        [TournamentType.Youth] = null,
        [TournamentType.Eliminator] = null,
        [TournamentType.Baker] = null
    };

    private static readonly Dictionary<BowlerOfTheYearCategory, HallOfFamePointRule?> AwardRules = new()
    {
        [BowlerOfTheYearCategory.Open] = BowlerOfTheYear,
        [BowlerOfTheYearCategory.Senior] = SeniorBowlerOfTheYear,
        [BowlerOfTheYearCategory.SuperSenior] = SuperSeniorBowlerOfTheYear,
        [BowlerOfTheYearCategory.Woman] = WomanBowlerOfTheYear,
        [BowlerOfTheYearCategory.Rookie] = RookieOfTheYear,
        [BowlerOfTheYearCategory.Youth] = null
    };

    /// <summary>The rule for a title, or null when the title earns no points.</summary>
    public static HallOfFamePointRule? ForTitle(TournamentType type, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type == TournamentType.Singles && startDate.Year < PreNinetyOneSinglesCutoffYear)
        {
            return SinglesBeforeNinetyOne;
        }

        return TitleRules.GetValueOrDefault(type);
    }

    /// <summary>The rule for a Bowler of the Year award, or null when it earns no points.</summary>
    public static HallOfFamePointRule? ForAward(BowlerOfTheYearCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return AwardRules.GetValueOrDefault(category);
    }

    /// <summary>Whether the type has an entry in the table, including a deliberate zero.</summary>
    public static bool IsMapped(TournamentType type)
        => TitleRules.ContainsKey(type);

    /// <summary>Whether the category has an entry in the table, including a deliberate zero.</summary>
    public static bool IsMapped(BowlerOfTheYearCategory category)
        => AwardRules.ContainsKey(category);

    /// <summary>
    /// Whether all three Superior Performance criteria are met. Membership years can only be undercounted
    /// (records start in 1999), so a true result is reliable; a false one can miss a long-time member.
    /// </summary>
    public static bool MeetsAllCriteria(int totalPoints, int seasonsWithPoints, int membershipYears)
        => totalPoints >= RequiredPoints
            && seasonsWithPoints >= RequiredSeasonsWithPoints
            && membershipYears >= RequiredMembershipYears;
}
```

### Application: shared calculator and reader (`Features/HallOfFame`, feature root)

Both use cases share these. They sit in the feature root, not in a use-case folder, and depend only on other features' `Domain` namespaces, which `FeatureBoundaryTests` allows.

`Neba.Api/Features/HallOfFame/HallOfFamePointsCalculator.cs`

```csharp
using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame;

internal sealed record TitleWin(BowlerId BowlerId, SeasonId SeasonId, DateOnly StartDate, DateOnly EndDate, TournamentType TournamentType);

internal sealed record AwardWin(BowlerId BowlerId, SeasonId SeasonId, DateOnly SeasonEndDate, BowlerOfTheYearCategory Category);

internal sealed record MembershipRecord(BowlerId BowlerId, MembershipType Type, DateOnly EndDate);

internal sealed record EarnedPoint(SeasonId SeasonId, DateOnly Date, string Source, int Points);

internal sealed record MembershipYear(int Year, MembershipType Type);

/// <summary>
/// A bowler's counted membership years, and whether we hold their whole membership history.
/// </summary>
/// <param name="Years">Counted years, oldest first.</param>
/// <param name="HistoryComplete">
/// False only when the bowler has counting memberships but no <see cref="MembershipType.New"/> record: New is
/// once per lifetime, so without it the bowler joined before our records begin and <paramref name="Years"/> can understate.
/// </param>
internal sealed record MembershipSummary(IReadOnlyList<MembershipYear> Years, bool HistoryComplete)
{
    /// <summary>For a bowler with no counting memberships: no years, and nothing to understate.</summary>
    public static MembershipSummary None { get; } = new([], true);
}

internal sealed record BowlerPoints(BowlerId BowlerId, int TotalPoints, int SeasonsWithPoints, IReadOnlyList<EarnedPoint> Breakdown)
{
    public bool MeetsPoints => TotalPoints >= HallOfFamePointRules.RequiredPoints;

    public bool MeetsSeasons => SeasonsWithPoints >= HallOfFamePointRules.RequiredSeasonsWithPoints;
}

internal interface IHallOfFamePointsCalculator
{
    /// <summary>Points per bowler, only bowlers with at least one point. Breakdown is oldest first.</summary>
    IReadOnlyCollection<BowlerPoints> CalculatePoints(IReadOnlyCollection<TitleWin> titles, IReadOnlyCollection<AwardWin> awards);

    /// <summary>
    /// Membership summary per bowler: counted years (one per calendar year of the end date, oldest first, only
    /// for types that count toward the Hall of Fame) and whether the history is complete (see
    /// <see cref="MembershipSummary"/>). Bowlers with no records at all are absent; use
    /// <see cref="MembershipSummary.None"/> for them.
    /// </summary>
    IReadOnlyDictionary<BowlerId, MembershipSummary> SummarizeMemberships(IReadOnlyCollection<MembershipRecord> memberships);
}

internal sealed class HallOfFamePointsCalculator
    : IHallOfFamePointsCalculator
{
    public IReadOnlyCollection<BowlerPoints> CalculatePoints(IReadOnlyCollection<TitleWin> titles, IReadOnlyCollection<AwardWin> awards)
    {
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(awards);

        var earned = titles
            .Select(title => (title.BowlerId, Rule: HallOfFamePointRules.ForTitle(title.TournamentType, title.StartDate), title.SeasonId, Date: title.EndDate))
            .Concat(awards
                .Select(award => (award.BowlerId, Rule: HallOfFamePointRules.ForAward(award.Category), award.SeasonId, Date: award.SeasonEndDate)))
            .Where(entry => entry.Rule is not null)
            .Select(entry => (entry.BowlerId, Point: new EarnedPoint(entry.SeasonId, entry.Date, entry.Rule!.Label, entry.Rule.Points)));

        return [.. earned
            .GroupBy(entry => entry.BowlerId)
            .Select(group =>
            {
                var breakdown = group.Select(entry => entry.Point).OrderBy(point => point.Date).ToList();

                return new BowlerPoints(
                    group.Key,
                    breakdown.Sum(point => point.Points),
                    breakdown.Select(point => point.SeasonId).Distinct().Count(),
                    breakdown);
            })];
    }

    public IReadOnlyDictionary<BowlerId, MembershipSummary> SummarizeMemberships(IReadOnlyCollection<MembershipRecord> memberships)
    {
        ArgumentNullException.ThrowIfNull(memberships);

        return memberships
            .GroupBy(membership => membership.BowlerId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var counting = group.Where(membership => membership.Type.CountsTowardHallOfFame).ToList();

                    var years = counting
                        .GroupBy(membership => membership.EndDate.Year)
                        .Select(year => new MembershipYear(year.Key, year.OrderBy(membership => membership.EndDate).First().Type))
                        .OrderBy(year => year.Year)
                        .ToList();

                    // Incomplete only when counting memberships exist without the lifetime-once New record.
                    var historyComplete = counting.Count == 0
                        || counting.Any(membership => membership.Type == MembershipType.New);

                    return new MembershipSummary(years, historyComplete);
                });
    }
}
```

`Neba.Api/Features/HallOfFame/HallOfFamePointsReader.cs`

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame;

/// <summary>
/// Loads the facts the Hall of Fame point calculation needs. Passing a bowler narrows each load to that bowler.
/// </summary>
internal sealed class HallOfFamePointsReader(AppDbContext appDbContext)
{
    private readonly IQueryable<Bowler> _bowlers = appDbContext.Bowlers.AsNoTracking();
    private readonly IQueryable<HallOfFameInduction> _inductions = appDbContext.HallOfFameInductions.AsNoTracking();
    private readonly IQueryable<HistoricalTournamentChampion> _historicalChampions = appDbContext.HistoricalTournamentChampions.AsNoTracking();
    private readonly IQueryable<Tournament> _tournaments = appDbContext.Tournaments.AsNoTracking();
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<TitleWin>> LoadTitleWinsAsync(BowlerId? bowlerId, CancellationToken cancellationToken)
    {
        var historicalQuery = _historicalChampions
            .Select(champion => new
            {
                BowlerId = champion.Bowler.Id,
                champion.Tournament.SeasonId,
                champion.Tournament.StartDate,
                champion.Tournament.EndDate,
                champion.Tournament.TournamentType
            });

        // Titles recorded in this system (2019 on); earlier titles are the historical rows above.
        var recordedQuery = _tournaments
            .Where(tournament => tournament.TitleEligible)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1)
                .Select(result => new
                {
                    result.BowlerId,
                    tournament.SeasonId,
                    tournament.StartDate,
                    tournament.EndDate,
                    tournament.TournamentType
                }));

        if (bowlerId is { } id)
        {
            historicalQuery = historicalQuery.Where(row => row.BowlerId == id);
            recordedQuery = recordedQuery.Where(row => row.BowlerId == id);
        }

        var historical = await historicalQuery.ToListAsync(cancellationToken);
        var recorded = await recordedQuery.ToListAsync(cancellationToken);

        return [.. historical.Concat(recorded)
            .Select(row => new TitleWin(row.BowlerId, row.SeasonId, row.StartDate, row.EndDate, row.TournamentType))];
    }

    public async Task<IReadOnlyCollection<AwardWin>> LoadAwardWinsAsync(BowlerId? bowlerId, CancellationToken cancellationToken)
    {
        var query = _seasons
            .SelectMany(season => season.BowlerOfTheYearAwards
                .Select(award => new { award.BowlerId, SeasonId = season.Id, season.EndDate, award.Category }));

        if (bowlerId is { } id)
        {
            query = query.Where(row => row.BowlerId == id);
        }

        var rows = await query.ToListAsync(cancellationToken);

        return [.. rows.Select(row => new AwardWin(row.BowlerId, row.SeasonId, row.EndDate, row.Category))];
    }

    public async Task<IReadOnlyCollection<MembershipRecord>> LoadMembershipsAsync(IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bowlerIds);

        var rows = await _bowlers
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .SelectMany(bowler => bowler.Memberships
                .Select(membership => new { BowlerId = bowler.Id, membership.Type, membership.EndDate }))
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new MembershipRecord(row.BowlerId, row.Type, row.EndDate))];
    }

    /// <summary>Induction categories per inducted bowler, merged when a bowler has more than one induction.</summary>
    public async Task<IReadOnlyDictionary<BowlerId, IReadOnlyCollection<HallOfFameCategory>>> LoadInductedCategoriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _inductions
            .Select(induction => new { induction.BowlerId, induction.Categories })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BowlerId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<HallOfFameCategory>)[.. group.SelectMany(row => row.Categories).Distinct()]);
    }

    /// <summary>Display names ("First Last") for the given bowlers.</summary>
    public async Task<IReadOnlyDictionary<BowlerId, string>> LoadNamesAsync(IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bowlerIds);

        var rows = await _bowlers
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .Select(bowler => new { bowler.Id, bowler.Name })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Name.ToDisplayName());
    }
}
```

Verify when coding: EF translates `bowlerIds.Contains(bowler.Id)` with the ULID converter (an integration test covers it). If it does not, load names and memberships by the database ids, as `GetBowlerTitlesQueryHandler` does with `ShadowIdConfiguration`.

### Application: list (`Features/HallOfFame/ListHallOfFamePoints/`)

`ListHallOfFamePointsQuery.cs`

```csharp
using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed record ListHallOfFamePointsQuery
    : ICachedQuery<HallOfFamePointsDto>
{
    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.ListPoints;

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}
```

`HallOfFamePointsDto.cs` (primitives only, per the cached-DTO rule)

```csharp
namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

/// <summary>The Hall of Fame point table, thresholds, and each bowler's standing.</summary>
public sealed record HallOfFamePointsDto
{
    public required IReadOnlyCollection<HallOfFamePointRuleDto> Rules { get; init; }

    public required int RequiredPoints { get; init; }

    public required int RequiredSeasonsWithPoints { get; init; }

    public required int RequiredMembershipYears { get; init; }

    /// <summary>First year with membership records; earlier years are not counted.</summary>
    public required int MembershipDataStartYear { get; init; }

    public required IReadOnlyCollection<HallOfFamePointsStandingDto> Standings { get; init; }
}

public sealed record HallOfFamePointRuleDto
{
    public required string Group { get; init; }

    public required string Label { get; init; }

    public required int Points { get; init; }
}

public sealed record HallOfFamePointsStandingDto
{
    /// <summary>ULID string.</summary>
    public required string BowlerId { get; init; }

    /// <summary>"First Last".</summary>
    public required string BowlerName { get; init; }

    public required int TotalPoints { get; init; }

    public required int SeasonsWithPoints { get; init; }

    /// <summary>Counted membership years since the data start year.</summary>
    public required int MembershipYears { get; init; }

    /// <summary>
    /// False only when the bowler has counting memberships but no New membership on file: the bowler joined
    /// before the records begin and the count can understate. True otherwise, including no counting memberships.
    /// </summary>
    public required bool MembershipHistoryComplete { get; init; }

    /// <summary>True when points, seasons and membership years all meet the Superior Performance criteria.</summary>
    public required bool Eligible { get; init; }

    public required bool MeetsPoints { get; init; }

    public required bool MeetsSeasons { get; init; }

    /// <summary>Category names of an induction for something other than Superior Performance; empty if none.</summary>
    public required IReadOnlyCollection<string> InductedCategories { get; init; }
}
```

`ListHallOfFamePointsQueryHandler.cs`

```csharp
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed class ListHallOfFamePointsQueryHandler(
    HallOfFamePointsReader reader,
    IHallOfFamePointsCalculator calculator)
        : IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto>
{
    public async Task<HallOfFamePointsDto> HandleAsync(ListHallOfFamePointsQuery query, CancellationToken cancellationToken)
    {
        var titles = await reader.LoadTitleWinsAsync(null, cancellationToken);
        var awards = await reader.LoadAwardWinsAsync(null, cancellationToken);
        var inducted = await reader.LoadInductedCategoriesAsync(cancellationToken);

        var points = calculator.CalculatePoints(titles, awards)
            .Where(bowler => !(inducted.TryGetValue(bowler.BowlerId, out var categories)
                && categories.Contains(HallOfFameCategory.SuperiorPerformance)))
            .ToList();

        var bowlerIds = points.Select(bowler => bowler.BowlerId).ToList();
        var names = await reader.LoadNamesAsync(bowlerIds, cancellationToken);
        var memberships = calculator.SummarizeMemberships(await reader.LoadMembershipsAsync(bowlerIds, cancellationToken));

        // Points high to low, then name. There is no rank: the Hall of Fame is not a standings race.
        var standings = points
            .OrderByDescending(bowler => bowler.TotalPoints)
            .ThenBy(bowler => names[bowler.BowlerId], StringComparer.OrdinalIgnoreCase)
            .Select(bowler =>
            {
                var membership = memberships.GetValueOrDefault(bowler.BowlerId, MembershipSummary.None);

                return new HallOfFamePointsStandingDto
                {
                    BowlerId = bowler.BowlerId.ToString(),
                    BowlerName = names[bowler.BowlerId],
                    TotalPoints = bowler.TotalPoints,
                    SeasonsWithPoints = bowler.SeasonsWithPoints,
                    MembershipYears = membership.Years.Count,
                    MembershipHistoryComplete = membership.HistoryComplete,
                    Eligible = HallOfFamePointRules.MeetsAllCriteria(bowler.TotalPoints, bowler.SeasonsWithPoints, membership.Years.Count),
                    MeetsPoints = bowler.MeetsPoints,
                    MeetsSeasons = bowler.MeetsSeasons,
                    InductedCategories = inducted.TryGetValue(bowler.BowlerId, out var other)
                        ? [.. other.Select(category => category.Name)]
                        : []
                };
            })
            .ToList();

        return new HallOfFamePointsDto
        {
            Rules = [.. HallOfFamePointRules.All.Select(rule => new HallOfFamePointRuleDto
            {
                Group = rule.Group,
                Label = rule.Label,
                Points = rule.Points
            })],
            RequiredPoints = HallOfFamePointRules.RequiredPoints,
            RequiredSeasonsWithPoints = HallOfFamePointRules.RequiredSeasonsWithPoints,
            RequiredMembershipYears = HallOfFamePointRules.RequiredMembershipYears,
            MembershipDataStartYear = HallOfFamePointRules.MembershipDataStartYear,
            Standings = standings
        };
    }
}
```

`ListHallOfFamePointsEndpoint.cs`

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.ListHallOfFamePoints;

internal sealed class ListHallOfFamePointsEndpoint(IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto> queryHandler)
    : EndpointWithoutRequest<HallOfFamePointsResponse>
{
    private readonly IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("points");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListHallOfFamePoints")
            .WithTags("Public")
            .Produces<HallOfFamePointsResponse>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var dto = await _queryHandler.HandleAsync(new ListHallOfFamePointsQuery(), ct);

        var response = new HallOfFamePointsResponse
        {
            Rules = [.. dto.Rules.Select(rule => new HallOfFamePointRuleResponse
            {
                Group = rule.Group,
                Label = rule.Label,
                Points = rule.Points
            })],
            RequiredPoints = dto.RequiredPoints,
            RequiredSeasonsWithPoints = dto.RequiredSeasonsWithPoints,
            RequiredMembershipYears = dto.RequiredMembershipYears,
            MembershipDataStartYear = dto.MembershipDataStartYear,
            Standings = [.. dto.Standings.Select(standing => new HallOfFamePointsStandingResponse
            {
                BowlerId = standing.BowlerId,
                BowlerName = standing.BowlerName,
                TotalPoints = standing.TotalPoints,
                SeasonsWithPoints = standing.SeasonsWithPoints,
                MembershipYears = standing.MembershipYears,
                MembershipHistoryComplete = standing.MembershipHistoryComplete,
                Eligible = standing.Eligible,
                MeetsPoints = standing.MeetsPoints,
                MeetsSeasons = standing.MeetsSeasons,
                InductedCategories = standing.InductedCategories
            })]
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

`ListHallOfFamePointsSummary.cs`: same shape as `ListHallOfFameInductionsSummary`: `Summary = "Lists Hall of Fame points for bowlers not inducted for Superior Performance."`, a `Description` that states the points count toward Superior Performance only and membership years start in 1999, and `Response(200, ..., example: new HallOfFamePointsResponse { ... two rules, one standing ... })`.

### Application: detail (`Features/HallOfFame/GetHallOfFamePointsDetail/`)

`GetHallOfFamePointsDetailQuery.cs`

```csharp
using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed record GetHallOfFamePointsDetailQuery
    : ICachedQuery<ErrorOr<HallOfFamePointsDetailDto>>
{
    public required BowlerId BowlerId { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.HallOfFame.PointsDetail(BowlerId);

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}
```

`HallOfFamePointsDetailDto.cs` (primitives only)

```csharp
namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

public sealed record HallOfFamePointsDetailDto
{
    public required string BowlerId { get; init; }

    public required string BowlerName { get; init; }

    /// <summary>Names of every induction category the bowler holds; empty if not inducted.</summary>
    public required IReadOnlyCollection<string> InductedCategories { get; init; }

    public required int TotalPoints { get; init; }

    public required int SeasonsWithPoints { get; init; }

    /// <summary>Each point-earning title or award, oldest first.</summary>
    public required IReadOnlyCollection<HallOfFamePointsEntryDto> Breakdown { get; init; }

    /// <summary>Each counted membership year with the type behind it, oldest first.</summary>
    public required IReadOnlyCollection<HallOfFameMembershipYearDto> MembershipYears { get; init; }

    /// <summary>False only when counting memberships exist without a New record, so <see cref="MembershipYears"/> can understate.</summary>
    public required bool MembershipHistoryComplete { get; init; }
}

public sealed record HallOfFamePointsEntryDto
{
    public required string Source { get; init; }

    public required DateOnly Date { get; init; }

    public required int Points { get; init; }
}

public sealed record HallOfFameMembershipYearDto
{
    public required int Year { get; init; }

    /// <summary>Display name of the membership type.</summary>
    public required string MembershipType { get; init; }
}
```

`GetHallOfFamePointsDetailQueryHandler.cs`

```csharp
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;

internal sealed class GetHallOfFamePointsDetailQueryHandler(
    HallOfFamePointsReader reader,
    IHallOfFamePointsCalculator calculator)
        : IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>>
{
    public async Task<ErrorOr<HallOfFamePointsDetailDto>> HandleAsync(GetHallOfFamePointsDetailQuery query, CancellationToken cancellationToken)
    {
        BowlerId[] ids = [query.BowlerId];

        var names = await reader.LoadNamesAsync(ids, cancellationToken);
        if (!names.TryGetValue(query.BowlerId, out var name))
        {
            return BowlerErrors.NotFound(query.BowlerId);
        }

        var titles = await reader.LoadTitleWinsAsync(query.BowlerId, cancellationToken);
        var awards = await reader.LoadAwardWinsAsync(query.BowlerId, cancellationToken);
        var inducted = await reader.LoadInductedCategoriesAsync(cancellationToken);
        var memberships = calculator
            .SummarizeMemberships(await reader.LoadMembershipsAsync(ids, cancellationToken))
            .GetValueOrDefault(query.BowlerId, MembershipSummary.None);

        var points = calculator.CalculatePoints(titles, awards).SingleOrDefault();

        return new HallOfFamePointsDetailDto
        {
            BowlerId = query.BowlerId.ToString(),
            BowlerName = name,
            InductedCategories = inducted.TryGetValue(query.BowlerId, out var categories)
                ? [.. categories.Select(category => category.Name)]
                : [],
            TotalPoints = points?.TotalPoints ?? 0,
            SeasonsWithPoints = points?.SeasonsWithPoints ?? 0,
            Breakdown = [.. (points?.Breakdown ?? []).Select(entry => new HallOfFamePointsEntryDto
            {
                Source = entry.Source,
                Date = entry.Date,
                Points = entry.Points
            })],
            MembershipYears = [.. memberships.Years
                .Select(year => new HallOfFameMembershipYearDto { Year = year.Year, MembershipType = year.Type.Name })],
            MembershipHistoryComplete = memberships.HistoryComplete
        };
    }
}
```

`GetHallOfFamePointsDetailRequest.cs` and validator (same shape as `GetBowlerTitlesRequest` and its validator)

```csharp
internal sealed class GetHallOfFamePointsDetailRequest
{
    public required string BowlerId { get; set; }
}

internal sealed class GetHallOfFamePointsDetailRequestValidator : Validator<GetHallOfFamePointsDetailRequest>
{
    public GetHallOfFamePointsDetailRequestValidator()
    {
        RuleFor(r => r.BowlerId)
            .NotEmpty()
            .WithErrorCode("GetHallOfFamePointsDetailRequest.BowlerIdRequired")
            .WithMessage("Bowler ID is required.")
            .Length(26)
            .WithErrorCode("GetHallOfFamePointsDetailRequest.BowlerIdInvalidLength")
            .WithMessage("Bowler ID must be a 26-character ULID.");
    }
}
```

Each class goes in its own file. Verify when coding that a 26-character non-ULID string returns 400, not 500 (`GetBowlerTitles` has the same gap); add a ULID parse rule if it does not.

`GetHallOfFamePointsDetailEndpoint.cs`

```csharp
internal sealed class GetHallOfFamePointsDetailEndpoint(
    IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>> queryHandler)
    : Endpoint<GetHallOfFamePointsDetailRequest, HallOfFamePointsDetailResponse>
{
    public override void Configure()
    {
        Get("points/{bowlerId}");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetHallOfFamePointsDetail")
            .WithTags("Public")
            .Produces<HallOfFamePointsDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetHallOfFamePointsDetailRequest req, CancellationToken ct)
    {
        var result = await queryHandler.HandleAsync(
            new GetHallOfFamePointsDetailQuery { BowlerId = new BowlerId(req.BowlerId) }, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            // Stryker disable once Statement
            return;
        }

        // map DTO -> HallOfFamePointsDetailResponse field by field, as the list endpoint does
        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

`GetHallOfFamePointsDetailSummary.cs`: summary "Gets one bowler's Hall of Fame points." Description: points count toward Superior Performance only; works for inducted bowlers; membership years start in 1999. Responses 200 (example), 404.

### Edits to existing files

`Neba.Api.Contracts/Memberships/MembershipType.cs`: new constructor parameter after `qualifiesAsPriorMembership`, new property, new value on every type.

```csharp
/// <summary>
/// Whether a year held under this type counts toward the Hall of Fame membership-years criterion.
/// </summary>
public bool CountsTowardHallOfFame { get; }

// constructor: bool qualifiesAsPriorMembership, bool countsTowardHallOfFame, bool active
// CountsTowardHallOfFame = countsTowardHallOfFame;

// New, Renewal, Military, Honorary, Courtesy, TimeHonored: countsTowardHallOfFame: true
// NonMember, Guest, Reservation:                           countsTowardHallOfFame: false
```

`Neba.Api/Caching/CacheDescriptors.cs`, inside `HallOfFame`:

```csharp
/// <summary>
/// The tag shared by every cached Hall of Fame points entry; remove by it to evict them all.
/// </summary>
public const string PointsTag = "neba:hall-of-fame:points";

/// <summary>
/// Returns a cache descriptor for the Hall of Fame points list.
/// </summary>
public static CacheDescriptor ListPoints
    => new()
    {
        Key = "neba:hall-of-fame:points:list",
        Tags = ["neba", "neba:hall-of-fame", PointsTag]
    };

/// <summary>
/// Returns a cache descriptor for one bowler's Hall of Fame points detail. The bowler tag lets a bowler edit evict it.
/// </summary>
public static CacheDescriptor PointsDetail(BowlerId bowlerId)
    => new()
    {
        Key = $"neba:hall-of-fame:points:{bowlerId}",
        Tags = ["neba", "neba:hall-of-fame", PointsTag, $"neba:bowlers:{bowlerId}"]
    };
```

`Neba.Api/ApplicationConfiguration.cs`, in `AddServices()`:

```csharp
services.AddSingleton<IHallOfFamePointsCalculator, HallOfFamePointsCalculator>();
services.AddScoped<HallOfFamePointsReader>();
```

Cache eviction, one line after each existing eviction and only on the success path:

```csharp
await cache.RemoveByTagAsync(CacheDescriptors.HallOfFame.PointsTag, token: ct);
```

- `Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs` (after line 70, next to the champions eviction).
- `Legacy/Seasons/Complete/AssignBowlerOfTheYearAwardJob.cs`, `AssignWomanOfTheYearAwardJob.cs`, `AssignSuperSeniorBowlerOfTheYearAwardJob.cs`, and the Senior and Rookie assign jobs in the same folder. Skip `AssignYouthBowlerOfTheYearAwardJob` (no points) and the high block and high average jobs.
- `Legacy/HallOfFame/HallOfFame.cs`, `NewHallOfFameInductionSyncJob` (and any sibling job that adds a category to an induction), after it evicts the inductions tag.

`Neba.Api.Contracts/HallOfFame/IHallOfFameApi.cs`:

```csharp
/// <summary>
/// Lists Hall of Fame points for bowlers not inducted for Superior Performance, with the point table and thresholds.
/// </summary>
/// <param name="cancellationToken">A token to cancel the operation.</param>
[Get("/hall-of-fame/points")]
Task<IApiResponse<HallOfFamePointsResponse>> ListHallOfFamePointsAsync(CancellationToken cancellationToken = default);

/// <summary>
/// Gets one bowler's Hall of Fame points: breakdown and counted membership years. Works for inducted bowlers.
/// </summary>
/// <param name="bowlerId">The bowler's ULID string.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
[Get("/hall-of-fame/points/{bowlerId}")]
Task<IApiResponse<HallOfFamePointsDetailResponse>> GetHallOfFamePointsDetailAsync(string bowlerId, CancellationToken cancellationToken = default);
```

### Contracts (`Neba.Api.Contracts/HallOfFame/`)

`ListHallOfFamePoints/HallOfFamePointsResponse.cs` (three records, each with XML summaries; each in its own file)

```csharp
public sealed record HallOfFamePointsResponse
{
    public required IReadOnlyCollection<HallOfFamePointRuleResponse> Rules { get; init; }

    public required int RequiredPoints { get; init; }

    public required int RequiredSeasonsWithPoints { get; init; }

    public required int RequiredMembershipYears { get; init; }

    public required int MembershipDataStartYear { get; init; }

    public required IReadOnlyCollection<HallOfFamePointsStandingResponse> Standings { get; init; }
}

public sealed record HallOfFamePointRuleResponse
{
    public required string Group { get; init; }

    public required string Label { get; init; }

    public required int Points { get; init; }
}

public sealed record HallOfFamePointsStandingResponse
{
    public required string BowlerId { get; init; }

    public required string BowlerName { get; init; }

    public required int TotalPoints { get; init; }

    public required int SeasonsWithPoints { get; init; }

    public required int MembershipYears { get; init; }

    public required bool MembershipHistoryComplete { get; init; }

    public required bool Eligible { get; init; }

    public required bool MeetsPoints { get; init; }

    public required bool MeetsSeasons { get; init; }

    public required IReadOnlyCollection<string> InductedCategories { get; init; }
}
```

`GetHallOfFamePointsDetail/HallOfFamePointsDetailResponse.cs` (plus `HallOfFamePointsEntryResponse` and `HallOfFameMembershipYearResponse`): same fields as the detail DTOs above.

### Database

No schema change, no migration. The `MembershipType` flag is code, not stored.

### Authorization

`AllowAnonymous()` on both endpoints. No row in `docs/policies/README.md`.

### Tests (`tests/Neba.Api.Tests`, `tests/Neba.TestFactory`)

Traits on every test: `[UnitTest]` or `[IntegrationTest]`, `[Component("HallOfFame")]`, `DisplayName`, AAA comments, Shouldly, `MockBehavior.Strict`.

- `Features/HallOfFame/Domain/HallOfFamePointRulesTests.cs`
  - Every `TournamentType` in `TournamentType.List` satisfies `IsMapped` (a new type fails this test until someone picks its points).
  - Every `BowlerOfTheYearCategory` in `BowlerOfTheYearCategory.List` satisfies `IsMapped`.
  - Theory over each title type and each award category against the points table from the page.
  - Singles in 1990 returns 12 and in 1991 returns 6.
  - Zero-point types return null.
  - `All` has no duplicate label.
  - `MeetsAllCriteria`: false at 35 points, 2 seasons, or 9 membership years (one short each); true when all three are exactly at the threshold.
- `Features/HallOfFame/HallOfFamePointsCalculatorTests.cs`
  - Sums titles and awards for one bowler; a bowler with only zero-point wins is absent.
  - `SeasonsWithPoints` counts distinct seasons (two titles in one season count once).
  - Breakdown is oldest first.
  - `MeetsPoints` is false at 35 and true at 36; `MeetsSeasons` is false at 2 seasons and true at 3.
  - Membership years: only counting types; two memberships ending in the same year give one year; Guest and Non-Member give none; a November purchase counts for the next year.
  - `HistoryComplete`: true with a New record; false with only Renewal records (the 245 case); false with Renewal plus Guest records and no New; true with only Guest or Non-Member records; a bowler with no records is absent and `MembershipSummary.None` gives 0 years and true.
  - A guest winner who never joined: points and seasons count, 0 membership years, history complete. After a later Renewal with no New on file: 1 year, history incomplete.
- `Tests for MembershipType`: every type in `MembershipType.List` has the expected `CountsTowardHallOfFame` (extend the existing `MembershipType` tests).
- `Features/HallOfFame/ListHallOfFamePoints/ListHallOfFamePointsQueryHandlerTests.cs` (integration style, as `GetBowlerTitlesQueryHandler` tests)
  - Excludes a Superior Performance inductee.
  - Keeps a bowler inducted for Meritorious Service only, with `InductedCategories` set.
  - Excludes zero-point bowlers.
  - `Eligible` is true only when points, seasons and membership years all pass; a bowler with 36 or more points but 2 seasons, or but 8 membership years, is not eligible (`MeetsPoints` still true).
  - Orders by points then name; equal points sort by name. No rank is returned.
  - Counts titles from both historical champions and recorded results.
  - Membership years and `MembershipHistoryComplete` for bowlers with points only.
  - Confirms `bowlerIds.Contains(...)` translates in Postgres.
- `Features/HallOfFame/GetHallOfFamePointsDetailQueryHandlerTests.cs`
  - Inductee returns points and categories; bowler with no points returns empty breakdown, total 0, name set; unknown bowler returns `Bowler.NotFound`; breakdown and membership years carry the type name.
- Endpoint integration tests for both routes: 200 anonymous with no token, 400 for a bad id, 404 for an unknown id, and an eviction test that a `RemoveByTagAsync(PointsTag)` makes the next call rebuild the list.
- Job tests: extend each edited job's tests to assert the points tag is evicted on success and not on failure.
- Factories in `Neba.TestFactory/HallOfFame/`, each with `Create()` (nullable parameters, const defaults) and `Bogus(int count, int? seed)` where a collection is useful: `TitleWinFactory`, `AwardWinFactory`, `MembershipRecordFactory`, `HallOfFamePointsStandingDtoFactory`, `HallOfFamePointsDtoFactory`, `HallOfFamePointsDetailDtoFactory`, and response factories for the Website tests in Phase 2.
- Check `FeatureBoundaryTests` still passes (the new classes only reference other features' `Domain` namespaces).

### Deferred to Phase 2 / out of scope

- The page, search, modal and the link from the Hall of Fame page.
- The Hall of Fame name-click stats modal. The detail endpoint is built to serve it, but the modal is a later feature.
- Importing the legacy base-points table (not needed).
- Name fixes for duplicate bowler records (data cleanup).

## Phase 2: UI

Paths relative to `src/Neba.Website.Server/` unless noted. The functional breakdown and the mockup are confirmed; this is the code draft.

### Flow (from scoping)

1. `/hall-of-fame` links to `/hall-of-fame/points` from the Superior Performance blurb and from the criteria card.
2. `/hall-of-fame/points` loads once (`GET /hall-of-fame/points`). Two notes sit above the list: points count toward Superior Performance only (bowlers inducted for Service or Friend of NEBA are listed with their points), and membership years count from 1999.
3. A search box filters the list by name as the user types. The list has two parts, highest points first in each, with no rank:
   - **Eligible for the Hall of Fame** (`Eligible`), on the navy stage: bowlers who meet all three criteria. Shows points, seasons and membership years, each with a check.
   - **Not yet eligible**: everyone else, each as a dot on a 0 to 36 line. A bowler with 36 or more points but not the seasons or membership years stays here, at the top (highest points first), with a gold dot at the finish and what is still open: seasons "2 of 3, needs 1 more", membership "8 of 10 years, needs 2 more". Bowlers under 36 points show plain counts at the right.
4. A row marks `*` on the membership count when `MembershipHistoryComplete` is false; the note above explains it. An incomplete count under 10 says "count may be low" and never "needs N more". A count already at 10 or more is reliable even with the `*`.
5. Clicking a name or a membership count opens a modal that loads `GET /hall-of-fame/points/{bowlerId}`: the point breakdown and the counted membership years with type, plus the disclaimer when the history is incomplete.

### Mockups

Static HTML in `docs/plans/mockups/hall-of-fame-points/`: one self-contained file, `track.html` (invented names, the real point table). Three layouts were compared (ledger table, bands, track); the track was chosen, with the bands idea kept for the eligible group. The black bar at the top is review-only: preview phone width and the modal's error state. A script error shows as a red box on the page, not a blank page.

- `track.html`: the page notes and search, then two parts.
  - **Eligible for the Hall of Fame**, on the navy stage: name, points, seasons and membership years, all met. Examples: Ray Dunleavy (42 points, 28* membership years, count incomplete but already over 10) and Marcus Hale (36 points, 13 years).
  - **Not yet eligible**: a 0 to 36 line with a gold finish and a tick every 6 points (one title). Seasons and membership years at the right; on a phone they drop under the line. Examples at the top: Ken Almeida (42 points, 2 seasons, "needs 1 more") and Sue Whitfield (38 points, 6 seasons, 8 years, "needs 2 more").
  - Rules section from `Rules`, and one detail modal (totals, breakdown, membership years with type, a note when the history is incomplete, loading and error states). A `*` marks an incomplete history; guest winners show 0 years with no `*`. No rank anywhere.

### View models and mapping

`HallOfFame/HallOfFamePointsViewModel.cs` (new; each record in its own file in the real change)

```csharp
namespace Neba.Website.Server.HallOfFame;

/// <summary>The thresholds the page and modal compare against.</summary>
public sealed record HallOfFamePointRequirements
{
    public required int Points { get; init; }

    public required int Seasons { get; init; }

    public required int MembershipYears { get; init; }

    /// <summary>First year with membership records.</summary>
    public required int MembershipDataStartYear { get; init; }
}

public sealed record HallOfFamePointRuleViewModel
{
    public required string Label { get; init; }

    public required int Points { get; init; }
}

public sealed record HallOfFamePointRuleGroupViewModel
{
    public required string Name { get; init; }

    public required IReadOnlyCollection<HallOfFamePointRuleViewModel> Rules { get; init; }
}

public sealed record HallOfFamePointsStandingViewModel
{
    public required string BowlerId { get; init; }

    public required string BowlerName { get; init; }

    public required int TotalPoints { get; init; }

    public required int SeasonsWithPoints { get; init; }

    public required int MembershipYears { get; init; }

    public required bool MembershipHistoryComplete { get; init; }

    public required bool MeetsPoints { get; init; }

    public required bool MeetsSeasons { get; init; }

    public required bool MeetsMembershipYears { get; init; }

    /// <summary>Seasons still needed; 0 when met.</summary>
    public required int SeasonsNeeded { get; init; }

    /// <summary>Membership years still needed; 0 when met.</summary>
    public required int MembershipYearsNeeded { get; init; }

    /// <summary>Where the dot sits on the 0 to 36 line, 0 to 100. Whole numbers, so no culture issue in CSS.</summary>
    public required int LinePercent { get; init; }

    /// <summary>"In the Hall of Fame for Meritorious Service"; null when the bowler is not inducted.</summary>
    public string? InductedLine { get; init; }
}

public sealed record HallOfFamePointsViewModel
{
    public required HallOfFamePointRequirements Requirements { get; init; }

    public required IReadOnlyCollection<HallOfFamePointRuleGroupViewModel> RuleGroups { get; init; }

    /// <summary>Bowlers who meet all three criteria, points high to low.</summary>
    public required IReadOnlyCollection<HallOfFamePointsStandingViewModel> Eligible { get; init; }

    /// <summary>Everyone else, points high to low.</summary>
    public required IReadOnlyCollection<HallOfFamePointsStandingViewModel> NotYetEligible { get; init; }
}

public sealed record HallOfFamePointsEntryViewModel
{
    public required string Source { get; init; }

    public required int Year { get; init; }

    public required int Points { get; init; }
}

public sealed record HallOfFameMembershipYearViewModel
{
    public required int Year { get; init; }

    public required string MembershipType { get; init; }
}

public sealed record HallOfFamePointsDetailViewModel
{
    public required string BowlerName { get; init; }

    public string? InductedLine { get; init; }

    public required int TotalPoints { get; init; }

    public required int SeasonsWithPoints { get; init; }

    public required bool MembershipHistoryComplete { get; init; }

    public required IReadOnlyCollection<HallOfFamePointsEntryViewModel> Breakdown { get; init; }

    public required IReadOnlyCollection<HallOfFameMembershipYearViewModel> MembershipYears { get; init; }
}
```

`HallOfFame/HallOfFameMappingExtensions.cs` (edit): add these blocks beside the existing `extension(HallOfFameInductionResponse dto)`; add `using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;` and `using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;`.

```csharp
extension(HallOfFamePointsResponse response)
{
    public HallOfFamePointsViewModel ToViewModel()
    {
        var requirements = new HallOfFamePointRequirements
        {
            Points = response.RequiredPoints,
            Seasons = response.RequiredSeasonsWithPoints,
            MembershipYears = response.RequiredMembershipYears,
            MembershipDataStartYear = response.MembershipDataStartYear
        };

        return new HallOfFamePointsViewModel
        {
            Requirements = requirements,
            RuleGroups = [.. response.Rules
                .GroupBy(rule => rule.Group)
                .Select(group => new HallOfFamePointRuleGroupViewModel
                {
                    Name = group.Key,
                    Rules = [.. group.Select(rule => new HallOfFamePointRuleViewModel { Label = rule.Label, Points = rule.Points })]
                })],
            Eligible = [.. response.Standings.Where(standing => standing.Eligible).Select(standing => standing.ToViewModel(requirements))],
            NotYetEligible = [.. response.Standings.Where(standing => !standing.Eligible).Select(standing => standing.ToViewModel(requirements))]
        };
    }
}

extension(HallOfFamePointsStandingResponse standing)
{
    public HallOfFamePointsStandingViewModel ToViewModel(HallOfFamePointRequirements requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        return new HallOfFamePointsStandingViewModel
        {
            BowlerId = standing.BowlerId,
            BowlerName = standing.BowlerName,
            TotalPoints = standing.TotalPoints,
            SeasonsWithPoints = standing.SeasonsWithPoints,
            MembershipYears = standing.MembershipYears,
            MembershipHistoryComplete = standing.MembershipHistoryComplete,
            MeetsPoints = standing.MeetsPoints,
            MeetsSeasons = standing.MeetsSeasons,
            MeetsMembershipYears = standing.MembershipYears >= requirements.MembershipYears,
            SeasonsNeeded = Math.Max(0, requirements.Seasons - standing.SeasonsWithPoints),
            MembershipYearsNeeded = Math.Max(0, requirements.MembershipYears - standing.MembershipYears),
            LinePercent = requirements.Points > 0
                ? Math.Min(100, standing.TotalPoints * 100 / requirements.Points)
                : 0,
            InductedLine = InductedLine(standing.InductedCategories)
        };
    }
}

extension(HallOfFamePointsDetailResponse response)
{
    public HallOfFamePointsDetailViewModel ToViewModel()
        => new()
        {
            BowlerName = response.BowlerName,
            InductedLine = InductedLine(response.InductedCategories),
            TotalPoints = response.TotalPoints,
            SeasonsWithPoints = response.SeasonsWithPoints,
            MembershipHistoryComplete = response.MembershipHistoryComplete,
            Breakdown = [.. response.Breakdown.Select(entry => new HallOfFamePointsEntryViewModel
            {
                Source = entry.Source,
                Year = entry.Date.Year,
                Points = entry.Points
            })],
            MembershipYears = [.. response.MembershipYears.Select(year => new HallOfFameMembershipYearViewModel
            {
                Year = year.Year,
                MembershipType = year.MembershipType
            })]
        };
}

// private static member of HallOfFameMappingExtensions, outside the extension blocks:
private static string? InductedLine(IReadOnlyCollection<string> categories)
    => categories.Count == 0
        ? null
        : "In the Hall of Fame for " + string.Join(" and ", categories.OrderBy(HallOfFameCategories.OrderOf));
```

### Pages

`HallOfFame/HallOfFamePoints.razor` (new)

```razor
@page "/hall-of-fame/points"
@using System.Globalization
@using Humanizer
@using Neba.Api.Contracts.HallOfFame
@using Neba.Website.Server.Services

@inject ApiExecutor ApiExecutor
@inject IHallOfFameApi HallOfFameApi
@inject IJSRuntime JsRuntime
@inject ILogger<HallOfFamePoints> Logger
@implements IAsyncDisposable

@rendermode @(new InteractiveServerRenderMode(prerender: false))

<PageTitle>Hall of Fame Points - BowlNEBA</PageTitle>
<HeadContent>
    <meta name="description"
          content="Hall of Fame points for NEBA bowlers: who is eligible for Superior Performance and who is closest, with the points earned for each title and award." />
    <meta property="og:title" content="Hall of Fame Points - NEBA" />
    <meta property="og:description" content="Who is eligible for the NEBA Hall of Fame Superior Performance category and who is closest." />
    <meta property="og:url" content="https://www.bowlneba.com/hall-of-fame/points" />
    <meta property="og:type" content="website" />
</HeadContent>

<div class="pts-page neba-space-y-6">
    <header class="pts-head">
        <h1>Hall of Fame Points</h1>
        <p>Who is closest to Superior Performance</p>
    </header>

    @if (!string.IsNullOrWhiteSpace(_errorMessage))
    {
        <NebaAlert Severity="NotifySeverity.Error"
                   Title="Error Loading Hall of Fame Points"
                   Message="@_errorMessage"
                   Dismissible="true"
                   OnDismiss="@(() => _errorMessage = null)" />
    }

    @if (_isLoading)
    {
        <div aria-busy="true">
            <NebaSkeletonLoader Type="SkeletonType.Custom" Width="100%" Height="6rem" />
            <NebaSkeletonLoader Type="SkeletonType.Custom" Width="100%" Height="18rem" />
        </div>
    }
    else if (_model is not null)
    {
        <div @ref="_root">
            <div class="pts-notes" role="note">
                <p>Points count toward Superior Performance only. Bowlers in the Hall of Fame for Meritorious Service or Friend of NEBA appear here with their Superior Performance points.</p>
                <p>Membership years count from @_model.Requirements.MembershipDataStartYear. A * means the bowler's first membership is not on file, so the count may be low.</p>
            </div>

            <p class="pts-criteria">
                Superior Performance takes <strong>@_model.Requirements.Points points</strong>, titles or awards in
                <strong>@_model.Requirements.Seasons different seasons</strong>, and
                <strong>@_model.Requirements.MembershipYears years</strong> as a member.
                <a href="#hof-points-rules">See how points are earned</a>.
            </p>

            <div class="pts-tools" hidden="@(!_searchReady)">
                <label class="pts-search">
                    <span class="pts-search__icon" aria-hidden="true">⌕</span>
                    <input type="search"
                           placeholder="Search by name"
                           aria-label="Search bowlers by name"
                           autocomplete="off"
                           data-pts-search />
                </label>
                <span class="pts-status" role="status" data-pts-status></span>
            </div>

            @if (_model.Eligible.Count > 0)
            {
                <section class="pts-stage" data-pts-section aria-labelledby="pts-eligible-title">
                    <h2 id="pts-eligible-title">Eligible for the Hall of Fame</h2>
                    <p class="pts-note">These bowlers meet all three criteria: @_model.Requirements.Points points, titles or awards in @_model.Requirements.Seasons different seasons, and @_model.Requirements.MembershipYears years as a member.</p>
                    @foreach (var bowler in _model.Eligible)
                    {
                        <div class="pts-qrow" data-pts-row data-name="@bowler.BowlerName" @key="bowler.BowlerId">
                            <div class="pts-who">
                                <button type="button" class="pts-name" @onclick="@(() => Open(bowler))">@bowler.BowlerName</button>
                                @if (bowler.InductedLine is not null)
                                {
                                    <span class="pts-inducted">@bowler.InductedLine</span>
                                }
                            </div>
                            <div class="pts-big"><b>@bowler.TotalPoints</b><span>points</span></div>
                            <ul class="pts-crit">
                                @SeasonsCriterion(bowler)
                                @MembershipCriterion(bowler)
                            </ul>
                        </div>
                    }
                </section>
            }

            <section class="pts-below" data-pts-section aria-labelledby="pts-below-title">
                <h2 id="pts-below-title">Not yet eligible</h2>
                <p class="pts-note">Bowlers at @_model.Requirements.Points or more points without the seasons or membership years yet are here, with what is still open. One tick on the line is a 6-point title.</p>
                <div class="pts-axis" aria-hidden="true">
                    <span></span>
                    <div class="pts-scale">
                        <span style="left: 0">0</span><span style="left: 33.333%">12</span><span style="left: 66.666%">24</span><span style="left: 100%">@_model.Requirements.Points</span>
                    </div>
                    <span>Seasons and membership</span>
                </div>
                @foreach (var bowler in _model.NotYetEligible)
                {
                    <div class="pts-row" data-pts-row data-name="@bowler.BowlerName" @key="bowler.BowlerId">
                        <div class="pts-who">
                            <button type="button" class="pts-name" @onclick="@(() => Open(bowler))">@bowler.BowlerName</button>
                            @if (bowler.InductedLine is not null)
                            {
                                <span class="pts-inducted">@bowler.InductedLine</span>
                            }
                        </div>
                        <div class="pts-line" role="img" aria-label="@(bowler.TotalPoints + " of " + _model.Requirements.Points + " points")">
                            @foreach (var position in TickPositions)
                            {
                                <span class="pts-tick @(position == "33.333" || position == "66.667" ? "pts-tick--mid" : null)" style="left: @(position)%"></span>
                            }
                            <span class="pts-reach" style="width: @(bowler.LinePercent)%"></span>
                            <span class="pts-finish"></span>
                            <span class="pts-dot @(bowler.MeetsPoints ? "pts-dot--met" : null)" style="left: @(bowler.LinePercent)%"></span>
                            <span class="pts-pts @(bowler.LinePercent >= 100 ? "pts-pts--edge" : null)" style="left: @(bowler.LinePercent)%">@bowler.TotalPoints</span>
                        </div>
                        @if (bowler.MeetsPoints)
                        {
                            <ul class="pts-crit">
                                @SeasonsCriterion(bowler)
                                @MembershipCriterion(bowler)
                            </ul>
                        }
                        else
                        {
                            <div class="pts-stats">
                                <span>@("season".ToQuantity(bowler.SeasonsWithPoints))@(bowler.MeetsSeasons ? " ✓" : null)</span>
                                <span><button type="button" class="pts-member" @onclick="@(() => Open(bowler))">@MembershipYearsLabel(bowler)</button> a member</span>
                            </div>
                        }
                    </div>
                }
            </section>

            @if (_model.Eligible.Count == 0 && _model.NotYetEligible.Count == 0)
            {
                <p class="pts-empty">No bowlers have Hall of Fame points yet.</p>
            }

            <section class="pts-rules" id="hof-points-rules" aria-labelledby="pts-rules-title">
                <h2 id="pts-rules-title">How points are earned</h2>
                <div class="pts-rule-groups">
                    @foreach (var group in _model.RuleGroups)
                    {
                        <section class="pts-rule-group">
                            <h3>@group.Name</h3>
                            <dl>
                                @foreach (var rule in group.Rules)
                                {
                                    <div class="pts-rule"><dt>@rule.Points</dt><dd>@rule.Label</dd></div>
                                }
                            </dl>
                        </section>
                    }
                </div>
            </section>
        </div>
    }

    <HallOfFamePointsModal IsOpen="@(_selected is not null)"
                           BowlerId="@(_selected?.BowlerId ?? string.Empty)"
                           BowlerName="@(_selected?.BowlerName ?? string.Empty)"
                           Requirements="@_model?.Requirements"
                           OnClose="CloseModal" />
</div>

@code {
    // Ticks every 6 points on the 0 to 36 line: 6/36 of the width apart.
    private static readonly string[] TickPositions = ["16.667", "33.333", "50", "66.667", "83.333"];

    private bool _isLoading = true;
    private HallOfFamePointsViewModel? _model;
    private string? _errorMessage;
    private HallOfFamePointsStandingViewModel? _selected;
    private ElementReference _root;
    private IJSObjectReference? _jsModule;
    private bool _attachAttempted;
    private bool _searchReady;

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync(
            "HallOfFame",
            "ListHallOfFamePoints",
            ct => HallOfFameApi.ListHallOfFamePointsAsync(ct));

        _isLoading = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        _model = result.Value.ToViewModel();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The list only exists once the data has loaded, so attach the search on the first render that has it.
        // One attempt only: a second render during the import, or a failed import, must not try again.
        if (_attachAttempted || _model is null)
        {
            return;
        }

        _attachAttempted = true;

        try
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./HallOfFame/HallOfFamePoints.razor.js");
            await _jsModule.InvokeVoidAsync("initialize", _root);
            _searchReady = true;
            StateHasChanged();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            Logger.LogWarning(ex, "[HallOfFamePoints] The search script could not be attached.");
        }
    }

    private void Open(HallOfFamePointsStandingViewModel bowler)
        => _selected = bowler;

    private void CloseModal()
        => _selected = null;

    private static string MembershipYearsLabel(HallOfFamePointsStandingViewModel bowler)
        => bowler.MembershipYears
            + (bowler.MembershipHistoryComplete ? string.Empty : "*")
            + " "
            + "year".ToQuantity(bowler.MembershipYears, ShowQuantityAs.None);

    private RenderFragment<HallOfFamePointsStandingViewModel> SeasonsCriterion => bowler => @<li class="@(bowler.MeetsSeasons ? "pts-met" : null)">
        <span class="pts-mark" aria-hidden="true">@(bowler.MeetsSeasons ? "✓" : "○")</span>
        @if (bowler.MeetsSeasons)
        {
            <span>@("season".ToQuantity(bowler.SeasonsWithPoints)) with points</span>
        }
        else
        {
            <span>@bowler.SeasonsWithPoints of @_model!.Requirements.Seasons seasons <span class="pts-need">needs @bowler.SeasonsNeeded more</span></span>
        }
    </li>;

    private RenderFragment<HallOfFamePointsStandingViewModel> MembershipCriterion => bowler => @<li class="@(bowler.MeetsMembershipYears ? "pts-met" : null)">
        <span class="pts-mark" aria-hidden="true">@(bowler.MeetsMembershipYears ? "✓" : "○")</span>
        <span>
            @if (bowler.MeetsMembershipYears)
            {
                <button type="button" class="pts-member" @onclick="@(() => Open(bowler))">@(bowler.MembershipYears + (bowler.MembershipHistoryComplete ? string.Empty : "*") + " membership " + "year".ToQuantity(bowler.MembershipYears, ShowQuantityAs.None))</button>
            }
            else
            {
                <button type="button" class="pts-member" @onclick="@(() => Open(bowler))">@(bowler.MembershipYears + (bowler.MembershipHistoryComplete ? string.Empty : "*") + " of " + _model!.Requirements.MembershipYears + " years")</button>
                @* A count we know is low cannot say "needs": the bowler may have joined before the records begin. *@
                <span class="pts-need">@(bowler.MembershipHistoryComplete ? "needs " + bowler.MembershipYearsNeeded + " more" : "count may be low")</span>
            }
        </span>
    </li>;

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        try
        {
            await _jsModule.InvokeVoidAsync("dispose", _root);
            await _jsModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone; nothing to release.
        }
    }
}
```

`HallOfFame/HallOfFamePoints.razor.js` (new)

```js
import { normalize } from './HallOfFame.razor.js';

const instances = new WeakMap();

/** Reads the rows once, so typing does not re-normalize every name on every keystroke. */
export function buildIndex(root) {
    return [...root.querySelectorAll('[data-pts-row]')].map((row) => ({
        row,
        name: normalize(row.dataset.name),
    }));
}

/** Hides rows whose name does not contain the term, hides sections left empty, and updates the status text. Writes only hidden and text. */
export function applyFilter(root, search, index = buildIndex(root)) {
    const term = normalize(search);
    let shown = 0;

    index.forEach((entry) => {
        const visible = entry.name.includes(term);
        entry.row.hidden = !visible;
        if (visible) {
            shown += 1;
        }
    });

    root.querySelectorAll('[data-pts-section]').forEach((section) => {
        section.hidden = !section.querySelector('[data-pts-row]:not([hidden])');
    });

    // One always-present live region, so a zero-result search is announced as a text change.
    const status = root.querySelector('[data-pts-status]');
    if (status) {
        const none = shown === 0 && index.length > 0;
        status.classList.toggle('pts-status--empty', none);

        if (none) {
            status.textContent = 'No bowlers match your search.';
        } else {
            status.textContent = term === '' ? '' : `Showing ${shown} of ${index.length}`;
        }
    }

    return shown;
}

export function initialize(root) {
    if (!root || instances.has(root)) {
        return;
    }

    const input = root.querySelector('[data-pts-search]');
    const instance = { index: buildIndex(root), search: input?.value ?? '', cleanup: null };

    const onInput = () => {
        instance.search = input.value;
        applyFilter(root, instance.search, instance.index);
    };
    input?.addEventListener('input', onInput);
    instance.cleanup = () => input?.removeEventListener('input', onInput);

    instances.set(root, instance);
    applyFilter(root, instance.search, instance.index);
}

export function dispose(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.cleanup?.();
    instances.delete(root);
}
```

`HallOfFame/HallOfFamePoints.razor.js` has no `refresh`: the page never re-renders the rows after load (the list loads once).

`HallOfFame/HallOfFamePoints.razor.tests.js` (new)

```js
import { describe, test, expect } from '@jest/globals';
import { buildIndex, applyFilter, initialize, dispose } from './HallOfFamePoints.razor.js';

const row = (name) => `<div data-pts-row data-name="${name}">${name}</div>`;

function render() {
    document.body.innerHTML = `
        <div id="root">
            <input data-pts-search type="search">
            <p data-pts-status role="status"></p>
            <section data-pts-section id="eligible">${row('Marcus Hale')}</section>
            <section data-pts-section id="below">${row('José Marchetti')}${row('Tom Davis')}</section>
        </div>`;
    return document.getElementById('root');
}

const visible = (root) => [...root.querySelectorAll('[data-pts-row]:not([hidden])')].map((r) => r.dataset.name);
const status = (root) => root.querySelector('[data-pts-status]');

describe('applyFilter', () => {
    test('shows every row and clears the status for an empty term', () => {
        const root = render();
        applyFilter(root, '');
        expect(visible(root)).toHaveLength(3);
        expect(status(root).textContent).toBe('');
    });

    test('matches without accents and ignores case', () => {
        const root = render();
        applyFilter(root, 'JOSE');
        expect(visible(root)).toEqual(['José Marchetti']);
        expect(status(root).textContent).toBe('Showing 1 of 3');
    });

    test('hides a section when none of its rows match', () => {
        const root = render();
        applyFilter(root, 'marcus');
        expect(root.querySelector('#eligible').hidden).toBe(false);
        expect(root.querySelector('#below').hidden).toBe(true);
    });

    test('says so when nothing matches', () => {
        const root = render();
        applyFilter(root, 'zzz');
        expect(visible(root)).toHaveLength(0);
        expect(status(root).textContent).toBe('No bowlers match your search.');
        expect(status(root).classList.contains('pts-status--empty')).toBe(true);
    });
});

describe('initialize and dispose', () => {
    test('filters as the user types', () => {
        const root = render();
        initialize(root);
        const input = root.querySelector('[data-pts-search]');
        input.value = 'tom';
        input.dispatchEvent(new Event('input'));
        expect(visible(root)).toEqual(['Tom Davis']);
    });

    test('stops filtering after dispose', () => {
        const root = render();
        initialize(root);
        dispose(root);
        const input = root.querySelector('[data-pts-search]');
        input.value = 'tom';
        input.dispatchEvent(new Event('input'));
        expect(visible(root)).toHaveLength(3);
    });

    test('indexes each row once', () => {
        expect(buildIndex(render())).toHaveLength(3);
    });
});
```

`HallOfFame/HallOfFamePoints.razor.css` (new). Tokens come from `neba_theme.css`; `[hidden]` needs `!important` because rows set their own `display`.

```css
/* Eligible stage and the 0 to 36 line follow docs/plans/mockups/hall-of-fame-points/track.html. Gold marks the 36-point finish only. */
.pts-page { container-type: inline-size; max-width: 1040px; margin: 0 auto; }
.pts-page [hidden] { display: none !important; }

.pts-head { text-align: center; }
.pts-head h1 { font-family: var(--neba-font-display); font-size: 1.875rem; font-weight: 800; color: var(--neba-blue-700); margin: 0; }
.pts-head p { margin: .25rem 0 0; font-size: .875rem; color: var(--neba-gray-600); }

.pts-notes { margin: 1.5rem 0 1rem; padding: .875rem 1.25rem; background: #fff; border-left: 4px solid var(--neba-gold);
    border-radius: 0 var(--neba-radius) var(--neba-radius) 0; font-size: .9rem; color: #374151; }
.pts-notes p { margin: .25rem 0; max-width: 70ch; }
.pts-criteria { margin: 0 0 1.25rem; font-size: .9rem; color: var(--neba-gray-700); max-width: 70ch; }
.pts-criteria strong { font-family: var(--neba-font-display); color: var(--neba-blue-brand); }
.pts-criteria a { color: var(--neba-blue-700); }

.pts-tools { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: .5rem 1rem; margin-bottom: 1rem; }
.pts-search { position: relative; flex: 1 1 18rem; max-width: 26rem; }
.pts-search input { width: 100%; padding: .625rem .75rem .625rem 2.25rem; border: 1px solid var(--neba-gray-300);
    border-radius: var(--neba-radius-lg); font: inherit; background: #fff; }
.pts-search__icon { position: absolute; left: .75rem; top: 50%; translate: 0 -50%; color: var(--neba-gray-500); }
.pts-status { font-size: .8125rem; color: var(--neba-gray-600); min-height: 1.25rem; }
.pts-status--empty { color: var(--neba-accent-red); }
.pts-empty { padding: 2rem 1rem; text-align: center; color: var(--neba-gray-600); }

.pts-name { background: none; border: 0; padding: 0; cursor: pointer; font-weight: 600; text-align: left; }
.pts-name:hover { color: var(--neba-blue-700); text-decoration: underline; }
.pts-member { background: none; border: 0; border-bottom: 1px dotted var(--neba-gray-500); padding: 0; cursor: pointer; font-weight: 600; }
.pts-member:hover { color: var(--neba-blue-700); border-bottom-color: var(--neba-blue-700); }
.pts-inducted { display: block; font-size: .75rem; color: var(--neba-gray-600); font-weight: 400; }
.pts-who .pts-name { display: block; }

/* Eligible: the navy Hall of Fame stage */
.pts-stage { background: var(--neba-blue-brand); color: #F4F6FF; border-radius: var(--neba-radius-xl); padding: 1.5rem clamp(1rem, 4vw, 2rem) .5rem; margin-bottom: 2rem; }
.pts-stage h2 { margin: 0; font-family: var(--neba-font-display); font-weight: 800; font-size: 1.5rem; color: var(--neba-gold); }
.pts-stage .pts-note { margin: .125rem 0 1rem; font-size: .875rem; color: #AEB8E6; max-width: 70ch; }
.pts-stage :focus-visible { outline-color: var(--neba-gold); }
.pts-qrow { display: grid; grid-template-columns: 1fr 5.5rem 1.3fr; gap: .5rem 1.5rem; align-items: center; padding: 1rem 0; border-top: 1px solid rgba(255, 255, 255, .16); }
.pts-stage .pts-name { color: #fff; }
.pts-stage .pts-name:hover { color: var(--neba-gold); }
.pts-stage .pts-inducted { color: #AEB8E6; }
.pts-stage .pts-member { color: #fff; border-bottom-color: #8E9AD6; }
.pts-stage .pts-member:hover { color: var(--neba-gold); border-bottom-color: var(--neba-gold); }
.pts-big { text-align: center; }
.pts-big b { display: block; font-family: var(--neba-font-display); font-weight: 800; font-size: 2rem; line-height: 1; color: var(--neba-gold); }
.pts-big span { font-size: .75rem; color: #AEB8E6; }
.pts-stage .pts-mark { color: #8E9AD6; }
.pts-stage .pts-met .pts-mark { color: var(--neba-gold); font-weight: 700; }
.pts-stage .pts-need { color: #AEB8E6; }

/* Criteria lines, used in both parts */
.pts-crit { list-style: none; margin: 0; padding: 0; display: grid; gap: .25rem; font-size: .8125rem; }
.pts-crit li { display: flex; gap: .5rem; align-items: baseline; }
.pts-mark { width: 1rem; text-align: center; }

/* Not yet eligible: dots on the 0 to 36 line */
.pts-below { background: #fff; border: 1px solid var(--neba-gray-200); border-radius: var(--neba-radius-lg); padding: 1.25rem 1rem 1rem; }
.pts-below h2 { margin: 0; font-family: var(--neba-font-display); font-weight: 800; font-size: 1.5rem; color: var(--neba-blue-brand); }
.pts-below .pts-note { margin: .125rem 0 0; font-size: .875rem; color: var(--neba-gray-600); max-width: 70ch; }
.pts-below .pts-mark { color: var(--neba-gray-500); }
.pts-below .pts-met .pts-mark { color: var(--neba-success); font-weight: 700; }
.pts-below .pts-need { color: var(--neba-gray-600); }
.pts-row, .pts-axis { display: grid; grid-template-columns: 13rem 1fr 14rem; gap: 1rem; align-items: center; }
.pts-axis { padding: .75rem 0 .25rem; font-size: .75rem; color: var(--neba-gray-500); }
.pts-scale { position: relative; height: 1rem; }
.pts-scale span { position: absolute; translate: -50% 0; }
.pts-scale span:first-child { translate: 0 0; }
.pts-scale span:last-child { translate: -100% 0; color: var(--neba-gold-deep); font-weight: 600; }
.pts-row { padding: .5rem 0; border-top: 1px solid var(--neba-gray-200); }
.pts-line { position: relative; height: 1.75rem; }
.pts-line::before { content: ""; position: absolute; left: 0; right: 0; top: 50%; height: 2px; translate: 0 -50%; background: var(--neba-gray-200); }
.pts-tick { position: absolute; top: 50%; width: 1px; height: .75rem; translate: 0 -50%; background: var(--neba-gray-300); }
.pts-tick--mid { height: 1.1rem; background: var(--neba-gray-400); }
.pts-finish { position: absolute; right: 0; top: 0; bottom: 0; width: 3px; background: var(--neba-gold-deep); border-radius: 2px; }
.pts-reach { position: absolute; left: 0; top: 50%; height: 2px; translate: 0 -50%; background: var(--neba-blue-300); }
.pts-dot { position: absolute; top: 50%; width: 1.125rem; height: 1.125rem; translate: -50% -50%; border-radius: 50%;
    background: var(--neba-blue-700); border: 2px solid #fff; box-shadow: 0 0 0 1px var(--neba-blue-700); }
.pts-dot--met { background: var(--neba-gold); box-shadow: 0 0 0 1px var(--neba-gold-deep); }
.pts-pts { position: absolute; top: -.1rem; translate: -50% -100%; font: 800 .8125rem/1 var(--neba-font-display); color: var(--neba-blue-brand); }
.pts-pts--edge { translate: -100% -100%; }
.pts-stats { font-size: .8125rem; color: var(--neba-gray-600); display: grid; gap: .125rem; }

/* Rules, from the API's Rules */
.pts-rules { margin-top: 3rem; padding-top: 2rem; border-top: 2px solid var(--neba-gray-200); }
.pts-rules h2 { font-family: var(--neba-font-display); font-size: 1.5rem; margin: 0 0 1.25rem; color: var(--neba-gray-700); }
.pts-rule-groups { display: grid; grid-template-columns: repeat(3, 1fr); gap: 2rem; }
.pts-rule-group h3 { font-family: var(--neba-font-display); font-size: 1rem; margin: 0 0 .75rem; padding-bottom: .5rem; color: var(--neba-blue-700); border-bottom: 2px solid var(--neba-blue-700); }
.pts-rule-group dl { margin: 0; }
.pts-rule { display: grid; grid-template-columns: 2.25rem 1fr; gap: .5rem; align-items: baseline; padding: .3rem 0; font-size: .9rem; }
.pts-rule dt { font-family: var(--neba-font-display); font-weight: 800; font-size: 1.0625rem; color: var(--neba-blue-700); text-align: right; }
.pts-rule dd { margin: 0; color: var(--neba-gray-700); }

/* Phone: stack name, line, then stats. Nothing needs a tap to expand. */
@container (max-width: 760px) {
    .pts-qrow { grid-template-columns: 1fr auto; }
    .pts-qrow .pts-crit { grid-column: 1 / -1; }
    .pts-axis { display: none; }
    .pts-row { grid-template-columns: 1fr; gap: .25rem; padding: .75rem 0; }
    .pts-line { margin: 1.25rem .75rem .25rem 0; }
    .pts-stats { grid-auto-flow: column; justify-content: start; gap: 1rem; }
    .pts-rule-groups { grid-template-columns: 1fr; gap: 1.5rem; }
}
```

Check when building: that Blazor's scoped-CSS rewriting keeps `@container` blocks. If it does not, move the phone rules to a `@media (max-width: 767px)` block, the breakpoint `--neba-breakpoint-mobile` already names.

### Components

`HallOfFame/HallOfFamePointsModal.razor` (new). It depends on nothing from the list page: the later Hall of Fame name-click modal can use it with a bowler id and name alone (leave `Requirements` null and the "of 36" text drops out).

```razor
@using Humanizer
@using Neba.Api.Contracts.HallOfFame
@using Neba.Website.Server.Services

@inject ApiExecutor ApiExecutor
@inject IHallOfFameApi HallOfFameApi

<NebaModal IsOpen="@IsOpen" OnClose="@OnClose" MaxWidth="640px">
    <HeaderContent>
        <div class="pm-title-wrap">
            <h2 class="pm-title">@BowlerName</h2>
            @if (_data?.InductedLine is not null)
            {
                <span class="pm-flag">@_data.InductedLine</span>
            }
        </div>
    </HeaderContent>
    <ChildContent>
        <div class="pm-body">
            @if (_state == ModalState.Loading)
            {
                <div class="pm-state">
                    <NebaLoadingIndicator />
                    <div>Loading points…</div>
                </div>
            }
            else if (_state == ModalState.Error)
            {
                <div class="pm-state">
                    <h3>Could not load points</h3>
                    <p>Something went wrong fetching this bowler's record.</p>
                    <button type="button" class="pm-retry" @onclick="RetryAsync">Retry</button>
                </div>
            }
            else if (_data is not null)
            {
                <div class="pm-stats">
                    <div class="pm-stat">
                        <b>@_data.TotalPoints</b>
                        <span>@(Requirements is null ? "points" : "of " + Requirements.Points + " points")</span>
                    </div>
                    <div class="pm-stat">
                        <b>@_data.SeasonsWithPoints</b>
                        <span>@("season".ToQuantity(_data.SeasonsWithPoints, ShowQuantityAs.None)) with points@(Requirements is null ? null : " (" + Requirements.Seasons + " needed)")</span>
                    </div>
                    <div class="pm-stat">
                        <b>@(_data.MembershipYears.Count + (_data.MembershipHistoryComplete ? string.Empty : "*"))</b>
                        <span>membership @("year".ToQuantity(_data.MembershipYears.Count, ShowQuantityAs.None))@(Requirements is null ? null : " (" + Requirements.MembershipYears + " needed)")</span>
                    </div>
                </div>

                <h3>Points earned</h3>
                @if (_data.Breakdown.Count == 0)
                {
                    <p class="pm-muted">No points yet.</p>
                }
                else
                {
                    <table class="pm-table">
                        <thead>
                            <tr><th scope="col">Year</th><th scope="col">Title or award</th><th scope="col" class="pm-r">Points</th></tr>
                        </thead>
                        <tbody>
                            @foreach (var entry in _data.Breakdown)
                            {
                                <tr><td>@entry.Year</td><td>@entry.Source</td><td class="pm-r">@entry.Points</td></tr>
                            }
                        </tbody>
                        <tfoot>
                            <tr><td></td><td>Total</td><td class="pm-r">@_data.TotalPoints</td></tr>
                        </tfoot>
                    </table>
                }

                <h3>Membership years</h3>
                @if (_data.MembershipYears.Count == 0)
                {
                    <p class="pm-muted">No counted membership years. Guest and non-member entries do not count.</p>
                }
                else
                {
                    <ul class="pm-years">
                        @foreach (var year in _data.MembershipYears)
                        {
                            <li><span>@year.Year</span><span>@year.MembershipType</span></li>
                        }
                    </ul>
                }

                @if (!_data.MembershipHistoryComplete)
                {
                    <p class="pm-note">
                        This bowler's first membership is not on file. Records start in @(Requirements?.MembershipDataStartYear.ToString(CultureInfo.InvariantCulture) ?? "1999"), so the real count may be higher.
                    </p>
                }
            }
        </div>
    </ChildContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    [Parameter, EditorRequired]
    public string BowlerId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    /// <summary>The thresholds, when the caller has them. Null drops the "of 36" and "needed" text.</summary>
    [Parameter]
    public HallOfFamePointRequirements? Requirements { get; set; }

    private enum ModalState { Loading, Error, Data }

    private ModalState _state = ModalState.Loading;
    private HallOfFamePointsDetailViewModel? _data;
    private string? _lastLoadedBowlerId;

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen || string.IsNullOrEmpty(BowlerId) || BowlerId == _lastLoadedBowlerId)
        {
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _lastLoadedBowlerId = BowlerId;
        _state = ModalState.Loading;
        _data = null;
        StateHasChanged();

        var result = await ApiExecutor.ExecuteAsync(
            "HallOfFame",
            "GetHallOfFamePointsDetail",
            ct => HallOfFameApi.GetHallOfFamePointsDetailAsync(BowlerId, ct));

        if (result.IsError)
        {
            _state = ModalState.Error;
            return;
        }

        _data = result.Value.ToViewModel();
        _state = ModalState.Data;
    }

    private Task RetryAsync()
    {
        _lastLoadedBowlerId = null;
        return LoadAsync();
    }
}
```

Add `@using System.Globalization` at the top of the modal (used for the start-year text).

`HallOfFame/HallOfFamePointsModal.razor.css` (new): same look as the mockup's modal body; `NebaModal` supplies the shell.

```css
.pm-title { font-family: var(--neba-font-display); font-size: 1.375rem; font-weight: 800; margin: 0; }
.pm-flag { display: inline-flex; margin-top: .5rem; padding: .2rem .6rem; border-radius: 999px; background: var(--neba-blue-brand); color: var(--neba-gold); font-size: .75rem; font-weight: 600; }
.pm-body h3 { font-family: var(--neba-font-display); font-size: 1rem; margin: 1.25rem 0 .5rem; color: var(--neba-gray-700); }
.pm-stats { display: flex; gap: 2rem; margin-bottom: 1.25rem; }
.pm-stat b { display: block; font-family: var(--neba-font-display); font-size: 1.75rem; line-height: 1.1; color: var(--neba-blue-700); }
.pm-stat span { font-size: .8125rem; color: var(--neba-gray-600); }
.pm-table { width: 100%; border-collapse: collapse; font-size: .9rem; }
.pm-table th { text-align: left; font-weight: 600; font-size: .8125rem; color: var(--neba-gray-600); padding: .4rem .5rem; border-bottom: 1px solid var(--neba-gray-300); }
.pm-table td { padding: .4rem .5rem; border-bottom: 1px solid var(--neba-gray-200); }
.pm-table tfoot td { font-weight: 700; border-bottom: 0; }
.pm-r { text-align: right; font-variant-numeric: tabular-nums; }
.pm-years { columns: 2; column-gap: 1.5rem; margin: 0; padding: 0; list-style: none; font-size: .9rem; }
.pm-years li { display: flex; justify-content: space-between; gap: .75rem; padding: .25rem 0; border-bottom: 1px solid var(--neba-gray-200); break-inside: avoid; }
.pm-years span:last-child { color: var(--neba-gray-600); }
.pm-muted { color: var(--neba-gray-600); font-size: .9rem; margin: 0; }
.pm-note { margin: .75rem 0 0; padding: .625rem .875rem; background: #FFF8E6; border-left: 4px solid var(--neba-gold); font-size: .85rem; border-radius: 0 var(--neba-radius) var(--neba-radius) 0; }
.pm-state { padding: 2.5rem 1rem; text-align: center; color: var(--neba-gray-600); }
.pm-state h3 { color: #1f2937; margin: .5rem 0 .25rem; }
.pm-retry { margin-top: .75rem; padding: .5rem 1rem; border: 1px solid var(--neba-blue-700); background: #fff; color: var(--neba-blue-700); border-radius: var(--neba-radius); cursor: pointer; font-weight: 600; }
@media (max-width: 520px) { .pm-years { columns: 1; } .pm-stats { gap: 1.25rem; } }
```

### Edits to existing UI files

`HallOfFame/HallOfFame.razor`: link in the Superior Performance blurb.

```razor
<p>Bowlers with an outstanding competitive record in NEBA. Eligibility comes from earning 36 Hall of Fame points through tournament titles and Bowler of the Year awards. <a href="/hall-of-fame/points">See who is eligible and who is closest</a>.</p>
```

`HallOfFame/HallOfFame.razor`: in the criteria card, replace the third bullet; then delete the two grids that follow (the Bowler of the Year Awards and Tournament Titles cards, and the Legacy Titles card). The criteria card and its other two bullets stay.

```razor
<p class="text-gray-700 mb-3 leading-relaxed"><strong>• Must accumulate 36 NEBA Hall of Fame points</strong> for titles and Bowler of the Year awards. <a href="/hall-of-fame/points">See the points for each title, and who is closest</a>.</p>
```

`wwwroot/sitemap.xml`: add beside the `/hall-of-fame` entry.

```xml
<url>
    <loc>https://www.bowlneba.com/hall-of-fame/points</loc>
</url>
```

(match the other entries' extra tags, such as `lastmod` or `changefreq`, when adding it).

`tests/e2e/mock-api/mock-api-server.ts`: add the data and the two routes. Every field the contract marks required is present (a missing required field fails every Playwright run).

```ts
const MOCK_HALL_OF_FAME_POINTS = {
  rules: [
    { group: 'Bowler of the Year Awards', label: 'Bowler of the Year', points: 12 },
    { group: 'Tournament Titles', label: 'Singles Title', points: 6 },
    { group: 'Legacy Titles', label: 'NEBA High-Roller Title', points: 3 },
  ],
  requiredPoints: 36,
  requiredSeasonsWithPoints: 3,
  requiredMembershipYears: 10,
  membershipDataStartYear: 1999,
  standings: [
    { bowlerId: '01JX0000000000000000000101', bowlerName: 'Ray Dunleavy', totalPoints: 42, seasonsWithPoints: 5, membershipYears: 28, membershipHistoryComplete: false, eligible: true, meetsPoints: true, meetsSeasons: true, inductedCategories: [] },
    { bowlerId: '01JX0000000000000000000102', bowlerName: 'Ken Almeida', totalPoints: 42, seasonsWithPoints: 2, membershipYears: 15, membershipHistoryComplete: true, eligible: false, meetsPoints: true, meetsSeasons: false, inductedCategories: [] },
    { bowlerId: '01JX0000000000000000000103', bowlerName: 'Walt Brennan', totalPoints: 34, seasonsWithPoints: 6, membershipYears: 28, membershipHistoryComplete: false, eligible: false, meetsPoints: false, meetsSeasons: true, inductedCategories: ['Meritorious Service'] },
    { bowlerId: '01JX0000000000000000000104', bowlerName: 'José Marchetti', totalPoints: 12, seasonsWithPoints: 2, membershipYears: 8, membershipHistoryComplete: true, eligible: false, meetsPoints: false, meetsSeasons: false, inductedCategories: [] },
  ],
};

const MOCK_HALL_OF_FAME_POINTS_DETAIL = (bowlerId: string) => {
  const standing = MOCK_HALL_OF_FAME_POINTS.standings.find((s) => s.bowlerId === bowlerId);
  if (!standing) return null;
  return {
    bowlerId: standing.bowlerId,
    bowlerName: standing.bowlerName,
    inductedCategories: standing.inductedCategories,
    totalPoints: standing.totalPoints,
    seasonsWithPoints: standing.seasonsWithPoints,
    membershipHistoryComplete: standing.membershipHistoryComplete,
    breakdown: [
      { source: 'Bowler of the Year', date: '2017-12-31', points: 12 },
      { source: 'Singles Title', date: '2019-03-10', points: 6 },
    ],
    membershipYears: [
      { year: 2024, membershipType: 'Renewal' },
      { year: 2025, membershipType: 'Renewal' },
    ],
  };
};
```

In `resolveStaticGetRoute` add the detail lookup (the list goes in the static route map beside `'/hall-of-fame/inductions'`):

```ts
  '/hall-of-fame/points': MOCK_HALL_OF_FAME_POINTS,
```

```ts
  if (pathname.startsWith('/hall-of-fame/points/')) {
    return MOCK_HALL_OF_FAME_POINTS_DETAIL(pathname.slice('/hall-of-fame/points/'.length));
  }
```

### UI rules that apply

- Humanizer, not hand-written plurals: `"season".ToQuantity(n)` and `"year".ToQuantity(n, ShowQuantityAs.None)` for the words, so a `*` can sit after the number. The API returns raw numbers (ADR-0010).
- Razor `@code`: no `< N =>` patterns, no `$"{x}"` interpolation (strings are joined with `+`), `@` on component attribute values, `[Parameter, EditorRequired]` on the modal's required parameters.
- Per-keystroke search lives in `HallOfFamePoints.razor.js`; the C# only handles clicks.
- Phone layout through a container query on the page; the modal scrolls its body.
- Read-only page: no `DirtyFormGuard`, no FAB.

### Tests (UI)

Factories (`tests/Neba.TestFactory/HallOfFame/`, public, `Create()` with nullable parameters and const defaults, `Bogus(int count, int? seed)` for the list): `HallOfFamePointsStandingResponseFactory`, `HallOfFamePointsResponseFactory`, `HallOfFamePointsDetailResponseFactory`. Signatures:

```csharp
public static class HallOfFamePointsStandingResponseFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    public static HallOfFamePointsStandingResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? totalPoints = null,          // default 18
        int? seasonsWithPoints = null,    // default 3
        int? membershipYears = null,      // default 11
        bool? membershipHistoryComplete = null,   // default true
        bool? eligible = null,            // default false
        bool? meetsPoints = null,         // default false
        bool? meetsSeasons = null,        // default true
        IReadOnlyCollection<string>? inductedCategories = null);   // default []

    public static IReadOnlyCollection<HallOfFamePointsStandingResponse> Bogus(int count, int? seed = null);
}

public static class HallOfFamePointsResponseFactory
{
    public static HallOfFamePointsResponse Create(
        IReadOnlyCollection<HallOfFamePointRuleResponse>? rules = null,
        IReadOnlyCollection<HallOfFamePointsStandingResponse>? standings = null,
        int? requiredPoints = null,              // default 36
        int? requiredSeasonsWithPoints = null,   // default 3
        int? requiredMembershipYears = null,     // default 10
        int? membershipDataStartYear = null);    // default 1999
}
```

`tests/Neba.Website.Tests/HallOfFame/HallOfFamePointsTests.cs` (new). Setup mirrors `HallOfFameTests`: bUnit context, strict `Mock<IHallOfFameApi>`, `ApiExecutor` built from a strict `IStopwatchProvider` mock, `JSInterop.Mode = Loose`. Representative tests:

```csharp
[UnitTest]
[Component("Website.HallOfFame.HallOfFamePoints")]
public sealed class HallOfFamePointsTests : IDisposable
{
    // constructor and Dispose as in HallOfFameTests; no authorization context needed (anonymous page)

    [Fact(DisplayName = "Shows an eligible bowler under the eligible heading and a bowler without the criteria under not yet eligible")]
    public void Render_ShouldSplitEligibleFromNotYetEligible()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Ray Dunleavy", totalPoints: 42, eligible: true, meetsPoints: true),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis", totalPoints: 12)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePoints>();

        // Assert
        cut.Find(".pts-stage").TextContent.ShouldContain("Ray Dunleavy");
        cut.Find(".pts-stage").TextContent.ShouldNotContain("Tom Davis");
        cut.Find(".pts-below").TextContent.ShouldContain("Tom Davis");
    }

    [Fact(DisplayName = "Shows what is open for a bowler with the points but too few seasons")]
    public void Render_ShouldShowNeedsMore_WhenPointsMetButSeasonsShort() { /* totalPoints 42, seasonsWithPoints 2, eligible false: markup contains "2 of 3 seasons" and "needs 1 more"; the dot has pts-dot--met */ }

    [Fact(DisplayName = "Says the count may be low, not needs more, when the membership history is incomplete")]
    public void Render_ShouldShowCountMayBeLow_WhenHistoryIncompleteAndUnderThreshold() { /* membershipYears 8, membershipHistoryComplete false, meetsPoints true: contains "8* of 10 years" and "count may be low", not "needs 2 more" */ }

    [Fact(DisplayName = "Shows the membership note and the Superior Performance note")]
    public void Render_ShouldShowBothNotes() { /* contains "Superior Performance only" and "count from 1999" */ }

    [Fact(DisplayName = "Shows the inducted line for a bowler in the Hall of Fame for another category")]
    public void Render_ShouldShowInductedLine_WhenInductedForOtherCategory() { /* inductedCategories ["Meritorious Service"]: contains "In the Hall of Fame for Meritorious Service" */ }

    [Fact(DisplayName = "Renders the rule groups from the response")]
    public void Render_ShouldRenderRuleGroups_FromResponse() { /* three groups, each rule's points and label */ }

    [Fact(DisplayName = "Shows an error alert when the API fails")]
    public void Render_ShouldShowError_WhenApiFails() { /* SetupFailure(500): contains "Error Loading Hall of Fame Points" */ }

    [Fact(DisplayName = "Opens the points modal for the clicked bowler")]
    public void Click_ShouldOpenModal_ForTheClickedBowler()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings: [HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis")]));
        SetupDetail(HallOfFamePointsStandingResponseFactory.ValidBowlerId);
        var cut = _ctx.Render<HallOfFamePoints>();

        // Act
        cut.Find(".pts-name").Click();

        // Assert
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(HallOfFamePointsStandingResponseFactory.ValidBowlerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // SetupList / SetupDetail / SetupFailure build StubApiResponse<T> exactly as HallOfFameTests.SetupSuccessResponse does.
}
```

`HallOfFamePointsModalTests.cs` (new, same setup): renders nothing and makes no call while closed; loading state first; breakdown rows and membership years render; "of 36 points" present with `Requirements` and absent without; the note shows only when `membershipHistoryComplete` is false; failure shows "Could not load points" and Retry calls the API again and shows the data; an inducted bowler shows the inducted flag; a bowler with no points shows "No points yet." and the guest message for no membership years.

`HallOfFameMappingExtensionsTests.cs` (edit): add

```csharp
[Fact(DisplayName = "Splits standings into eligible and not yet eligible and keeps the API order")]
public void ToViewModel_ShouldSplitStandingsByEligible() { /* three standings in, two eligible: Eligible has them in order, NotYetEligible has the rest */ }

[Fact(DisplayName = "Works out seasons needed, membership years needed, and the line position")]
public void ToViewModel_ShouldDeriveNeeds_FromRequirements() { /* 2 seasons -> 1 needed; 8 years -> 2 needed; 18 of 36 -> 50 percent; 42 of 36 -> 100 percent */ }

[Fact(DisplayName = "Builds the inducted line in display order")]
public void ToViewModel_ShouldBuildInductedLine() { /* ["Friend of NEBA", "Meritorious Service"] -> "In the Hall of Fame for Meritorious Service and Friend of NEBA"; none -> null */ }

[Fact(DisplayName = "Groups rules by group in the order sent")]
public void ToViewModel_ShouldGroupRules() { }

[Fact(DisplayName = "Maps the detail response, taking the year from each breakdown date")]
public async Task DetailToViewModel_ShouldMapAllFields() { /* await Verify(viewModel) with a seeded factory */ }
```

`HallOfFameTests.cs` (edit): replace the tests that read the point cards, if any, and add

```csharp
[Fact(DisplayName = "Links to the Hall of Fame points page from the criteria and the Superior Performance blurb")]
public void Render_ShouldLinkToPointsPage()
{
    // Arrange
    SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

    // Act
    var cut = _ctx.Render<HallOfFamePage>();

    // Assert
    cut.FindAll("a[href='/hall-of-fame/points']").Count.ShouldBe(2);
}

[Fact(DisplayName = "No longer lists the point values on this page")]
public void Render_ShouldNotShowPointCards()
{
    // Arrange
    SetupSuccessResponse([]);

    // Act
    var cut = _ctx.Render<HallOfFamePage>();

    // Assert
    cut.Markup.ShouldNotContain("Points Leader/Invitational Title");
}
```

`tests/e2e/HallOfFamePoints.spec.ts` (new)

```ts
import { test, expect } from '@playwright/test';

test.describe('Hall of Fame points page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('is reachable from the Hall of Fame page', async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
    await page.getByRole('link', { name: /See who is eligible and who is closest/ }).click();
    await expect(page).toHaveURL(/\/hall-of-fame\/points$/);
    await expect(page.locator('h1')).toContainText('Hall of Fame Points');
  });

  test.describe('list', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/hall-of-fame/points');
      await page.waitForSelector('.pts-qrow');
    });

    test('puts the eligible bowler on the stage and the others below', async ({ page }) => {
      await expect(page.locator('.pts-stage .pts-name', { hasText: 'Ray Dunleavy' })).toBeVisible();
      await expect(page.locator('.pts-below .pts-name', { hasText: 'Ken Almeida' })).toBeVisible();
      await expect(page.locator('.pts-below .pts-row', { hasText: 'Ken Almeida' })).toContainText('needs 1 more');
    });

    test('shows the rules from the API', async ({ page }) => {
      await expect(page.locator('.pts-rule', { hasText: 'Bowler of the Year' })).toContainText('12');
    });

    test('filters by name without a round trip and says when nothing matches', async ({ page }) => {
      await page.locator('[data-pts-search]').pressSequentially('jose');
      await expect(page.locator('.pts-name', { hasText: 'José Marchetti' })).toBeVisible();
      await expect(page.locator('.pts-name', { hasText: 'Ken Almeida' })).toBeHidden();
      await expect(page.locator('.pts-status')).toHaveText('Showing 1 of 4');

      await page.locator('[data-pts-search]').fill('zzz');
      await expect(page.locator('.pts-status')).toHaveText('No bowlers match your search.');
    });

    test('opens the detail modal with the breakdown and membership years', async ({ page }) => {
      await page.locator('.pts-name', { hasText: 'Ken Almeida' }).click();
      const dialog = page.getByRole('dialog');
      await expect(dialog.getByRole('heading', { name: 'Ken Almeida' })).toBeVisible();
      await expect(dialog.getByText('Points earned')).toBeVisible();
      await expect(dialog.getByText('Membership years')).toBeVisible();
    });

    test('shows the membership note in the modal for an incomplete history', async ({ page }) => {
      await page.locator('.pts-name', { hasText: 'Walt Brennan' }).click();
      await expect(page.getByRole('dialog')).toContainText("first membership is not on file");
    });
  });

  test('stacks the rows on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 });
    await page.goto('/hall-of-fame/points');
    await page.waitForSelector('.pts-row');
    await expect(page.locator('.pts-axis')).toBeHidden();
  });
});
```

### Deferred / out of scope

- The Hall of Fame name-click stats modal (`HallOfFamePointsModal` is built to be reusable for it).
- Sorting controls and filters beyond name search (order is points high to low, then name).
- A help doc under `docs/help/`; confirm whether a read-only public page needs one before the PR.
- Jump-to-anchor for `#hof-points-rules` from outside the page: the rules render after the data loads, so a cold link with the anchor will not scroll. The in-page link works.

## Commit plan

Branch `hallOfFamePoints`. Thirteen commits, in this order. Each one builds and passes its own tests; code and its tests travel together. Stage only the files named, never `git add -A`. Every message ends with:

```text
Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
```

If the work drifts from this list, change the list; do not merge commits.

1. **Add Hall of Fame points plan and mockup**
   - `docs/plans/hall-of-fame-points.md`, `docs/plans/mockups/hall-of-fame-points/track.html`
   - Body: records the design and the confirmed mockup before any code, so the later commits can point at it.

2. **Add CountsTowardHallOfFame to MembershipType**
   - `src/Neba.Api.Contracts/Memberships/MembershipType.cs`, the existing `MembershipType` tests (one case per type)
   - Body: the Hall of Fame membership-years rule counts only yearly types; the flag keeps that data with the type, next to `QualifiesAsPriorMembership`.

3. **Add Hall of Fame point rules**
   - `src/Neba.Api/Features/HallOfFame/Domain/HallOfFamePointRule.cs`, `HallOfFamePointRules.cs`, `tests/Neba.Api.Tests/Features/HallOfFame/Domain/HallOfFamePointRulesTests.cs`, `docs/ubiquitous-language.md`
   - Body: one table for the point values and the 36/3/10 thresholds, with a test that a new tournament type or award category cannot go unmapped. The language doc gains Hall of Fame Points, Superior Performance criteria and Eligible, and the old "point values deferred" note (line 1282) is updated.

4. **Add Hall of Fame points calculator**
   - `src/Neba.Api/Features/HallOfFame/HallOfFamePointsCalculator.cs`, `src/Neba.Api/ApplicationConfiguration.cs` (calculator registration), `tests/Neba.TestFactory/HallOfFame/TitleWinFactory.cs`, `AwardWinFactory.cs`, `MembershipRecordFactory.cs`, `tests/Neba.Api.Tests/Features/HallOfFame/HallOfFamePointsCalculatorTests.cs`
   - Body: pure points and membership-year math, including the New-record rule for an incomplete history, so it tests without a database.

5. **Add Hall of Fame points reader**
   - `src/Neba.Api/Features/HallOfFame/HallOfFamePointsReader.cs`, `src/Neba.Api/ApplicationConfiguration.cs` (reader registration), `tests/Neba.Api.Tests/Features/HallOfFame/HallOfFamePointsReaderTests.cs`
   - Body: one place loads titles from both sources, awards, memberships and inductions; the integration test also proves the `Contains` on strongly typed ids translates.

6. **Add Hall of Fame points list endpoint**
   - `src/Neba.Api/Caching/CacheDescriptors.cs` (`PointsTag`, `ListPoints`) and its descriptor tests, `src/Neba.Api/Features/HallOfFame/ListHallOfFamePoints/` (query, handler, DTOs, endpoint, summary), `src/Neba.Api.Contracts/HallOfFame/ListHallOfFamePoints/`, `src/Neba.Api.Contracts/HallOfFame/IHallOfFameApi.cs` (list method), `tests/Neba.TestFactory/HallOfFame/HallOfFamePointsStandingResponseFactory.cs`, `HallOfFamePointsResponseFactory.cs`, `HallOfFamePointsDtoFactory.cs`, handler and endpoint tests
   - Body: the public list with the rule table and thresholds, cached 14 days, with `Eligible` decided in the API and no rank.

7. **Add Hall of Fame points detail endpoint**
   - `src/Neba.Api/Caching/CacheDescriptors.cs` (`PointsDetail`) and its tests, `src/Neba.Api/Features/HallOfFame/GetHallOfFamePointsDetail/` (query, handler, DTO, request, validator, endpoint, summary), `src/Neba.Api.Contracts/HallOfFame/GetHallOfFamePointsDetail/`, `IHallOfFameApi.cs` (detail method), `tests/Neba.TestFactory/HallOfFame/HallOfFamePointsDetailResponseFactory.cs`, `HallOfFamePointsDetailDtoFactory.cs`, handler and endpoint tests
   - Body: one bowler's breakdown and membership years, for inducted bowlers too, so a later name-click modal can use it.

8. **Evict Hall of Fame points cache when results, awards or inductions change**
   - `src/Neba.Api/Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs`, the Bowler of the Year `Assign*` jobs in `src/Neba.Api/Legacy/Seasons/Complete/` that add point-earning awards (not Youth, high block or high average), `src/Neba.Api/Legacy/HallOfFame/HallOfFame.cs`, and each job's tests
   - Body: the 14-day expiry is a floor; these are the events that change the numbers.

9. **Add Hall of Fame points view models and mapping**
   - `src/Neba.Website.Server/HallOfFame/HallOfFamePointsViewModel.cs` (and the other records), `HallOfFameMappingExtensions.cs`, `tests/Neba.Website.Tests/HallOfFame/HallOfFameMappingExtensionsTests.cs` (plus the new Verify snapshot)
   - Body: splits eligible from not yet eligible and works out "needs N more" and the line position once, outside the markup.

10. **Add Hall of Fame points modal**
    - `src/Neba.Website.Server/HallOfFame/HallOfFamePointsModal.razor`, `HallOfFamePointsModal.razor.css`, `tests/Neba.Website.Tests/HallOfFame/HallOfFamePointsModalTests.cs`
    - Body: depends only on a bowler id and name, so the points page and a later Hall of Fame name-click modal share it.

11. **Add Hall of Fame points page**
    - `src/Neba.Website.Server/HallOfFame/HallOfFamePoints.razor`, `HallOfFamePoints.razor.css`, `HallOfFamePoints.razor.js`, `HallOfFamePoints.razor.tests.js`, `tests/Neba.Website.Tests/HallOfFame/HallOfFamePointsTests.cs`
    - Body: the eligible stage, the 0 to 36 line, search in JS and the rules from the API; reachable by URL before it is linked.

12. **Link the Hall of Fame page to the points page**
    - `src/Neba.Website.Server/HallOfFame/HallOfFame.razor`, `src/Neba.Website.Server/wwwroot/sitemap.xml`, `tests/Neba.Website.Tests/HallOfFame/HallOfFameTests.cs`
    - Body: the point values now live in one place, the API, so the Hall of Fame page links to them and drops its hard-coded copy.

13. **Add Playwright coverage for Hall of Fame points**
    - `tests/e2e/mock-api/mock-api-server.ts`, `tests/e2e/HallOfFamePoints.spec.ts`
    - Body: the link, the split, search, the modal and the phone layout need a real browser and HTTP, and the mock needs every required response field.

Not in a commit: the name fixes for the duplicate bowler records (data cleanup), a help doc (decide before the PR), and the release `<Version>` bump in `Directory.Build.props` (set when the PR targets `main` or a release branch).
