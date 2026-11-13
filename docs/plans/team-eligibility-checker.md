# Team Eligibility Checker

A public page where bowlers pick the members of a doubles, trios, or baker team and learn whether the team may enter a given tournament under Rules 401–403. Also adds a "previous tournament" link on each tournament, so the site can show defending champions on the tournament list and detail pages and exempt a defending team from the champion rules.

## Decisions locked in during scoping

- **Not critical in the money sense, but NFRs were run** (public, reads date of birth).
- **Pro titles:** no PBA/PWBA data. A team that passes on NEBA titles, age, and Hall of Fame gets "eligible" with a note that pro titles were not checked. A fail on NEBA data is a hard no.
- **Age date:** age and champion windows are checked at the tournament's start date and end date.
- **Rules run in a fixed order until a hard stop.** Order: defending champions, then champion and Hall of Fame composition, then age. The first failing group stops the run and its reasons are returned. A team that fails composition is `Ineligible` regardless of age, so the date question never comes up for it.
- **Combining the two dates:** eligible at both → `Eligible`; ineligible at both → `Ineligible` (reasons from both dates, duplicates removed); eligible at one and ineligible at the other → `DependsOnDate`, with the per-date results and an explanation. `DependsOnDate` only arises for a team that is otherwise eligible.
- **Date of birth never leaves the API.** A missing DOB in an over/under tournament makes the team ineligible. The reason text is the same whichever member lacks a DOB. Two bowlers in the same age group (both over or both under) get one reason that does not say which group. The wording of the age reasons gets a review with you before it is final (open item).
- **Missing DOB outside over/under events** (regular doubles, trios, baker): the bowler is treated as not over 60, so the champion window is the longer 10 years in regular doubles. Staff fix a wrong result by adding the DOB; a recheck then passes. No reason text mentions DOB.
- **Bowlers with no title data** count as non-champions for NEBA titles. The pro-title note still applies.
- **Bowlers not in the system:** the request carries `BowlerIds` (found bowlers) plus `UnknownBowlerCount`. Found plus unknown must equal the team size. An unknown bowler has no NEBA titles, no Hall of Fame record, and no DOB, so the evaluator treats them as a non-champion; in an over/under event an unknown bowler makes the team ineligible, because their age cannot be verified (same reason as a missing DOB). Other members' facts still count, so a baker team with two Hall of Fame Superior Performance members and three unknown bowlers is ineligible. A team with an unknown bowler is never the defending team, so the defending check is skipped. The page offers a "bowler not found in system" control (mockup options at Phase 2).
- **Defending champions:** `Tournament.PreviousTournamentId` (nullable, same tournament type, previous must end before this one starts). If a previous tournament exists and the whole team equals its place-1 bowlers, the check returns 200 with outcome `DefendingChampions` and runs no other rule. Multiple place-1 rows on a tournament are the winning team (no ties for first have happened). No previous tournament means no exemption.
- **Truncated previous tournament (`TournamentStatus.Truncated`):** the winners of the truncated tournament, and the winners of its own previous tournament, may each bowl together as a defending team. Example: the 2026 doubles was truncated, so the team awarded first place in 2026 and the 2025 winners (who never got to defend) can each bowl together in 2027. The chain continues while each tournament reached is also truncated, and stops at the first one that is not. Each defending team is checked on its own: the whole team must equal one of them. Winners are place-1 results whether or not the tournament was title-eligible, so a truncated tournament's first place counts.
  - *Possible later change, deliberately out of scope:* one could argue the 2025 winners had to make match play in 2026 to count as having failed to defend, and so should not be exempt in 2027 if they missed it. We do not model that. Revisit it if the case ever arises.
- **Over 60** means the bowler reached their 60th birthday by the as-of date (the 60th birthday counts).
- **USBC ID source:** the `UsbcId` column of the legacy `dbo.Bowlers` table, read by the three legacy bowler jobs (new, update, weekly sync). The one-time backfill of existing bowlers is your LINQPad script at the 1.2.0 release.
- **Non-Champions events cannot have a defending champion.** The domain rejects a previous tournament on a `NonChampions` tournament (`Error.Validation`). The picker is hidden for that type in Phase 2, and the list and detail pages show no defending champions for it.
- **Defending champions are also shown** on the tournament list and detail pages for every format (singles, doubles, trios, baker), next to the existing `Winners`. They come as a list of defending teams, each with the tournament it won, so a truncated chain shows two teams without mixing their names (the list is empty when there is no previous tournament or no results yet).
- **Reference shape:** ID only, self-referencing FK, no navigation property, on delete set null. The edit handler loads the previous tournament's type and end date and passes them to the aggregate.
- **Permissions:** no new policy, permission, or role. Check and search endpoints are `AllowAnonymous()`. Setting the previous tournament rides on `Permission:Tournaments.EditTournament` (create: `Tournaments.CreateTournament`).
- **Rate limiting (corrects an earlier note):** the API already has a per-IP public policy (100 per minute, from `BaseEndpointGroup`), so both new endpoints inherit it. Search also gets a tighter policy of its own.
- **Search is split:** this feature ships a public bowler search. A later authenticated registration search gets its own use case, DTO, and policy. The public result is display name, bowler ID, and full USBC ID (you judged this fine, since looking one up requires USBC membership). `Bowler` gets a nullable `UsbcId`; a bowler without one shows name only.
- **Check route is `GET`** with bowler ids in the query string. Bowler ids are not sensitive.
- **Rules display:** the page shows the team composition criteria and the champion definition inline, not in a slideover: Doubles and Over/Under 50 Doubles show 401a and 401b; Trios show 402a and 402b; Baker shows 403a and 402b (403a says its definitions follow 402b).
- **Defending champions show only while the tournament is not finished.** The January 2027 tournament shows the January 2026 champions; once it completes, its page and card show the 2027 winners instead. The UI shows the defending line for scheduled tournaments only; completed, truncated, and cancelled tournaments do not show it. The API still returns the field for every tournament.
- **Previous-tournament picker** lists same-type tournaments from the three seasons before, labelled with the month and year after the name, such as "Joe's Pizza Open (02/2026)", so two years of the same event are easy to tell apart.

## Phase 1: API

Conventions followed: `ICachedQuery` pattern for the two cached tournament reads; new use-case folders hold Endpoint, Summary, Validator, Query, Handler; request types live beside the endpoint (as `GetBowlerTitlesRequest` does) and responses in `Neba.Api.Contracts`; new contract response properties default to empty rather than `required`, so the e2e mock server keeps working.

### Domain

`Features/Tournaments/Domain/PreviousTournamentFacts.cs` (new)

```csharp
namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>The facts about a previous tournament that <see cref="Tournament"/> needs to accept it as its previous tournament.</summary>
internal readonly record struct PreviousTournamentFacts(TournamentId Id, TournamentType TournamentType, DateOnly EndDate);
```

`Features/Tournaments/Domain/Tournament.cs` (edit)

```csharp
/// <summary>Gets the tournament this one follows in the prior year, if any; its winners are this one's defending champions.</summary>
public TournamentId? PreviousTournamentId { get; private set; }

public static ErrorOr<Tournament> Create(
    /* existing parameters ... */
    decimal nebaAddedMoney = 0,
    PreviousTournamentFacts? previousTournament = null,   // new, before id
    TournamentId? id = null,
    int? legacyId = null)
{
    /* existing checks ... */
    var previousError = ValidatePreviousTournament(null, tournamentType, startDate, previousTournament);
    if (previousError is { } error) return error;
    /* existing initializer, plus: */
    //   PreviousTournamentId = previousTournament?.Id,
}

public ErrorOr<Updated> Update(
    /* existing parameters ... */
    DateTimeOffset? oilPatternRevealDateTime,
    PreviousTournamentFacts? previousTournament)           // new
{
    /* existing checks ... */
    var previousError = ValidatePreviousTournament(Id, tournamentType, startDate, previousTournament);
    if (previousError is { } error) return error;
    /* existing assignments, plus: */
    PreviousTournamentId = previousTournament?.Id;
    return Result.Updated;
}

private static Error? ValidatePreviousTournament(
    TournamentId? selfId, TournamentType tournamentType, DateOnly startDate, PreviousTournamentFacts? previous)
{
    if (previous is not { } facts) return null;

    if (tournamentType == TournamentType.NonChampions)
        return TournamentErrors.PreviousTournamentNotAllowed(tournamentType);
    if (selfId is { } id && facts.Id == id)
        return TournamentErrors.PreviousTournamentIsSelf;
    if (facts.TournamentType != tournamentType)
        return TournamentErrors.PreviousTournamentTypeMismatch(facts.TournamentType, tournamentType);
    if (facts.EndDate >= startDate)
        return TournamentErrors.PreviousTournamentNotBefore(facts.EndDate, startDate);

    return null;
}
```

`Features/Tournaments/Domain/TournamentErrors.cs` (edit; same style as existing members)

```csharp
public static Error PreviousTournamentNotAllowed(TournamentType type)
    => Error.Validation("Tournament.PreviousTournament.NotAllowed",
        $"A {type.Name} tournament cannot have a previous tournament.");

public static readonly Error PreviousTournamentIsSelf =
    Error.Validation("Tournament.PreviousTournament.IsSelf", "A tournament cannot be its own previous tournament.");

public static Error PreviousTournamentTypeMismatch(TournamentType previous, TournamentType current)
    => Error.Validation("Tournament.PreviousTournament.TypeMismatch",
        $"The previous tournament is a {previous.Name} tournament; it must be a {current.Name} tournament.");

public static Error PreviousTournamentNotBefore(DateOnly previousEnd, DateOnly start)
    => Error.Validation("Tournament.PreviousTournament.NotBefore",
        $"The previous tournament ends on {previousEnd}; it must end before this tournament starts on {start}.");

public static Error PreviousTournamentNotFound(TournamentId id)
    => Error.Validation("Tournament.PreviousTournament.NotFound", "The previous tournament was not found.");
    // Match the error type BowlingCenterNotFound uses for a missing referenced id.
```

`Features/Bowlers/Domain/Bowler.cs` (edit)

```csharp
/// <summary>Gets the bowler's USBC membership ID; null until known.</summary>
public string? UsbcId { get; internal set; }
```

Add the term to `docs/ubiquitous-language.md` (USBC ID: bowler's USBC membership identifier, distinct from the bowling center's certification number).

`Features/Tournaments/Domain/TeamEligibility*.cs` (new; the rules)

```csharp
internal sealed record TeamMemberFacts
{
    /// <summary>Null for a bowler who is not in the system.</summary>
    public BowlerId? BowlerId { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public IReadOnlyCollection<TitleWin> Titles { get; init; } = [];
    public bool HallOfFameSuperiorPerformance { get; init; }
}

/// <summary>A NEBA title, with the dates of the tournament it was won in.</summary>
internal readonly record struct TitleWin(DateOnly StartDate, DateOnly EndDate);

internal readonly record struct TeamEligibilityContext(TournamentType TournamentType, DateOnly StartDate, DateOnly EndDate);

internal sealed class TeamEligibilityOutcome : SmartEnum<TeamEligibilityOutcome>
{
    public static readonly TeamEligibilityOutcome DefendingChampions = new(nameof(DefendingChampions), 1);
    public static readonly TeamEligibilityOutcome Eligible = new(nameof(Eligible), 2);
    public static readonly TeamEligibilityOutcome Ineligible = new(nameof(Ineligible), 3);
    public static readonly TeamEligibilityOutcome DependsOnDate = new(nameof(DependsOnDate), 4);
    private TeamEligibilityOutcome(string name, int value) : base(name, value) { }
}

internal readonly record struct TeamEligibilityReason(string Code, string Message);

/// <summary>All reason text lives here, so the wording review touches one file. Age reasons never name a bowler or give a DOB.</summary>
internal static class TeamEligibilityReasons
{
    // Wording below is a placeholder pending the review you asked for.
    // Covers a bowler with no DOB and a bowler not found in the system. Never says which bowler or why.
    public static readonly TeamEligibilityReason AgeNotVerified =
        new("Age.NotVerified", "We could not verify the age of every bowler. An over/under team needs every bowler found in our system with an age we can check.");
    // Does not say whether both bowlers are over or both are under.
    public static readonly TeamEligibilityReason SameAgeGroup =
        new("Age.SameAgeGroup", "An over/under team needs one bowler in each age group. Both bowlers on this team are in the same group.");
    public static TeamEligibilityReason TooManyChampions(int max) =>
        new("Champions.TooMany", $"A team may have at most {max} previous champion{(max == 1 ? string.Empty : "s")}.");
    public static readonly TeamEligibilityReason NoNonChampion =
        new("Champions.NoNonChampion", "A team needs at least one non-champion.");
    public static TeamEligibilityReason TooManyMultiTimeChampions(int max) =>
        new("Champions.TooManyMultiTime", $"A team may have at most {max} multi-time champion{(max == 1 ? string.Empty : "s")}.");
    public static readonly TeamEligibilityReason TooManyHallOfFame =
        new("HallOfFame.TooMany", "A team may have at most one Hall of Fame member for Superior Performance.");
}

internal sealed record TeamEligibilityCheck(DateOnly AsOf, IReadOnlyList<TeamEligibilityReason> Reasons)
{
    public bool Eligible => Reasons.Count == 0;
}

internal sealed record TeamEligibilityResult(
    TeamEligibilityOutcome Outcome,
    IReadOnlyList<TeamEligibilityReason> Reasons,
    IReadOnlyList<TeamEligibilityCheck> DateChecks);
```

`Features/Tournaments/Domain/TeamEligibilityEvaluator.cs` (new)

```csharp
internal static class TeamEligibilityEvaluator
{
    private const int OverUnderAgeLimit = 50;
    private const int SeniorChampionAge = 60;

    /// <summary>Runs the rules at the start date and the end date and combines the two answers.</summary>
    public static TeamEligibilityResult Evaluate(TeamEligibilityContext context, IReadOnlyCollection<TeamMemberFacts> members)
    {
        var atStart = EvaluateAt(context, context.StartDate, members);
        if (context.StartDate == context.EndDate)
            return ToResult(atStart);

        var atEnd = EvaluateAt(context, context.EndDate, members);

        return (atStart.Eligible, atEnd.Eligible) switch
        {
            (true, true) => new(TeamEligibilityOutcome.Eligible, [], []),
            (false, false) => new(TeamEligibilityOutcome.Ineligible,
                [.. atStart.Reasons.Concat(atEnd.Reasons).DistinctBy(reason => reason.Code)], []),
            _ => new(TeamEligibilityOutcome.DependsOnDate, [], [atStart, atEnd]),
        };
    }

    private static TeamEligibilityResult ToResult(TeamEligibilityCheck check)
        => check.Eligible
            ? new(TeamEligibilityOutcome.Eligible, [], [])
            : new(TeamEligibilityOutcome.Ineligible, check.Reasons, []);

    /// <summary>Fixed order, stopping at the first failing group: composition, then age.</summary>
    internal static TeamEligibilityCheck EvaluateAt(
        TeamEligibilityContext context, DateOnly asOf, IReadOnlyCollection<TeamMemberFacts> members)
    {
        var composition = CheckComposition(context, asOf, members);
        if (composition.Count > 0) return new(asOf, composition);

        var age = IsOverUnder(context.TournamentType) ? CheckAge(asOf, members) : [];
        return new(asOf, age);
    }

    private static List<TeamEligibilityReason> CheckComposition(
        TeamEligibilityContext context, DateOnly asOf, IReadOnlyCollection<TeamMemberFacts> members)
    {
        var titleCounts = members
            .Select(member => CountTitlesInWindow(context, asOf, member))
            .ToList();
        var champions = titleCounts.Count(count => count >= 1);
        var multiTime = titleCounts.Count(count => count > 1);
        var nonChampions = titleCounts.Count(count => count == 0);
        var reasons = new List<TeamEligibilityReason>();

        if (context.TournamentType == TournamentType.Doubles || IsOverUnder(context.TournamentType))
        {
            if (champions > 1) reasons.Add(TeamEligibilityReasons.TooManyChampions(1));
        }
        else if (context.TournamentType == TournamentType.Trios)
        {
            if (nonChampions < 1) reasons.Add(TeamEligibilityReasons.NoNonChampion);
            if (multiTime > 1) reasons.Add(TeamEligibilityReasons.TooManyMultiTimeChampions(1));
        }
        else if (context.TournamentType == TournamentType.Baker)
        {
            if (nonChampions < 1) reasons.Add(TeamEligibilityReasons.NoNonChampion);
            if (multiTime > 2) reasons.Add(TeamEligibilityReasons.TooManyMultiTimeChampions(2));
            if (members.Count(member => member.HallOfFameSuperiorPerformance) > 1)
                reasons.Add(TeamEligibilityReasons.TooManyHallOfFame);
        }

        return reasons;
    }

    // Titles won in tournaments that overlap this tournament's dates do not count (Rule 401a, same
    // weekend); neither does this tournament's own title. Titles after the as-of date never count.
    private static int CountTitlesInWindow(TeamEligibilityContext context, DateOnly asOf, TeamMemberFacts member)
    {
        var windowStart = asOf.AddYears(-WindowYears(context.TournamentType, asOf, member.DateOfBirth));

        return member.Titles.Count(title =>
            title.EndDate >= windowStart
            && title.EndDate <= asOf
            && !(title.StartDate <= context.EndDate && title.EndDate >= context.StartDate));
    }

    // Regular doubles: 10 years, or 5 for a bowler over 60. Everything else: 5 years.
    // A missing DOB means "not over 60", so the longer window applies.
    private static int WindowYears(TournamentType type, DateOnly asOf, DateOnly? dateOfBirth)
        => type == TournamentType.Doubles && !IsOver(dateOfBirth, asOf, SeniorChampionAge) ? 10 : 5;

    private static List<TeamEligibilityReason> CheckAge(DateOnly asOf, IReadOnlyCollection<TeamMemberFacts> members)
    {
        if (members.Any(member => member.DateOfBirth is null))
            return [TeamEligibilityReasons.AgeNotVerified];

        var overs = members.Count(member => IsOver(member.DateOfBirth, asOf, OverUnderAgeLimit));
        var unders = members.Count - overs;

        return overs == 1 && unders == 1
            ? []
            : [TeamEligibilityReasons.SameAgeGroup];
    }

    /// <summary>True when the bowler has reached <paramref name="age"/> by <paramref name="asOf"/> ("age or older").</summary>
    private static bool IsOver(DateOnly? dateOfBirth, DateOnly asOf, int age)
        => dateOfBirth is { } dob && dob.AddYears(age) <= asOf;

    private static bool IsOverUnder(TournamentType type)
        => type == TournamentType.OverUnderFiftyDoubles || type == TournamentType.OverUnderFortyDoubles;
}
```

Notes: "Over 60" means the bowler has reached their 60th birthday by the as-of date (`dob.AddYears(60) <= asOf`), as you confirmed. The age limit for over/under is 50 in the code above; `OverUnderFortyDoubles` is inactive, so the draft lists it only to keep the type check total. Either drop it from `IsOverUnder` or give it a limit of 40 during implementation.

### Application

`Features/Tournaments/DefendingTeamsReader.cs` (new, shared by the check and the two cached reads)

```csharp
internal readonly record struct TournamentWinner(BowlerId BowlerId, Name Name);

/// <summary>
/// One team that may bowl together as defending champions of a tournament.
/// <paramref name="SourceTruncated"/> is true when the tournament it won was truncated;
/// <paramref name="Undefended"/> is true when it won an earlier tournament and never got to defend,
/// because a later tournament in the chain was truncated.
/// </summary>
internal sealed record DefendingTeam(
    TournamentId SourceTournamentId,
    string SourceTournamentName,
    string SeasonDescription,
    bool SourceTruncated,
    bool Undefended,
    IReadOnlyList<TournamentWinner> Champions);

/// <summary>
/// Loads the defending teams of the given tournaments. A tournament's first defending team is the winners
/// of its previous tournament. While the tournament just reached is truncated, the winners of its own
/// previous tournament are added too. Dates fall strictly (a previous tournament must end before the next
/// one starts), so the walk cannot loop.
/// </summary>
internal sealed class DefendingTeamsReader(AppDbContext appDbContext)
{
    private sealed record SourceRow(
        TournamentId Id, string Name, string SeasonDescription, TournamentStatus Status, TournamentId? PreviousTournamentId);

    public async Task<IReadOnlyDictionary<TournamentId, IReadOnlyList<DefendingTeam>>> LoadAsync(
        IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<TournamentId, IReadOnlyList<DefendingTeam>>();
        if (tournamentIds.Count == 0) return result;

        // The tournament just reached, per owner (the tournament whose defenders we want).
        var frontier = (await appDbContext.Tournaments.AsNoTracking()
                .Where(t => tournamentIds.Contains(t.Id) && t.PreviousTournamentId != null)
                .Select(t => new { t.Id, t.PreviousTournamentId })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.Id, row => row.PreviousTournamentId);

        var sourcesByOwner = new Dictionary<TournamentId, List<SourceRow>>();

        while (frontier.Count > 0)
        {
            var sourceIds = frontier.Values.OfType<TournamentId>().Distinct().ToList();
            var sources = (await appDbContext.Tournaments.AsNoTracking()
                    .Where(t => sourceIds.Contains(t.Id))
                    .Select(t => new SourceRow(t.Id, t.Name, t.Season.Description, t.Status, t.PreviousTournamentId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(row => row.Id);

            var next = new Dictionary<TournamentId, TournamentId?>();

            foreach (var (ownerId, sourceId) in frontier)
            {
                if (sourceId is not { } id || !sources.TryGetValue(id, out var source)) continue;

                if (!sourcesByOwner.TryGetValue(ownerId, out var list))
                    sourcesByOwner[ownerId] = list = [];
                list.Add(source);

                // A truncated tournament leaves its own previous tournament's winners undefended.
                if (source.Status == TournamentStatus.Truncated && source.PreviousTournamentId is { } previousId)
                    next[ownerId] = previousId;
            }

            frontier = next;
        }

        var allSourceIds = sourcesByOwner.Values.SelectMany(list => list.Select(s => s.Id)).Distinct().ToList();
        var winners = await LoadWinnersAsync(allSourceIds, cancellationToken);

        foreach (var (ownerId, sources) in sourcesByOwner)
        {
            result[ownerId] = [.. sources
                .Select((source, index) => (source, index))
                .Where(item => winners.ContainsKey(item.source.Id))
                .Select(item => new DefendingTeam(
                    item.source.Id,
                    item.source.Name,
                    item.source.SeasonDescription,
                    item.source.Status == TournamentStatus.Truncated,
                    item.index > 0,
                    winners[item.source.Id]))];
        }

        return result;
    }

    // Place-1 bowlers from historical champions and recorded results; title eligibility is not required.
    private async Task<Dictionary<TournamentId, IReadOnlyList<TournamentWinner>>> LoadWinnersAsync(
        IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0) return [];

        var historical = await appDbContext.HistoricalTournamentChampions.AsNoTracking()
            .Where(champion => tournamentIds.Contains(champion.Tournament.Id))
            .Select(champion => new { TournamentId = champion.Tournament.Id, BowlerId = champion.Bowler.Id, champion.Bowler.Name })
            .ToListAsync(cancellationToken);

        var recorded = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1)
                .Select(result => new { TournamentId = tournament.Id, BowlerId = result.Bowler.Id, result.Bowler.Name }))
            .ToListAsync(cancellationToken);

        return historical.Concat(recorded)
            .GroupBy(row => row.TournamentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TournamentWinner>)[.. group.Select(row => new TournamentWinner(row.BowlerId, row.Name))]);
    }
}
```

The reader runs one small batch of queries per level of the chain (usually one, two when the previous tournament was truncated), for all tournaments at once, so the season list stays at a handful of queries.

`Features/Bowlers/BowlerTitlesReader.cs` (new; same two sources as `GetBowlerTitlesQueryHandler`, for many bowlers)

```csharp
internal sealed class BowlerTitlesReader(AppDbContext appDbContext)
{
    public async Task<ILookup<BowlerId, TitleWin>> LoadAsync(
        IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken)
    {
        var historical = await appDbContext.HistoricalTournamentChampions.AsNoTracking()
            .Where(champion => bowlerIds.Contains(champion.Bowler.Id))
            .Select(champion => new { BowlerId = champion.Bowler.Id, champion.Tournament.StartDate, champion.Tournament.EndDate })
            .ToListAsync(cancellationToken);

        // Same filter as GetBowlerTitles: only title-eligible tournaments count.
        var recorded = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.TitleEligible)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1 && bowlerIds.Contains(result.Bowler.Id))
                .Select(result => new { BowlerId = result.Bowler.Id, tournament.StartDate, tournament.EndDate }))
            .ToListAsync(cancellationToken);

        return historical.Concat(recorded)
            .ToLookup(row => row.BowlerId, row => new TitleWin(row.StartDate, row.EndDate));
    }
}
```

Optional separate commit: move `GetBowlerTitlesQueryHandler` onto this reader. The two readers register as scoped services in `TournamentsConfiguration` and `BowlersConfiguration` (or wherever those features register services).

`Features/Tournaments/CheckTeamEligibility/` (new use case)

```csharp
// CheckTeamEligibilityRequest.cs
internal sealed record CheckTeamEligibilityRequest
{
    public required string Id { get; init; }
    public string[] BowlerIds { get; init; } = [];
    public int UnknownBowlerCount { get; init; }
}

// CheckTeamEligibilityRequestValidator.cs
internal sealed class CheckTeamEligibilityRequestValidator : Validator<CheckTeamEligibilityRequest>
{
    public CheckTeamEligibilityRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("CheckTeamEligibilityRequest.IdRequired").WithMessage("Tournament ID is required.")
            .Length(26).WithErrorCode("CheckTeamEligibilityRequest.IdInvalidLength").WithMessage("Tournament ID must be a 26-character ULID.");

        RuleForEach(r => r.BowlerIds)
            .Length(26).WithErrorCode("CheckTeamEligibilityRequest.BowlerIdInvalidLength").WithMessage("Bowler IDs must be 26-character ULIDs.");

        RuleFor(r => r.BowlerIds)
            .Must(ids => ids.Distinct(StringComparer.Ordinal).Count() == ids.Length)
            .WithErrorCode("CheckTeamEligibilityRequest.BowlerIdsDuplicate").WithMessage("A bowler can appear only once on a team.");

        RuleFor(r => r.UnknownBowlerCount)
            .InclusiveBetween(0, 5).WithErrorCode("CheckTeamEligibilityRequest.UnknownBowlerCountInvalid")
            .WithMessage("Unknown bowler count must be between 0 and 5.");
    }
}

// CheckTeamEligibilityQuery.cs  (not cached: keyed per team, low volume)
internal sealed record CheckTeamEligibilityQuery : IQuery<ErrorOr<TeamEligibilityDto>>   // use the marker interface the other uncached queries use
{
    public required TournamentId TournamentId { get; init; }
    public required IReadOnlyCollection<BowlerId> BowlerIds { get; init; }
    public required int UnknownBowlerCount { get; init; }
}

// TeamEligibilityDto.cs
internal sealed record TeamEligibilityDto(
    TeamEligibilityOutcome Outcome,
    IReadOnlyList<TeamEligibilityReason> Reasons,
    IReadOnlyList<TeamEligibilityCheck> DateChecks,
    DefendingTeam? DefendingTeam = null);   // set only when Outcome is DefendingChampions

// TeamEligibilityErrors.cs
internal static class TeamEligibilityErrors
{
    public static Error NotATeamTournament(TournamentId id)
        => Error.Validation("TeamEligibility.NotATeamTournament", "This tournament is not a team event.");
    public static Error TeamSizeMismatch(int expected)
        => Error.Validation("TeamEligibility.TeamSizeMismatch", $"This tournament needs exactly {expected} bowlers.");
}

// CheckTeamEligibilityQueryHandler.cs
internal sealed class CheckTeamEligibilityQueryHandler(
    AppDbContext appDbContext,
    DefendingTeamsReader defendingTeamsReader,
    BowlerTitlesReader titlesReader)
    : IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>
{
    public async Task<ErrorOr<TeamEligibilityDto>> HandleAsync(CheckTeamEligibilityQuery query, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments.AsNoTracking()
            .Where(t => t.Id == query.TournamentId)
            .Select(t => new { t.TournamentType, t.StartDate, t.EndDate, t.PreviousTournamentId })
            .SingleOrDefaultAsync(cancellationToken);

        if (tournament is null) return TournamentErrors.TournamentNotFound(query.TournamentId);

        var teamSize = tournament.TournamentType.TeamSize;
        if (teamSize < 2) return TeamEligibilityErrors.NotATeamTournament(query.TournamentId);
        if (query.BowlerIds.Count + query.UnknownBowlerCount != teamSize) return TeamEligibilityErrors.TeamSizeMismatch(teamSize);

        // 1. Defending champions: the whole team must equal one defending team (the previous tournament's
        //    winners, plus the winners before it while the chain is truncated). Skipped when any bowler
        //    is unknown, since an unknown bowler cannot be a past winner.
        if (query.UnknownBowlerCount == 0 && tournament.PreviousTournamentId is not null)
        {
            var defending = await defendingTeamsReader.LoadAsync([query.TournamentId], cancellationToken);

            var matched = defending.GetValueOrDefault(query.TournamentId, [])
                .FirstOrDefault(team => team.Champions.Count == teamSize
                    && team.Champions.Select(c => c.BowlerId).ToHashSet().SetEquals(query.BowlerIds));

            if (matched is not null)
                return new TeamEligibilityDto(TeamEligibilityOutcome.DefendingChampions, [], [], matched);
        }

        // 2. Facts per member. A bowler id that does not exist is a 404.
        var bowlers = await appDbContext.Bowlers.AsNoTracking()
            .Where(b => query.BowlerIds.Contains(b.Id))
            .Select(b => new { b.Id, b.DateOfBirth })
            .ToListAsync(cancellationToken);

        var missing = query.BowlerIds.FirstOrDefault(id => bowlers.All(b => b.Id != id));
        if (missing is not null) return BowlerErrors.NotFound(missing);

        // Categories is read in memory; it is not translated to SQL.
        var superiorPerformers = (await appDbContext.HallOfFameInductions.AsNoTracking()
                .Where(i => query.BowlerIds.Contains(i.BowlerId))
                .ToListAsync(cancellationToken))
            .Where(i => i.Categories.Contains(HallOfFameCategory.SuperiorPerformance))
            .Select(i => i.BowlerId)
            .ToHashSet();

        var titles = await titlesReader.LoadAsync(query.BowlerIds, cancellationToken);

        var members = bowlers
            .Select(b => new TeamMemberFacts
            {
                BowlerId = b.Id,
                DateOfBirth = b.DateOfBirth,
                Titles = [.. titles[b.Id]],
                HallOfFameSuperiorPerformance = superiorPerformers.Contains(b.Id),
            })
            .Concat(Enumerable.Range(0, query.UnknownBowlerCount).Select(_ => new TeamMemberFacts()))
            .ToList();

        // 3. Composition, then age, at both dates (the evaluator owns the rules and the combining).
        var result = TeamEligibilityEvaluator.Evaluate(
            new TeamEligibilityContext(tournament.TournamentType, tournament.StartDate, tournament.EndDate), members);

        return new TeamEligibilityDto(result.Outcome, result.Reasons, result.DateChecks);
    }
}

// CheckTeamEligibilityEndpoint.cs
internal sealed class CheckTeamEligibilityEndpoint(
    IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>> queryHandler)
    : Endpoint<CheckTeamEligibilityRequest, TeamEligibilityResponse>
{
    public override void Configure()
    {
        Get("{id}/team-eligibility");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("CheckTeamEligibility")
            .WithTags("Public")
            .Produces<TeamEligibilityResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CheckTeamEligibilityRequest req, CancellationToken ct)
    {
        var result = await queryHandler.HandleAsync(new CheckTeamEligibilityQuery
        {
            TournamentId = new TournamentId(req.Id),
            BowlerIds = [.. req.BowlerIds.Select(id => new BowlerId(id))],
            UnknownBowlerCount = req.UnknownBowlerCount,
        }, ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors) AddError(error.Description, error.Code);

            await Send.ErrorsAsync(result.FirstError.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError,
            }, ct);
            return;
        }

        var dto = result.Value;

        await Send.OkAsync(new TeamEligibilityResponse
        {
            Outcome = dto.Outcome.Name,
            Reasons = [.. dto.Reasons.Select(r => new TeamEligibilityReasonResponse { Code = r.Code, Message = r.Message })],
            DateChecks = [.. dto.DateChecks.Select(c => new TeamEligibilityDateCheckResponse
            {
                AsOf = c.AsOf,
                Eligible = c.Eligible,
                Reasons = [.. c.Reasons.Select(r => new TeamEligibilityReasonResponse { Code = r.Code, Message = r.Message })],
            })],
            DefendingSource = dto.DefendingTeam is { } team
                ? new DefendingSourceResponse
                {
                    TournamentName = team.SourceTournamentName,
                    SeasonDescription = team.SeasonDescription,
                    SourceTruncated = team.SourceTruncated,
                    Undefended = team.Undefended,
                }
                : null,
            // Pro titles are never checked; the page shows the note when the outcome is Eligible or DependsOnDate.
            ProfessionalTitlesVerified = false,
        }, ct);
    }
}
```

The summary class (`CheckTeamEligibilitySummary`) follows `GetBowlerTitlesEndpointSummary`: one example per outcome, and 400, 404, 422 responses. Log the bowler ids on the query as `[PersonalData]` where the handler logs them (it logs nothing today).

`Features/Bowlers/SearchBowlers/` (new use case)

```csharp
// SearchBowlersRequest.cs
internal sealed record SearchBowlersRequest
{
    public string Name { get; init; } = string.Empty;
}

// SearchBowlersRequestValidator.cs
internal sealed class SearchBowlersRequestValidator : Validator<SearchBowlersRequest>
{
    public SearchBowlersRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithErrorCode("SearchBowlersRequest.NameRequired").WithMessage("Name is required.")
            .MinimumLength(3).WithErrorCode("SearchBowlersRequest.NameTooShort").WithMessage("Enter at least 3 characters.")
            .MaximumLength(100).WithErrorCode("SearchBowlersRequest.NameTooLong").WithMessage("Name is too long.");
    }
}

// SearchBowlersQuery.cs
internal sealed record SearchBowlersQuery : IQuery<IReadOnlyCollection<BowlerSearchSummary>>
{
    public required string Name { get; init; }
}

// BowlerSearchSummary.cs
internal sealed record BowlerSearchSummary
{
    public required BowlerId Id { get; init; }
    public required Name Name { get; init; }
    public string? UsbcId { get; init; }
}

// SearchBowlersQueryHandler.cs
internal sealed class SearchBowlersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>
{
    private const int MaxResults = 20;

    public async Task<IReadOnlyCollection<BowlerSearchSummary>> HandleAsync(SearchBowlersQuery query, CancellationToken cancellationToken)
    {
        // Escape LIKE wildcards so "%" or "_" in the input match literally.
        var pattern = "%" + query.Name.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

        return await appDbContext.Bowlers.AsNoTracking()
            .Where(b => EF.Functions.ILike(b.Name.FirstName + " " + b.Name.LastName, pattern))
            .OrderBy(b => b.Name.LastName).ThenBy(b => b.Name.FirstName)
            .Take(MaxResults)
            .Select(b => new BowlerSearchSummary { Id = b.Id, Name = b.Name, UsbcId = b.UsbcId })
            .ToListAsync(cancellationToken);
    }
}

// SearchBowlersEndpoint.cs
internal sealed class SearchBowlersEndpoint(
    IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>> queryHandler)
    : Endpoint<SearchBowlersRequest, CollectionResponse<BowlerSearchResponse>>
{
    public override void Configure()
    {
        Get(string.Empty);                         // GET /bowlers?name=...
        Group<BowlersEndpointGroup>();

        Options(options => options
            .WithVersionSet("Bowlers")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(RateLimitingConfiguration.BowlerSearchPolicy));

        AllowAnonymous();

        Description(description => description
            .WithName("SearchBowlers")
            .WithTags("Public")
            .Produces<CollectionResponse<BowlerSearchResponse>>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(SearchBowlersRequest req, CancellationToken ct)
    {
        var results = await queryHandler.HandleAsync(new SearchBowlersQuery { Name = req.Name }, ct);

        await Send.OkAsync(new CollectionResponse<BowlerSearchResponse>
        {
            Items = [.. results.Select(r => new BowlerSearchResponse
            {
                BowlerId = r.Id.Value.ToString(),
                Name = r.Name.ToDisplayName(),
                UsbcId = r.UsbcId,
            })],
        }, ct);
    }
}
```

Check `CollectionResponse<T>`'s real shape before copying this block (its property name may differ from `Items`). Log the search term as `[PersonalData]` if it is ever logged.

`Features/Tournaments/PreviousTournamentResolver.cs` (new; shared by create and edit, like `TournamentSeasonAndPatternResolver`)

```csharp
internal static class PreviousTournamentResolver
{
    public static async Task<ErrorOr<PreviousTournamentFacts?>> ResolveAsync(
        AppDbContext appDbContext, TournamentId? previousTournamentId, CancellationToken cancellationToken)
    {
        if (previousTournamentId is not { } id) return (PreviousTournamentFacts?)null;

        var facts = await appDbContext.Tournaments.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new PreviousTournamentFacts(t.Id, t.TournamentType, t.EndDate))
            .SingleOrDefaultAsync(cancellationToken);

        return facts is null
            ? TournamentErrors.PreviousTournamentNotFound(id)
            : (PreviousTournamentFacts?)facts;
    }
}
```

`Features/Tournaments/CreateTournament/*` and `EditTournament/*` (edit)

```csharp
// CreateTournamentCommand.cs and EditTournamentCommand.cs
public TournamentId? PreviousTournamentId { get; init; }

// CreateTournamentCommandHandler.cs, after the season/pattern resolve
var previous = await PreviousTournamentResolver.ResolveAsync(appDbContext, command.PreviousTournamentId, cancellationToken);
if (previous.IsError) return previous.Errors;
// ... Tournament.Create(..., nebaAddedMoney: command.NebaAddedMoney, previousTournament: previous.Value);

// EditTournamentCommandHandler.cs, same resolve, then
var updateResult = tournament.Update(/* existing arguments */, previousTournament: previous.Value);

// TournamentInputMapper.cs
public static TournamentId? ToPreviousTournamentId(string? previousTournamentId) =>
    string.IsNullOrWhiteSpace(previousTournamentId) ? null : new TournamentId(previousTournamentId);

// Create and Edit endpoints: add to the command initializers
PreviousTournamentId = TournamentInputMapper.ToPreviousTournamentId(input.PreviousTournamentId),

// TournamentInputValidator.cs
RuleFor(x => x.PreviousTournamentId)
    .Length(26).WithErrorCode($"{prefix}.PreviousTournamentIdInvalidLength").WithMessage("Previous tournament ID must be a 26-character ULID.")
    .When(x => !string.IsNullOrWhiteSpace(x.PreviousTournamentId));
```

`Features/Tournaments/ListTournamentsInSeason/*` and `GetTournament/*` (edit; add defending champions beside `Winners`)

```csharp
// SeasonTournamentDto.cs and TournamentDetailDto.cs
/// <summary>Teams that may bowl together as this tournament's defending champions; empty when there is no previous tournament or it has no results.</summary>
public IReadOnlyCollection<DefendingTeamDto> DefendingChampions { get; init; } = [];

public sealed record DefendingTeamDto
{
    public required string TournamentName { get; init; }
    public required string SeasonDescription { get; init; }
    public required bool SourceTruncated { get; init; }
    public required bool Undefended { get; init; }
    public required IReadOnlyCollection<Name> Champions { get; init; }
}

// TournamentDetailDto.cs only
public TournamentId? PreviousTournamentId { get; init; }   // for the edit form

// ListTournamentsInSeasonQueryHandler.cs: inject DefendingTeamsReader; the row projection adds
//   tournament.Id is already projected, so no new column is needed.
// After `rows` is loaded:
var defendingByTournament = await defendingTeamsReader.LoadAsync([.. rows.Select(r => r.Id)], cancellationToken);
// and in the DTO projection:
DefendingChampions = [.. defendingByTournament.GetValueOrDefault(row.Id, []).Select(team => new DefendingTeamDto
{
    TournamentName = team.SourceTournamentName,
    SeasonDescription = team.SeasonDescription,
    SourceTruncated = team.SourceTruncated,
    Undefended = team.Undefended,
    Champions = [.. team.Champions.Select(champion => champion.Name)],
})],

// GetTournamentQueryHandler.cs: same, for the single tournament (LoadAsync([query.Id], ...)).
```

Both DTOs hold value objects and primitives only, so the cached-query rule (no SmartEnum instances in cached DTOs) still holds. The status check happens inside the reader, so no `TournamentStatus` instance reaches the cached DTO.

Cache eviction: defending teams now sit inside cached reads of *later* tournaments (a different season), so any change that affects a tournament's winners or its truncated status must evict the tournaments downstream of it. `Features/Tournaments/TournamentSuccessorCacheEviction.cs` (new):

```csharp
internal static class TournamentSuccessorCacheEviction
{
    /// <summary>
    /// Evicts the cached list and detail of every tournament that names <paramref name="tournamentId"/> as its
    /// previous tournament. A truncated tournament also passes its own previous winners down, so the walk
    /// continues through successors that are truncated.
    /// </summary>
    public static async Task EvictAsync(AppDbContext appDbContext, IFusionCache cache, TournamentId tournamentId, CancellationToken cancellationToken)
    {
        var frontier = new List<TournamentId> { tournamentId };

        while (frontier.Count > 0)
        {
            var successors = await appDbContext.Tournaments.AsNoTracking()
                .Where(t => frontier.Contains(t.PreviousTournamentId!.Value))   // match the typed-id comparison style used elsewhere
                .Select(t => new { t.Id, t.SeasonId, t.Status })
                .ToListAsync(cancellationToken);

            foreach (var successor in successors)
            {
                await cache.RemoveByTagAsync($"neba:tournaments:{successor.Id}", token: cancellationToken);
                await cache.RemoveByTagAsync($"neba:tournaments:{successor.SeasonId}", token: cancellationToken);
            }

            frontier = [.. successors.Where(s => s.Status == TournamentStatus.Truncated).Select(s => s.Id)];
        }
    }
}
```

Call sites (corrected: truncating a tournament only changes its status; it deletes nothing):

- `Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs`: after the existing tag removals (around line 69), because the winners changed.
- `Features/Tournaments/TruncateTournament/TruncateTournamentCommandHandler.cs`: after `SaveChangesAsync`, because the new status adds the previous winners to the next tournament's defending teams.
- `Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandler.cs`: before the delete, because the successor's link is set to null afterward and the walk needs it.
- `EditTournamentCommandHandler`: when the type, end date, or previous link changed, because a truncated tournament passes its own previous winners down.
- `Legacy/Tournaments/Complete/CompleteTournamentSyncJob.cs` (the Truncated/Cancelled status path around line 63): after a status change.

Contracts (edit)

```csharp
// Neba.Api.Contracts/Tournaments/CreateTournament/TournamentInput.cs
/// <summary>The previous (last year's) tournament's ID; null if none. Not allowed for Non-Champions tournaments.</summary>
public string? PreviousTournamentId { get; init; }

// Neba.Api.Contracts/Tournaments/GetTournament/TournamentDetailResponse.cs
public string? PreviousTournamentId { get; init; }
public IReadOnlyCollection<DefendingTeamResponse> DefendingChampions { get; init; } = [];

// Neba.Api.Contracts/Seasons/ListTournamentsInSeason/SeasonTournamentResponse.cs
public IReadOnlyCollection<DefendingTeamResponse> DefendingChampions { get; init; } = [];
// Endpoint mappings fill names the same way Winners is filled (Name.ToDisplayName()).

// Team size (added after the Blazor review: no list or detail contract carries it, and the site needs it
// to decide which tournaments get the eligibility link and how many pickers to show).
// SeasonTournamentResponse.cs and TournamentDetailResponse.cs
/// <summary>Bowlers per entry: 1 for singles-style formats, 2 for doubles, 3 for trios, 5 for baker.</summary>
public int TeamSize { get; init; } = 1;
// SeasonTournamentDto.cs and TournamentDetailDto.cs get `public int TeamSize { get; init; } = 1;`.
// GetTournamentQueryHandler already computes it (`TournamentType.FromValue(...).TeamSize`); the list handler sets
// `TeamSize = TournamentType.FromName(row.TournamentType).TeamSize` when it builds each DTO.

// Neba.Api.Contracts/Tournaments/DefendingTeamResponse.cs (new, shared by both)
public sealed record DefendingTeamResponse
{
    /// <summary>The tournament this team won.</summary>
    public string TournamentName { get; init; } = string.Empty;
    public string SeasonDescription { get; init; } = string.Empty;
    /// <summary>The tournament this team won was truncated.</summary>
    public bool SourceTruncated { get; init; }
    /// <summary>The team won an earlier tournament and could not defend because a later one was truncated.</summary>
    public bool Undefended { get; init; }
    public IReadOnlyCollection<string> Champions { get; init; } = [];
}

// Neba.Api.Contracts/Tournaments/CheckTeamEligibility/TeamEligibilityResponse.cs (new)
public sealed record TeamEligibilityResponse
{
    /// <summary>DefendingChampions, Eligible, Ineligible, or DependsOnDate.</summary>
    public required string Outcome { get; init; }
    public IReadOnlyCollection<TeamEligibilityReasonResponse> Reasons { get; init; } = [];
    /// <summary>Per-date results; filled only when Outcome is DependsOnDate.</summary>
    public IReadOnlyCollection<TeamEligibilityDateCheckResponse> DateChecks { get; init; } = [];
    /// <summary>Always false today: PBA/PWBA titles are not checked.</summary>
    public bool ProfessionalTitlesVerified { get; init; }
    /// <summary>Which tournament this team won; filled only when Outcome is DefendingChampions.</summary>
    public DefendingSourceResponse? DefendingSource { get; init; }
}
public sealed record DefendingSourceResponse
{
    public required string TournamentName { get; init; }
    public required string SeasonDescription { get; init; }
    /// <summary>The tournament this team won was truncated.</summary>
    public bool SourceTruncated { get; init; }
    /// <summary>The team won an earlier tournament and could not defend because a later one was truncated.</summary>
    public bool Undefended { get; init; }
}
public sealed record TeamEligibilityReasonResponse { public required string Code { get; init; } public required string Message { get; init; } }
public sealed record TeamEligibilityDateCheckResponse
{
    public required DateOnly AsOf { get; init; }
    public required bool Eligible { get; init; }
    public IReadOnlyCollection<TeamEligibilityReasonResponse> Reasons { get; init; } = [];
}

// Neba.Api.Contracts/Bowlers/SearchBowlers/BowlerSearchResponse.cs (new)
public sealed record BowlerSearchResponse
{
    public required string BowlerId { get; init; }
    public required string Name { get; init; }
    public string? UsbcId { get; init; }
}

// ITournamentsApi.cs
[Get("/tournaments/{tournamentId}/team-eligibility")]
Task<IApiResponse<TeamEligibilityResponse>> CheckTeamEligibilityAsync(
    string tournamentId,
    [Query(CollectionFormat.Multi)] IEnumerable<string> bowlerIds,
    int unknownBowlerCount,
    CancellationToken cancellationToken = default);

// IBowlersApi.cs
[Get("/bowlers")]
Task<IApiResponse<CollectionResponse<BowlerSearchResponse>>> SearchBowlersAsync(
    string name, CancellationToken cancellationToken = default);
```

### Infrastructure

`Database/Configurations/TournamentConfiguration.cs` (edit)

```csharp
builder.Property(tournament => tournament.PreviousTournamentId)
    .IsUlid("previous_tournament_id");

builder.HasOne<Tournament>()
    .WithMany()
    .HasForeignKey(tournament => tournament.PreviousTournamentId)
    .HasPrincipalKey(tournament => tournament.Id)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasIndex(tournament => tournament.PreviousTournamentId);
```

`Database/Configurations/BowlerConfiguration.cs` (edit)

```csharp
builder.Property(bowler => bowler.UsbcId)
    .HasMaxLength(20)
    .IsRequired(false);

builder.HasIndex(bowler => bowler.UsbcId);
```

Migrations: `AddTournamentPreviousTournament` and `AddBowlerUsbcId`, generated with `dotnet ef migrations add` in `Database/Migrations`. Check the generated SQL, since `OnDelete(SetNull)` on a self-reference must not create a cycle error.

`RateLimiting/RateLimitingConfiguration.cs` (edit; follows the password-recovery pattern)

```csharp
internal const string BowlerSearchPolicy = "bowler-search";

// in AddRateLimiting
var bowlerSearchPermitLimit = config.GetValue("RateLimiting:BowlerSearchPermitLimit", 60);
var bowlerSearchWindowSeconds = config.GetValue("RateLimiting:BowlerSearchWindowSeconds", 60);
// ...same "must be greater than zero" checks as the other limits...

options.AddPolicy(BowlerSearchPolicy, context =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter($"bowler-search:{ip}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = bowlerSearchPermitLimit,
            Window = TimeSpan.FromSeconds(bowlerSearchWindowSeconds),
            QueueLimit = 0,
        });
});
```

The check endpoint uses the group's existing public policy (100 per minute per IP). The site's server connection forwards the visitor's IP, so the per-IP partition works (see `ForwardedHeadersOptions`).

Legacy bowler jobs (edit): the USBC ID is the `UsbcId` column of the legacy `dbo.Bowlers` table. Three jobs read that table, so all three change.

```csharp
// Legacy/Bowlers/NewBowler.cs
internal sealed record LegacyBowlerRow(
    int Id, string FirstName, string? MiddleInitial, string LastName, string? Suffix,
    int Gender, DateTime? DateOfBirth, string? UsbcId);          // new last member

// NewBowlerSyncJob, UpdateBowlerSyncJob, BowlerWeeklySyncJob: add the column to each SELECT
//     ...
//     Gender,
//     DateOfBirth,
//     UsbcId
// FROM Bowlers

// Legacy/Bowlers/LegacyBowlerMapping.cs
// Blank and whitespace-only values become null, so an empty legacy column never overwrites a real ID.
public static string? MapUsbcId(string? legacyUsbcId)
    => string.IsNullOrWhiteSpace(legacyUsbcId) ? null : legacyUsbcId.Trim();

// CreateBowler passes it through:
//     dateOfBirth: MapDateOfBirth(row.DateOfBirth),
//     usbcId: MapUsbcId(row.UsbcId));

// Legacy/Bowlers/NewBowler.cs: Bowler.CreateFromLegacy gains `string? usbcId = null`
//     ... DateOfBirth = dateOfBirth,
//         UsbcId = usbcId
// Legacy/Bowlers/UpdateBowler.cs: ApplyLegacyUpdate gains `string? usbcId = null`, sets bowler.UsbcId = usbcId;
// NewBowlerSyncJob and UpdateBowlerSyncJob call sites pass MapUsbcId(row.UsbcId).
// UpdateBowlerSyncJob already evicts "neba:bowlers" after saving; search is uncached, so nothing new to evict.

// Legacy/Sync/BowlerWeeklySyncJob.cs: the "Unchanged" check and the update call include it
var usbcId = LegacyBowlerMapping.MapUsbcId(row.UsbcId);
// ...
if (existing.Name == name.Value && existing.Gender == gender
    && existing.DateOfBirth == dateOfBirth && existing.UsbcId == usbcId)
{
    return Outcome.Unchanged;
}
// ...
var result = existing.ApplyLegacyUpdate(/* existing arguments */, dateOfBirth: dateOfBirth, usbcId: usbcId);
```

Behavior note: like gender and DOB today, a blank legacy `UsbcId` clears the website value on the next update or weekly sync. If you would rather a blank never erase an ID the LINQPad backfill wrote, say so and the apply method will keep the existing one when the legacy value is blank.

The one-time backfill of existing bowlers is your LINQPad script at the 1.2.0 release and is outside this plan. Tests: `LegacyBowlerMappingTests` for `MapUsbcId` (null, blank, padded), and each job's existing tests gain a `UsbcId` case (create stores it, update changes it, weekly sync treats an unchanged ID as `Unchanged` and a changed one as `Updated`).

### Authorization

- Both new endpoints are `AllowAnonymous()`, tag `Public`. No change to `docs/policies/README.md`.
- Create and edit tournament keep their existing permissions; the new field adds no policy.

### Tests

Factories (`Neba.TestFactory`):

```csharp
public static class TeamMemberFactsFactory
{
    public static readonly DateOnly ValidDateOfBirth = new(1990, 6, 15);

    public static TeamMemberFacts Create(
        BowlerId? bowlerId = null, DateOnly? dateOfBirth = null,
        IReadOnlyCollection<TitleWin>? titles = null, bool hallOfFameSuperiorPerformance = false)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            DateOfBirth = dateOfBirth ?? ValidDateOfBirth,
            Titles = titles ?? [],
            HallOfFameSuperiorPerformance = hallOfFameSuperiorPerformance,
        };

    public static TeamMemberFacts Unknown() => new();

    public static IReadOnlyList<TeamMemberFacts> Bogus(int count, int? seed = null) { /* Bogus */ }
}

public static class PreviousTournamentFactsFactory
{
    public static PreviousTournamentFacts Create(
        TournamentId? id = null, TournamentType? tournamentType = null, DateOnly? endDate = null)
        => new(id ?? TournamentId.New(), tournamentType ?? TournamentType.Singles, endDate ?? new DateOnly(2025, 3, 2));
}
```

Also extend `TournamentFactory.Create` with `previousTournament`, and `BowlerFactory.Create` with `usbcId`. Check that each factory's no-argument `Create()` still persists.

Unit tests (`[UnitTest]`, `[Component("Tournaments")]`, strict mocks, AAA comments, Shouldly):

- `TeamEligibilityEvaluatorTests`: every row of the rules table below as a `[Theory]`; plus missing DOB (over/under and regular doubles), unknown bowlers, DOB never appears in any reason, same-weekend exclusion, the tournament's own title excluded, titles after the as-of date ignored, window edges (exactly 10 and exactly 5 years), 60th-birthday window, `DependsOnDate` only when composition passes at both dates, and `Ineligible` merging duplicate reasons.
- `TournamentPreviousTournamentTests`: self, type mismatch, not before, Non-Champions, valid, cleared on update with null.
- `CheckTeamEligibilityQueryHandlerTests` (in-memory or mocked context per the handler test style): defending champions short-circuit, skipped with an unknown bowler, team size mismatch (422), not a team tournament, unknown bowler id (404), Hall of Fame Superior Performance read from `Categories`.
- `DefendingTeamsReaderTests`: no previous tournament; previous completed (one team); previous truncated (two teams, the second marked `Undefended`); two truncated in a row (three teams); a truncated tournament with no previous (one team); a previous tournament with no results (no team); several tournaments in one call.
- Validator tests for both new requests; `SearchBowlersQueryHandlerTests` for wildcard escaping and the 20-result cap.

| Case | Format | Expected |
| --- | --- | --- |
| Two champions in the last 10 years | Doubles | `Ineligible`: `Champions.TooMany` |
| 8-year-old title, bowler is 62, DOB known | Doubles | Eligible (5-year window) |
| Same title, DOB missing | Doubles | `Ineligible` (10-year window applies) |
| One champion, one non-champion | Doubles | `Eligible` |
| Two overs, or two unders | Over/Under 50 | `Ineligible`: `Age.SameAgeGroup` |
| Any bowler not found in the system | Over/Under 50 | `Ineligible`: `Age.NotVerified` |
| Any DOB missing | Over/Under 50 | `Ineligible`: `Age.NotVerified` only |
| Over and under, one champion | Over/Under 50 | `Eligible` |
| Three champions, none multi-time | Trios | `Ineligible`: `Champions.NoNonChampion` |
| Two multi-time champions | Trios | `Ineligible`: `Champions.TooManyMultiTime` |
| Three Hall of Fame (Superior Performance) | Baker | `Ineligible`: `HallOfFame.TooMany` |
| Two Superior Performance + three unknown | Baker | `Ineligible`: `HallOfFame.TooMany` |
| Turns 50 between start and end date, otherwise fine | Over/Under 50 | `DependsOnDate` |
| Team equals the previous tournament's winners | Any team format | `DefendingChampions`, no other rule runs |
| Previous truncated; team equals that tournament's winners | Doubles | `DefendingChampions` (`SourceTruncated`) |
| Previous truncated; team equals the winners before it | Doubles | `DefendingChampions` (`Undefended`) |
| Previous completed; team equals the winners before it | Doubles | Normal rules (not exempt) |
| Team has one defender and one new bowler | Doubles | Normal rules |

Integration tests (`[IntegrationTest]`): both endpoints through the real pipeline (200, 400, 404, 422); search returns USBC ID and caps at 20; the migrations apply and the self-FK sets null on delete; `GET /tournaments/{id}` and the season list return `DefendingChampions` for a linked previous tournament (one team), a truncated one (two teams), and empty without one; the successor's cached list and detail are evicted after results sync and after the previous tournament is truncated; the search policy returns 429 after the limit.

e2e: new response properties default to empty, so `tests/e2e/mock-api/mock-api-server.ts` needs no change for them; Phase 2 adds mock routes for the two new endpoints.

### Deferred

- Registration search (authenticated), PBA/PWBA title data, changing a tournament's type or dates when another tournament points at it, moving `GetBowlerTitles` onto `BowlerTitlesReader`.

### How the NFR targets show up here

- Performance: search is capped at 20 results with a 3-character minimum and its own 60-per-minute policy; the check is uncached and runs a handful of queries.
- Security: no DOB field in any response; one reason text for every missing-DOB case; both routes rate limited; no logging of DOB.
- Concurrency and availability: read-only, so no tokens and no degraded mode.
- Cache: the two cached tournament reads now carry defending champions, so successor tournaments are evicted when results change.

## Phase 2: UI

Conventions followed: Server project for everything (the Client project stays empty; nothing here needs the browser alone). Pages are thin orchestrators, feature components get data through parameters, and services wrap the Refit interfaces and return `ErrorOr`. Every routable page has `<PageTitle>` (`{Page} - BowlNEBA`) and `@rendermode InteractiveServer`. Razor `@code` follows the repo rules: no `$"..."` interpolation, no `< N =>` patterns, `[Parameter, EditorRequired]` instead of `required`. Components that take a view model as a parameter need a `public` view model, so the new view models are `public`; the service interface stays `internal`.

Findings from the live rules document (exported from the Drive file the API serves, 449 KB):

- Lettered sub-rules **are headings**: `<h4>401a. Team Composition</h4>`, `<h4>401b. Champion Definition</h4>`, `<h4>402a. Team Composition</h4>`, `<h4>402b. Champion Definition</h4>`, `<h4>403a. Team Composition Criteria</h4>`. The risk from the functional draft is closed.
- In that export the heading ids look like `h.i34k8d87vsx`, not `rule-401a-...`. The sub-rule cut therefore matches on heading **text** ("401a" plus a keyword), never on the id, so it works with either shape.
- 402b has `<h5>` children (One Time Champion, Multi-Time Champion, Non-Champion). Cutting to the next heading of the same or higher level (`h4` or above) keeps them.
- 403a links to 402b with `<a href="#h.ssua519ioj21">`; the extractor already rewrites `#...` links to the full rules page.
- 401a shows one numbered list split across three `<ol start="...">` blocks (1-2, then 1, then 3-4). The existing extractor keeps the document's `<style>` blocks for list numbering; check the rendered numbering on the page, because a fix belongs in the source document, not in code.
- Rule 401f ("Title Defense") says an over/under team can defend if one person became an over. The defending-champions check skips every other rule, so this already holds.

### Rules extraction

`Documents/TournamentRulesAnchors.cs` (edit)

```csharp
/// <summary>One lettered sub-rule, such as 401a.</summary>
/// <param name="RuleNumber">Rule number, such as 401.</param>
/// <param name="Letter">Sub-rule letter, such as <c>a</c>.</param>
/// <param name="TitleKeyword">Text the heading must contain.</param>
public sealed record SubRuleRef(int RuleNumber, char Letter, string TitleKeyword)
{
    /// <summary>How the heading starts, such as "401a".</summary>
    public string HeadingPrefix => RuleNumber.ToString(CultureInfo.InvariantCulture) + Letter;
}

/// <summary>The sub-rules that describe who may form a team, for one tournament type.</summary>
/// <param name="SubRules">Shown in this order.</param>
/// <param name="FullRulesHref">Link to the full rules for the "read the full rules" fallback.</param>
public sealed record TeamRulesAnchor(IReadOnlyList<SubRuleRef> SubRules, string FullRulesHref);

// inside TournamentRulesAnchors
private static readonly TeamRulesAnchor DoublesTeamRules = new(
    [new(401, 'a', "Team Composition"), new(401, 'b', "Champion Definition")],
    RulesPath + "#doubles");

private static readonly Dictionary<string, TeamRulesAnchor> TeamRulesByType =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["Doubles"] = DoublesTeamRules,
        ["Over/Under 50 Doubles"] = DoublesTeamRules,
        ["Over/Under 40 Doubles"] = DoublesTeamRules,
        ["Trios"] = new(
            [new(402, 'a', "Team Composition"), new(402, 'b', "Champion Definition")],
            RulesPath + "#trios"),
        // Baker's own sub-rule (403a) points at the Trios champion definition (402b), so it is shown too.
        ["Baker"] = new(
            [new(403, 'a', "Team Composition"), new(402, 'b', "Champion Definition")],
            RulesPath + "#baker"),
    };

/// <summary>Finds the team composition and champion definition sub-rules for a tournament type.</summary>
public static bool TryGetTeamRules(string tournamentType, [NotNullWhen(true)] out TeamRulesAnchor? anchor)
    => TeamRulesByType.TryGetValue(tournamentType, out anchor);
```

`Documents/RulesSectionExtractor.cs` (edit)

```csharp
/// <summary>
/// Returns one lettered sub-rule (such as 401a): its heading and content up to the next heading of the
/// same or higher level. Matches on heading text, because the document's heading ids are not stable.
/// Pass <paramref name="includeStyles"/> for the first piece only, so list styles are not repeated.
/// Returns <c>null</c> when no heading matches.
/// </summary>
internal static string? ExtractSubRule(string html, SubRuleRef subRule, string rulesPath, bool includeStyles)
{
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    var start = doc.DocumentNode
        .Descendants()
        .FirstOrDefault(node => HeadingLevel(node) is not null && IsSubRuleHeading(node, subRule));

    return start is null ? null : Cut(doc, start, rulesPath, includeStyles);
}

// "401a. Team Composition" and "Rule 401a. Team Composition" both match.
private static bool IsSubRuleHeading(HtmlNode node, SubRuleRef subRule)
{
    var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
    if (text.StartsWith("Rule ", StringComparison.OrdinalIgnoreCase))
    {
        text = text[5..].TrimStart();
    }

    return text.StartsWith(subRule.HeadingPrefix, StringComparison.OrdinalIgnoreCase)
        && text.Contains(subRule.TitleKeyword, StringComparison.OrdinalIgnoreCase);
}

// The cutting loop and link rewrite that Extract already has, moved here so both entry points share it.
private static string Cut(HtmlDocument doc, HtmlNode start, string rulesPath, bool includeStyles)
{
    var level = HeadingLevel(start)!.Value;
    var section = HtmlNode.CreateNode("<div></div>");

    if (includeStyles)
    {
        foreach (var style in doc.DocumentNode.Descendants("style"))
        {
            section.AppendChild(style.CloneNode(true));
        }
    }

    for (var node = start; node is not null; node = NextElement(node))
    {
        var nodeLevel = HeadingLevel(node);
        if (node != start && nodeLevel is { } l && l <= level)
        {
            break;
        }

        section.AppendChild(node.CloneNode(true));
    }

    foreach (var link in section.Descendants("a").Where(a => a.GetAttributeValue("href", string.Empty).StartsWith('#')))
    {
        link.SetAttributeValue("href", rulesPath + link.GetAttributeValue("href", string.Empty));
    }

    return section.InnerHtml;
}

// Extract(...) keeps its heading lookup and ends with: return Cut(doc, start, rulesPath, includeStyles: true);
```

`Documents/IRulesSectionService.cs` and `RulesSectionService.cs` (edit)

```csharp
// IRulesSectionService
/// <summary>
/// Loads the rules document and joins the sub-rules <paramref name="anchor"/> lists, in order.
/// Returns <see cref="ErrorType.NotFound"/> when any of them is missing.
/// </summary>
Task<ErrorOr<RulesSection>> GetTeamRulesAsync(TeamRulesAnchor anchor, CancellationToken ct = default);

// RulesSectionService
public async Task<ErrorOr<RulesSection>> GetTeamRulesAsync(TeamRulesAnchor anchor, CancellationToken ct = default)
{
    var result = await executor.ExecuteAsync(
        "Documents",
        "GetTournamentRules",
        token => documentsApi.GetDocumentAsync(TournamentRulesAnchors.DocumentName, token),
        ct);

    if (result.IsError)
    {
        return Error.Failure(
            "RulesSection.LoadFailed",
            "The rules document could not be loaded: " + result.FirstError.Description);
    }

    var pieces = new List<string>(anchor.SubRules.Count);
    for (var i = 0; i < anchor.SubRules.Count; i++)
    {
        var piece = RulesSectionExtractor.ExtractSubRule(
            result.Value.Html, anchor.SubRules[i], TournamentRulesAnchors.RulesPath, includeStyles: i == 0);

        if (piece is null)
        {
            return Error.NotFound(
                "RulesSection.NotFound",
                "No rule " + anchor.SubRules[i].HeadingPrefix + " found.");
        }

        pieces.Add(piece);
    }

    var lastUpdated = await clientTimeZoneService.ToLocalOrNullAsync(result.Value.LastUpdated);

    return new RulesSection(string.Concat(pieces), lastUpdated);
}
```

### API client and service

`Tournaments/TeamEligibility/ITeamEligibilityService.cs` and `TeamEligibilityService.cs` (new)

```csharp
internal interface ITeamEligibilityService
{
    /// <summary>Checks whether the team may bowl together. <paramref name="unknownBowlerCount"/> counts members not found in the system.</summary>
    Task<ErrorOr<TeamEligibilityResultViewModel>> CheckAsync(
        string tournamentId, IReadOnlyCollection<string> bowlerIds, int unknownBowlerCount, CancellationToken ct = default);

    /// <summary>Searches bowlers by name; the API needs at least 3 characters and returns at most 20.</summary>
    Task<ErrorOr<IReadOnlyList<BowlerSearchResultViewModel>>> SearchBowlersAsync(string name, CancellationToken ct = default);
}

internal sealed class TeamEligibilityService(
    ApiExecutor executor,
    ITournamentsApi tournamentsApi,
    IBowlersApi bowlersApi) : ITeamEligibilityService
{
    public async Task<ErrorOr<TeamEligibilityResultViewModel>> CheckAsync(
        string tournamentId, IReadOnlyCollection<string> bowlerIds, int unknownBowlerCount, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "TournamentsApi",
            nameof(CheckAsync),
            token => tournamentsApi.CheckTeamEligibilityAsync(tournamentId, bowlerIds, unknownBowlerCount, token),
            ct);

        if (result.IsError)
        {
            return result.Errors;
        }

        // A new outcome name from the API must not read as "eligible" or "ineligible" by accident.
        return Enum.TryParse<TeamEligibilityOutcomeKind>(result.Value.Outcome, out var outcome)
            ? result.Value.ToViewModel(outcome)
            : Error.Failure("TeamEligibility.UnknownOutcome", "The API returned an outcome this site does not know.");
    }

    public async Task<ErrorOr<IReadOnlyList<BowlerSearchResultViewModel>>> SearchBowlersAsync(
        string name, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "BowlersApi",
            nameof(SearchBowlersAsync),
            token => bowlersApi.SearchBowlersAsync(name, token),
            ct);

        if (result.IsError)
        {
            return result.Errors;
        }

        IReadOnlyList<BowlerSearchResultViewModel> bowlers = [.. result.Value.Items.Select(b => b.ToViewModel())];
        return ErrorOrFactory.From(bowlers);
    }
}
```

`Program.cs` (edit, next to `ITournamentApiService`)

```csharp
builder.Services.AddScoped<ITeamEligibilityService, TeamEligibilityService>();
```

`IBowlersApi` is already registered (`ApiServicesConfiguration.cs:96`); Phase 1 adds the search method to it.

### View models and mapping

`Tournaments/TeamEligibility/TeamEligibilityResultViewModel.cs`, `BowlerSearchResultViewModel.cs`, `TeamEligibilityMappingExtensions.cs` (new)

```csharp
public enum TeamEligibilityOutcomeKind { DefendingChampions, Eligible, Ineligible, DependsOnDate }

public sealed record TeamEligibilityResultViewModel
{
    public required TeamEligibilityOutcomeKind Outcome { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public IReadOnlyList<TeamEligibilityDateCheckViewModel> DateChecks { get; init; } = [];
    public bool ProfessionalTitlesVerified { get; init; }
    public DefendingTeamViewModel? DefendingSource { get; init; }

    /// <summary>The pro-title note applies whenever the team passed on NEBA data.</summary>
    public bool ShowProfessionalTitlesNote =>
        !ProfessionalTitlesVerified
        && (Outcome == TeamEligibilityOutcomeKind.Eligible
            || (Outcome == TeamEligibilityOutcomeKind.DependsOnDate && DateChecks.Any(check => check.Eligible)));
}

public sealed record TeamEligibilityDateCheckViewModel(DateOnly AsOf, bool Eligible, IReadOnlyList<string> Reasons);

public sealed record BowlerSearchResultViewModel(string BowlerId, string Name, string? UsbcId)
{
    /// <summary>"Dave Smith (4821-09317)", or the name alone when there is no USBC ID.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(UsbcId) ? Name : Name + " (" + UsbcId + ")";
}

internal static class TeamEligibilityMappingExtensions
{
    extension(TeamEligibilityResponse response)
    {
        public TeamEligibilityResultViewModel ToViewModel(TeamEligibilityOutcomeKind outcome) => new()
        {
            Outcome = outcome,
            Reasons = [.. response.Reasons.Select(reason => reason.Message)],
            DateChecks =
            [
                .. response.DateChecks.Select(check => new TeamEligibilityDateCheckViewModel(
                    check.AsOf, check.Eligible, [.. check.Reasons.Select(reason => reason.Message)]))
            ],
            ProfessionalTitlesVerified = response.ProfessionalTitlesVerified,
            DefendingSource = response.DefendingSource is { } source
                ? new DefendingTeamViewModel(source.TournamentName, source.SeasonDescription, source.SourceTruncated, source.Undefended, [])
                : null,
        };
    }

    extension(BowlerSearchResponse response)
    {
        public BowlerSearchResultViewModel ToViewModel() => new(response.BowlerId, response.Name, response.UsbcId);
    }
}
```

`Tournaments/DefendingTeamViewModel.cs` and mapping (new)

```csharp
public sealed record DefendingTeamViewModel(
    string TournamentName,
    string SeasonDescription,
    bool SourceTruncated,
    bool Undefended,
    IReadOnlyList<string> Champions)
{
    /// <summary>Short note under the season, or null for a normal defending team.</summary>
    public string? Note => SourceTruncated ? "tournament truncated" : Undefended ? "could not defend" : null;
}

internal static class DefendingTeamMappingExtensions
{
    extension(DefendingTeamResponse response)
    {
        public DefendingTeamViewModel ToViewModel() => new(
            response.TournamentName, response.SeasonDescription, response.SourceTruncated, response.Undefended,
            [.. response.Champions]);
    }
}
```

`Tournaments/Detail/TournamentDetailViewModel.cs` (edit)

```csharp
/// <summary>Bowlers per entry: 1 for singles-style formats, 2 for doubles, 3 for trios, 5 for baker.</summary>
public int TeamSize { get; init; } = 1;

/// <summary>Id of last year's tournament of this format; null when there is none.</summary>
public string? PreviousTournamentId { get; init; }

/// <summary>Teams that defend their title here; empty when there is no previous tournament.</summary>
public IReadOnlyList<DefendingTeamViewModel> DefendingChampions { get; init; } = [];

/// <summary>Scheduled tournaments only; once the tournament finishes the winners show instead.</summary>
public bool ShowDefendingChampions => IsScheduled && DefendingChampions.Count > 0;

/// <summary>Same scheduled-only rule as <see cref="ShowRulesLink"/>.</summary>
public bool ShowTeamEligibilityLink => IsScheduled && TeamSize > 1;
```

`Tournaments/Detail/TournamentDetailMappingExtensions.cs` (edit, inside `ToViewModel`)

```csharp
TeamSize = response.TeamSize,
PreviousTournamentId = response.PreviousTournamentId,
DefendingChampions = [.. response.DefendingChampions.Select(team => team.ToViewModel())],
```

`Tournaments/Schedule/SeasonTournamentViewModel.cs` (edit)

```csharp
public int TeamSize { get; init; } = 1;
public IReadOnlyList<DefendingTeamViewModel> DefendingChampions { get; init; } = [];

/// <summary>True while the tournament has not run; the only state where defending champions show.</summary>
public bool IsScheduled => Status == "Scheduled";

public bool ShowDefendingChampions => IsScheduled && DefendingChampions.Count > 0;
public bool ShowTeamEligibilityLink => IsScheduled && TeamSize > 1;
```

`Tournaments/TournamentApiService.cs` (edit, in the season `MapToViewModel`)

```csharp
TeamSize = response.TeamSize,
DefendingChampions = [.. response.DefendingChampions.Select(team => team.ToViewModel())],
```

### Components

`Components/NebaAutocomplete.razor` (edit)

```razor
@* in the empty-list branch, replacing the single <li> *@
@if (_filteredItems.Count == 0)
{
    <li class="neba-autocomplete-empty">
        @NoResultsText
        @NoResultsContent
    </li>
}
```

```csharp
/// <summary>
/// True when <see cref="Items"/> is already filtered by the caller (a server search). The component then
/// shows the items as given instead of filtering them by the typed text.
/// </summary>
[Parameter]
public bool ItemsAreFiltered { get; set; }

/// <summary>Extra content after <see cref="NoResultsText"/> when nothing matches, such as an action link.</summary>
[Parameter]
public RenderFragment? NoResultsContent { get; set; }

protected override void OnParametersSet()
{
    // A server search replaces Items while the list is open; show the new items right away.
    if (ItemsAreFiltered && _isOpen)
    {
        _filteredItems = Items.ToList();
        _highlightedIndex = Math.Min(_highlightedIndex, Math.Max(_filteredItems.Count - 1, 0));
    }
}

private void UpdateFilteredItems()
{
    _filteredItems = ItemsAreFiltered || string.IsNullOrWhiteSpace(_filterText)
        ? Items.ToList()
        : Items.Where(item => GetDisplayText(item).Contains(_filterText, StringComparison.OrdinalIgnoreCase)).ToList();

    _highlightedIndex = 0;
}
```

`Tournaments/TeamEligibility/BowlerNotFoundControl.razor` (new): the link under each picker (mockup option 3). It marks one slot as "not in our system" or undoes the mark; the page counts the marked slots.

```razor
<button type="button" class="bowler-not-found" @onclick="OnToggle">
    @(IsMarked ? "Undo" : "Mark as not in our system")
</button>

@code {
    [Parameter]
    public bool IsMarked { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnToggle { get; set; }
}
```

`Tournaments/TeamEligibility/BowlerPicker.razor` (new): one slot.

```razor
@inject ITeamEligibilityService TeamEligibilityService
@implements IDisposable

<div class="bowler-picker">
    <label for="@Id" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">@Label</label>

    @if (IsUnknown)
    {
        <div class="bowler-picker__unknown">
            <span class="bowler-picker__tag">
                <span class="material-symbols-outlined" aria-hidden="true">person_off</span>
                Not in our system
            </span>
            <BowlerNotFoundControl IsMarked="true" OnToggle="@(() => SetUnknownAsync(false))" />
        </div>
    }
    else
    {
        <NebaAutocomplete Id="@Id"
                          TValue="string"
                          TItem="BowlerSearchResultViewModel"
                          Value="@Selected?.BowlerId"
                          ValueChanged="HandleSelectedAsync"
                          Items="@_results"
                          ItemsAreFiltered="true"
                          DisplayText="@(bowler => bowler.DisplayText)"
                          ItemValue="@(bowler => bowler.BowlerId)"
                          Placeholder="Search by name..."
                          EmptyLabel="Search by name..."
                          NoResultsText="@NoResultsText"
                          FilterTextChanged="SearchAsync"
                          Disabled="@Disabled">
            <NoResultsContent>
                @if (_searchText.Length >= MinimumSearchLength && !_searchFailed)
                {
                    <BowlerNotFoundControl IsMarked="false" OnToggle="@(() => SetUnknownAsync(true))" />
                }
            </NoResultsContent>
        </NebaAutocomplete>

        <BowlerNotFoundControl IsMarked="false" OnToggle="@(() => SetUnknownAsync(true))" />
    }
</div>

@code {
    private const int MinimumSearchLength = 3;
    private const int DebounceMilliseconds = 300;

    private List<BowlerSearchResultViewModel> _results = [];
    private string _searchText = string.Empty;
    private bool _searchFailed;
    private CancellationTokenSource? _debounce;

    [Parameter, EditorRequired]
    public string Id { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    [Parameter]
    public BowlerSearchResultViewModel? Selected { get; set; }

    [Parameter]
    public EventCallback<BowlerSearchResultViewModel?> SelectedChanged { get; set; }

    [Parameter]
    public bool IsUnknown { get; set; }

    [Parameter]
    public EventCallback<bool> IsUnknownChanged { get; set; }

    /// <summary>Bowlers already chosen in the other slots, so one bowler cannot fill two.</summary>
    [Parameter]
    public IReadOnlySet<string> ExcludedIds { get; set; } = new HashSet<string>();

    [Parameter]
    public bool Disabled { get; set; }

    private string NoResultsText => _searchFailed
        ? "Search is unavailable right now. Try again in a moment."
        : _searchText.Length < MinimumSearchLength
            ? "Type at least 3 letters to search."
            : "No bowler found.";

    protected override void OnParametersSet()
    {
        // NebaAutocomplete shows the chosen bowler by finding it in Items, so keep it there.
        if (Selected is not null && _results.All(bowler => bowler.BowlerId != Selected.BowlerId))
        {
            _results = [Selected];
        }
    }

    private async Task SearchAsync(string text)
    {
        _searchText = text.Trim();
        _debounce?.Cancel();
        _debounce?.Dispose();

        if (_searchText.Length < MinimumSearchLength)
        {
            _searchFailed = false;
            _results = Selected is null ? [] : [Selected];
            return;
        }

        var cts = _debounce = new CancellationTokenSource();

        try
        {
            await Task.Delay(DebounceMilliseconds, cts.Token);
            var result = await TeamEligibilityService.SearchBowlersAsync(_searchText, cts.Token);

            if (cts.IsCancellationRequested)
            {
                return;
            }

            _searchFailed = result.IsError;
            _results = result.IsError
                ? []
                : [.. result.Value.Where(bowler => !ExcludedIds.Contains(bowler.BowlerId))];
        }
        catch (OperationCanceledException)
        {
            // A newer keystroke replaced this search.
        }
    }

    private async Task HandleSelectedAsync(string? bowlerId)
    {
        var bowler = bowlerId is null ? null : _results.FirstOrDefault(b => b.BowlerId == bowlerId);
        _results = bowler is null ? [] : [bowler];
        await SelectedChanged.InvokeAsync(bowler);
    }

    private async Task SetUnknownAsync(bool unknown)
    {
        _results = [];
        _searchText = string.Empty;
        await SelectedChanged.InvokeAsync(null);
        await IsUnknownChanged.InvokeAsync(unknown);
    }

    public void Dispose()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
    }
}
```

`Tournaments/TeamEligibility/TeamRules.razor` (new): the composition criteria and champion definition, inline on the page.

```razor
@using ErrorOr
@using Neba.Website.Server.Documents
@implements IDisposable

@inject IRulesSectionService RulesSectionService
@inject ILogger<TeamRules> Logger

@if (_anchor is not null)
{
    <section class="team-rules" aria-labelledby="team-rules-heading">
        <h2 id="team-rules-heading" class="team-rules__title">Team rules</h2>

        @if (_isLoading)
        {
            <p class="team-rules__status">Loading the rules...</p>
        }
        else if (_content is { } content)
        {
            <div class="team-rules__body">@content</div>
        }
        else
        {
            <p class="team-rules__status">
                We couldn't load the rules. <a class="neba-link" href="@_anchor.FullRulesHref">Read the full rules</a>.
            </p>
        }
    </section>
}

@code {
    private TeamRulesAnchor? _anchor;
    private string? _loadedFor;
    private bool _isLoading;
    private MarkupString? _content;
    private readonly CancellationTokenSource _cts = new();

    [Parameter, EditorRequired]
    public string TournamentType { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedFor == TournamentType)
        {
            return;
        }

        _loadedFor = TournamentType;
        _content = null;
        _anchor = TournamentRulesAnchors.TryGetTeamRules(TournamentType, out var anchor) ? anchor : null;

        if (_anchor is null)
        {
            return;
        }

        _isLoading = true;

        try
        {
            var result = await RulesSectionService.GetTeamRulesAsync(_anchor, _cts.Token);
            // The extracted HTML comes from the API's own rules document, as in TournamentRulesLink.
            _content = result.IsError ? null : new MarkupString(result.Value.Html);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // The component was disposed; nothing left to update.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[TeamRules] Failed to load the team rules for {TournamentType}.", TournamentType);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
```

`Tournaments/TeamEligibility/TeamEligibilityResultPanel.razor` (new): the four outcomes.

```razor
<section class="result @CssClass">
    @switch (Result.Outcome)
    {
        case TeamEligibilityOutcomeKind.DefendingChampions:
            <div class="result__headline">
                <span class="material-symbols-outlined" aria-hidden="true">shield</span>
                These are the defending champions.
            </div>
            <p>@DefendingExplanation</p>
            break;

        case TeamEligibilityOutcomeKind.Eligible:
            <div class="result__headline">
                <span class="material-symbols-outlined" aria-hidden="true">check_circle</span>
                This team can bowl together.
            </div>
            <p>We checked NEBA titles, age, and Hall of Fame membership. Everyone qualifies.</p>
            break;

        case TeamEligibilityOutcomeKind.DependsOnDate:
            <div class="result__headline">
                <span class="material-symbols-outlined" aria-hidden="true">event</span>
                The answer depends on the squad date.
            </div>
            <p>This team can bowl together on some days of the tournament but not others.</p>
            <div class="result__dates">
                @foreach (var check in Result.DateChecks)
                {
                    <div class="result__date @(check.Eligible ? "result__date--good" : "result__date--bad")">
                        <span class="result__when">@check.AsOf.ToString("ddd, MMM d, yyyy", CultureInfo.CurrentCulture)</span>
                        <span>@(check.Eligible ? "Can bowl together" : "Can't bowl together. " + string.Join(" ", check.Reasons))</span>
                    </div>
                }
            </div>
            break;

        default:
            <div class="result__headline">
                <span class="material-symbols-outlined" aria-hidden="true">block</span>
                This team can't bowl together.
            </div>
            <ul>
                @foreach (var reason in Result.Reasons)
                {
                    <li>@reason</li>
                }
            </ul>
            break;
    }

    @if (Result.ShowProfessionalTitlesNote)
    {
        <p class="result__note">
            We can't check PBA or PWBA titles. Make sure no one on the team has a professional title inside the champion window.
        </p>
    }

    @if (Result.Outcome is TeamEligibilityOutcomeKind.Ineligible or TeamEligibilityOutcomeKind.DependsOnDate)
    {
        <p class="result__contact">
            If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.
        </p>
    }

    <button type="button" class="neba-btn neba-btn-secondary neba-btn-sm" @onclick="OnCheckAnother">Check another team</button>
</section>

@code {
    [Parameter, EditorRequired]
    public TeamEligibilityResultViewModel Result { get; set; } = default!;

    [Parameter]
    public EventCallback OnCheckAnother { get; set; }

    private string CssClass => Result.Outcome switch
    {
        TeamEligibilityOutcomeKind.DefendingChampions => "result--defend",
        TeamEligibilityOutcomeKind.Eligible => "result--ok",
        TeamEligibilityOutcomeKind.DependsOnDate => "result--date",
        _ => "result--no",
    };

    // Names the tournament they won and says why a truncated chain still counts.
    private string DefendingExplanation
    {
        get
        {
            var source = Result.DefendingSource;
            if (source is null)
            {
                return "The champion limits do not apply.";
            }

            var won = "This team won " + source.TournamentName + " (" + source.SeasonDescription + ").";

            return source.Undefended
                ? won + " A later tournament was truncated, so they never got to defend. The champion limits do not apply."
                : source.SourceTruncated
                    ? won + " That tournament was truncated, and they still count as defending champions. The champion limits do not apply."
                    : won + " The champion limits do not apply.";
        }
    }
}
```

`Tournaments/DefendingChampions.razor` (new, chosen option 2): the strip on a list card and the panel in the detail hero. Callers render it only when `ShowDefendingChampions` is true, so a completed, truncated, or cancelled tournament shows no defending strip.

```razor
@if (Teams.Count > 0)
{
    @if (Layout == DefendingChampionsLayout.Panel)
    {
        <div class="defending defending--panel">
            <h3 class="defending__title">
                <span class="material-symbols-outlined" aria-hidden="true">shield</span>
                @Heading
            </h3>
            @foreach (var team in Teams)
            {
                <div class="defending__row">
                    <div class="defending__season">
                        @team.SeasonDescription
                        @if (team.Note is not null)
                        {
                            <small>@team.Note</small>
                        }
                    </div>
                    <div class="defending__names">@string.Join(" / ", team.Champions)</div>
                </div>
            }
        </div>
    }
    else
    {
        <div class="defending defending--strip">
            <span class="defending__label">
                <span class="material-symbols-outlined" aria-hidden="true">shield</span>
                @Heading
            </span>
            @foreach (var team in Teams)
            {
                <div class="defending__group">
                    <div class="defending__pills">
                        @foreach (var champion in team.Champions)
                        {
                            <span class="defending__pill">@champion</span>
                        }
                    </div>
                    <span class="defending__tag">
                        @team.SeasonDescription winners@(team.Note is null ? string.Empty : ", " + team.Note)
                    </span>
                </div>
            }
        </div>
    }
}

@code {
    [Parameter, EditorRequired]
    public IReadOnlyList<DefendingTeamViewModel> Teams { get; set; } = default!;

    [Parameter]
    public DefendingChampionsLayout Layout { get; set; } = DefendingChampionsLayout.Strip;

    // Plural for any team of more than one bowler, and for two teams after a truncation.
    private string Heading => Teams.Count > 1 || Teams.Any(team => team.Champions.Count > 1)
        ? "Defending champions"
        : "Defending champion";
}
```

```csharp
// Tournaments/DefendingChampionsLayout.cs
public enum DefendingChampionsLayout { Strip, Panel }
```

`Tournaments/PreviousTournamentPicker.razor` (new): a plain `select` on its own row, laid out like the other fields. It is optional, so it carries no "(required)" tag. It does not use `InputSelect`, so it does not touch the form's `EditContext`; the page marks the form dirty from `ValueChanged`.

```razor
@using System.Globalization
@using Neba.Website.Server.Tournaments.Schedule
@inject ITournamentApiService TournamentApiService

@if (_visible)
{
    <div>
        <label for="@Id" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Previous tournament</label>
        <select id="@Id" class="neba-select" aria-describedby="@(Id + "-help")" @onchange="HandleChangedAsync">
            <option value="" selected="@(Value is null)">None</option>
            @if (Value is not null && _candidates.All(candidate => candidate.Id != Value))
            {
                @* A saved choice outside the three-season window stays selected until the user changes it. *@
                <option value="@Value" selected>Saved selection</option>
            }
            @foreach (var candidate in _candidates)
            {
                <option value="@candidate.Id" selected="@(candidate.Id == Value)">@LabelFor(candidate)</option>
            }
        </select>
        <p id="@(Id + "-help")" class="text-sm text-[var(--neba-gray-500)] mt-1">
            Last year's tournament in this format. Its winners show as the defending champions and can bowl
            together as a team. Leave empty if there is none.
        </p>
        @if (_loadFailed)
        {
            <p class="text-sm text-red-600 mt-1" role="alert">We couldn't load earlier tournaments. Reload the page to try again.</p>
        }
        @if (_notice is not null)
        {
            <p class="text-sm mt-1" role="status">@_notice</p>
        }
    </div>
}

@code {
    private const string NonChampionsType = "Non-Champions";

    private IReadOnlyList<SeasonViewModel>? _seasons;
    private readonly Dictionary<string, List<SeasonTournamentViewModel>> _bySeason = [];
    private List<SeasonTournamentViewModel> _candidates = [];
    private bool _visible;
    private bool _loadFailed;
    private string? _notice;
    private string? _lastType;
    private DateOnly? _lastStart;
    private bool _initialized;

    [Parameter, EditorRequired]
    public string Id { get; set; } = default!;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public string TournamentType { get; set; } = default!;

    [Parameter]
    public DateOnly? StartDate { get; set; }

    /// <summary>Id of the tournament being edited, so it is never offered as its own previous tournament.</summary>
    [Parameter]
    public string? SelfId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _visible = TournamentType != NonChampionsType;
        var changed = _initialized && (_lastType != TournamentType || _lastStart != StartDate);
        var typeChanged = _lastType != TournamentType;
        _lastType = TournamentType;
        _lastStart = StartDate;
        _initialized = true;

        if (!_visible)
        {
            _notice = null;
            if (Value is not null)
            {
                await ValueChanged.InvokeAsync(null);
            }
            return;
        }

        await LoadCandidatesAsync();

        // Only a change the user just made clears the saved value; opening the form never does.
        if (changed && Value is not null && _candidates.All(candidate => candidate.Id != Value))
        {
            _notice = typeChanged
                ? "The previous tournament was cleared. It has to be the same format as this tournament."
                : "The previous tournament was cleared. It has to end before this tournament starts.";
            await ValueChanged.InvokeAsync(null);
        }
    }

    // Candidates: same type, ended before this one starts, from the three seasons before the start date.
    private async Task LoadCandidatesAsync()
    {
        _loadFailed = false;

        if (StartDate is not { } start)
        {
            _candidates = [];
            return;
        }

        if (_seasons is null)
        {
            var seasons = await TournamentApiService.GetSeasonsAsync();
            if (seasons.IsError)
            {
                _loadFailed = true;
                _candidates = [];
                return;
            }

            _seasons = seasons.Value;
        }

        var prior = _seasons.Where(season => season.EndDate < start)
            .OrderByDescending(season => season.EndDate)
            .Take(3)
            .ToList();

        foreach (var season in prior.Where(season => !_bySeason.ContainsKey(season.Id)))
        {
            var tournaments = await TournamentApiService.GetTournamentsForSeasonAsync(season);
            if (tournaments.IsError)
            {
                _loadFailed = true;
                _candidates = [];
                return;
            }

            _bySeason[season.Id] = tournaments.Value;
        }

        // A cancelled tournament has no winners, so it never defends anything.
        _candidates =
        [
            .. prior.SelectMany(season => _bySeason[season.Id])
                .Where(t => t.TournamentType == TournamentType && t.EndDate < start && t.Id != SelfId && !t.IsCancelled)
                .OrderByDescending(t => t.StartDate)
        ];
    }

    private Task HandleChangedAsync(ChangeEventArgs e)
    {
        _notice = null;
        var id = e.Value?.ToString();
        return ValueChanged.InvokeAsync(string.IsNullOrEmpty(id) ? null : id);
    }

    // "Joe's Pizza Open (02/2026)": the month and year of the start date tell two years of one event apart.
    private static string LabelFor(SeasonTournamentViewModel tournament)
        => tournament.Name + " (" + tournament.StartDate.ToString("MM/yyyy", CultureInfo.InvariantCulture) + ")";
}
```

### Pages

`Tournaments/TeamEligibility/TeamEligibility.razor` (new), route `/tournaments/{Id}/team-eligibility`. No `DirtyFormGuard` (nothing is saved, so a leave warning would only annoy; a deliberate exception to the data-entry rule) and no `FormLabel` (every slot is required, so one line says so).

```razor
@page "/tournaments/{Id}/team-eligibility"
@rendermode InteractiveServer
@using ErrorOr
@using Neba.Api.Contracts.Tournaments
@using Neba.Website.Server.Services
@using Neba.Website.Server.Tournaments.Detail

@inject ApiExecutor ApiExecutor
@inject ITournamentsApi TournamentsApi
@inject ITeamEligibilityService TeamEligibilityService
@inject NavigationManager Navigation

<PageTitle>Check Team Eligibility - BowlNEBA</PageTitle>

<div class="team-eligibility">
    @if (_isLoading)
    {
        <p>Loading...</p>
    }
    else if (_loadFailed || _model is null)
    {
        <NebaAlert Severity="NotifySeverity.Error" Title="Unable to load this tournament" Message="Try again in a moment." />
    }
    else if (_model.TeamSize < 2)
    {
        <h1>This is not a team event</h1>
        <p><a class="neba-link" href="/tournaments/@_model.Id">Back to @_model.Name</a></p>
    }
    else
    {
        <a class="neba-link" href="/tournaments/@_model.Id">← @_model.Name</a>
        <h1>Check team eligibility</h1>
        <p class="team-eligibility__lead">@_model.Name · @_model.TournamentType · @_model.FormatDateRange()</p>

        <TeamRules TournamentType="@_model.TournamentType" />

        <section class="team-eligibility__slots" aria-labelledby="team-heading">
            <h2 id="team-heading">Your team</h2>
            <p>
                All @_model.TeamSize bowlers are required. Search by name, then pick the right bowler by USBC ID.
            </p>

            @for (var i = 0; i < _slots.Count; i++)
            {
                var index = i;
                <BowlerPicker Id="@("bowler-" + (index + 1))"
                              Label="@("Bowler " + (index + 1))"
                              Selected="@_slots[index].Bowler"
                              SelectedChanged="@(bowler => SelectBowler(index, bowler))"
                              IsUnknown="@_slots[index].IsUnknown"
                              IsUnknownChanged="@(unknown => SetUnknown(index, unknown))"
                              ExcludedIds="@ChosenIds"
                              Disabled="@_isChecking" />
            }

            <button type="button" class="neba-btn neba-btn-primary" disabled="@(!CanCheck)" @onclick="CheckAsync">
                Check team
            </button>
        </section>

        <div aria-live="polite" class="team-eligibility__result">
            @if (_isChecking)
            {
                <p>Checking...</p>
            }
            else if (_errorMessage is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to check this team" Message="@_errorMessage" />
            }
            else if (_result is not null)
            {
                <TeamEligibilityResultPanel Result="@_result" OnCheckAnother="ClearResult" />
            }
        </div>
    }
</div>

@code {
    private sealed class Slot
    {
        public BowlerSearchResultViewModel? Bowler { get; set; }
        public bool IsUnknown { get; set; }
    }

    private readonly List<Slot> _slots = [];
    private TournamentDetailViewModel? _model;
    private bool _isLoading = true;
    private bool _loadFailed;
    private bool _isChecking;
    private string? _errorMessage;
    private TeamEligibilityResultViewModel? _result;

    [Parameter]
    public string Id { get; set; } = string.Empty;

    private IReadOnlySet<string> ChosenIds =>
        _slots.Where(slot => slot.Bowler is not null).Select(slot => slot.Bowler!.BowlerId).ToHashSet(StringComparer.Ordinal);

    private bool CanCheck => !_isChecking && _slots.All(slot => slot.Bowler is not null || slot.IsUnknown);

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync(
            "TournamentsApi",
            "GetTournamentDetail",
            ct => TournamentsApi.GetTournamentAsync(Id, ct));

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                Navigation.NavigateTo("/not-found", replace: true);
                return;
            }

            _loadFailed = true;
            _isLoading = false;
            return;
        }

        _model = result.Value.ToViewModel();
        _slots.AddRange(Enumerable.Range(0, _model.TeamSize).Select(_ => new Slot()));
        _isLoading = false;
    }

    private void SelectBowler(int index, BowlerSearchResultViewModel? bowler)
    {
        _slots[index].Bowler = bowler;
        ClearResult();
    }

    private void SetUnknown(int index, bool unknown)
    {
        _slots[index].IsUnknown = unknown;
        _slots[index].Bowler = null;
        ClearResult();
    }

    // "Check another team" and any edit to a slot clear the old answer but keep the slots.
    private void ClearResult()
    {
        _result = null;
        _errorMessage = null;
    }

    private async Task CheckAsync()
    {
        ClearResult();
        _isChecking = true;

        try
        {
            var bowlerIds = _slots.Where(slot => slot.Bowler is not null).Select(slot => slot.Bowler!.BowlerId).ToList();
            var unknownCount = _slots.Count(slot => slot.IsUnknown);

            var result = await TeamEligibilityService.CheckAsync(Id, bowlerIds, unknownCount);

            if (result.IsError)
            {
                _errorMessage = result.FirstError.Type == ErrorType.Validation
                    ? result.FirstError.Description
                    : "We couldn't check this team. Try again, or email info@bowlneba.com.";
                return;
            }

            _result = result.Value;
        }
        finally
        {
            _isChecking = false;
        }
    }
}
```

`Tournaments/CreateTournament.razor` and `EditTournament.razor` (edit): the picker sits on its own row in Basic Info, after the type and date row.

```razor
<div>
    <PreviousTournamentPicker Id="previous-tournament"
                              Value="@_model.PreviousTournamentId"
                              ValueChanged="HandlePreviousTournamentChanged"
                              TournamentType="@_model.TournamentType"
                              StartDate="@_model.StartDate"
                              SelfId="@Id" />   @* SelfId only on the edit page *@
</div>
```

```csharp
// both form models
public string? PreviousTournamentId { get; set; }

// both pages
private void HandlePreviousTournamentChanged(string? previousTournamentId)
{
    _model.PreviousTournamentId = previousTournamentId;
    MarkDirty(); // not bound through an InputBase, so the page marks the form dirty itself
}

// BuildTournamentInputAsync, both pages
PreviousTournamentId = string.IsNullOrWhiteSpace(_model.PreviousTournamentId) ? null : _model.PreviousTournamentId,

// EditTournament load, next to the other fields
_model.PreviousTournamentId = _tournament.PreviousTournamentId;
```

A save the API rejects (for example a previous tournament of another type) already shows through the pages' existing `_errorMessage` alert, so no new error handling is needed.

`Tournaments/Detail/TournamentDetail.razor` (edit)

```razor
@* chips row, after TournamentRulesLink *@
@if (_model.ShowTeamEligibilityLink)
{
    <a class="td-team-link" href="/tournaments/@_model.Id/team-eligibility">
        <span class="material-symbols-outlined" aria-hidden="true">groups</span>
        Check team eligibility
    </a>
}

@* hero, right after the chips div and before the truncated note *@
@if (_model.ShowDefendingChampions)
{
    <DefendingChampions Teams="@_model.DefendingChampions" Layout="DefendingChampionsLayout.Panel" />
}
```

`Tournaments/Detail/TournamentDetail.razor.css` (edit): `.td-team-link` copies `.td-rules-link` from `TournamentRulesLink.razor.css` (white underlined link, gold focus ring). That file's rules are scoped to its own component, so the hero needs its own copy.

`Tournaments/Schedule/TournamentUpcomingCard.razor` (edit). The card is not itself a link (only the title and "View Details" are anchors), so the new link is valid HTML.

```razor
@* actions column, in the non-cancelled branch after "View Details" *@
@if (Tournament.ShowTeamEligibilityLink)
{
    <a href="/tournaments/@Tournament.Id/team-eligibility" class="neba-link tournament-upcoming-card__details-link">
        <span class="material-symbols-outlined" aria-hidden="true">groups</span>
        Check team eligibility
    </a>
}

@* inside .neba-card, after the closing tag of the grid div *@
@if (Tournament.ShowDefendingChampions)
{
    <DefendingChampions Teams="@Tournament.DefendingChampions" Layout="DefendingChampionsLayout.Strip" />
}
```

Add `@using Neba.Website.Server.Tournaments` to the card. `TournamentPastCard.razor` does not change: completed, truncated, and cancelled tournaments show winners as they do today, with no defending strip.

CSS for the new components is ported from the mockups (`shared/checker.css` for the checker, result, and picker; `defending-option-2.html` for the strip and panel) into scoped `.razor.css` files using the site's tokens. It is mechanical, so it is not repeated here.

### UI tests

- **bUnit** (internal component logic; `Neba.Website.Tests`):
  - `TeamEligibilityTests`: one picker per team size (2, 3, 5), button enabled only when complete, each result state renders, the contact line shows only for ineligible, not-a-team tournament, tournament not found, a failed check shows an error message.
  - `BowlerPickerTests`: no search under 3 characters, debounce, excluded bowlers hidden, name-only result when there is no USBC ID.
  - `BowlerNotFoundControlTests`, `TeamEligibilityResultPanelTests`, `TeamRulesTests`, `DefendingChampionsTests` (none, one team, two teams after a truncation, strip and panel layouts, plural heading).
  - Detail and card tests: the defending line shows for a scheduled tournament and not for completed, truncated, or cancelled ones.
  - `PreviousTournamentPickerTests`: candidates filtered by type and date, cancelled tournaments left out, labels read "Name (MM/yyyy)", hidden for Non-Champions, cleared with a notice on a type or date change, a saved value kept when the form opens.
  - Extend `CreateTournamentTests`, `EditTournamentTests`, the card and detail tests (defending line, link visibility), `TournamentApiServiceTests`, and add `TeamEligibilityServiceTests`.
  - `RulesSectionExtractorTests` (sub-rule cut, missing sub-rule, two sub-rules joined) and `TournamentRulesAnchorsTests` (mapping per type).
  - `NebaAutocomplete` bUnit tests for `ItemsAreFiltered` (no local filtering, list refreshes when `Items` changes) and `NoResultsContent`.
- **Sample test, built from the live rules HTML shape** (`RulesSectionExtractorTests`):

```csharp
// Shape of the real document: lettered sub-rules are h4 headings with opaque ids, 402b has h5 children.
private const string TeamRulesHtml =
    "<h3 id=\"h.gfg1eow43b1z\">Rule 401 - Doubles Tournament Format</h3>" +
    "<h4 id=\"h.i34k8d87vsx\">401a. Team Composition</h4><p>Entries are two-person teams.</p>" +
    "<h4 id=\"h.6b0g84ucy713\">401b. Champion Definition</h4><p>A champion is a title holder.</p>" +
    "<h4 id=\"h.8mrk89jd6fnp\">401c. Re-Entries</h4><p>Re-entries are permitted.</p>" +
    "<h3 id=\"h.pe9rxguiua35\">Rule 402 - Trios Tournament Format</h3>" +
    "<h4 id=\"h.ssua519ioj21\">402b. Champion Definition</h4>" +
    "<h5 id=\"h.78xuauepv9sy\">One Time Champion</h5><p>Won a single title.</p>" +
    "<h4 id=\"h.axsjlqpio9vu\">402c. Qualifying Format</h4><p>Qualifying text.</p>";

[Fact(DisplayName = "ExtractSubRule cuts a lettered sub-rule up to the next heading of the same level")]
public void ExtractSubRule_ShouldStopAtNextSubRule_WhenSubRuleFound()
{
    // Arrange
    var subRule = new SubRuleRef(401, 'a', "Team Composition");

    // Act
    var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, "/tournaments/rules", includeStyles: false);

    // Assert
    html.ShouldNotBeNull();
    html.ShouldContain("Entries are two-person teams.");
    html.ShouldNotContain("A champion is a title holder.");
}

[Fact(DisplayName = "ExtractSubRule keeps the h5 children of a sub-rule")]
public void ExtractSubRule_ShouldKeepChildHeadings_WhenSubRuleHasThem()
{
    // Arrange
    var subRule = new SubRuleRef(402, 'b', "Champion Definition");

    // Act
    var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, "/tournaments/rules", includeStyles: false);

    // Assert
    html.ShouldNotBeNull();
    html.ShouldContain("Won a single title.");
    html.ShouldNotContain("Qualifying text.");
}

[Fact(DisplayName = "ExtractSubRule returns null when no heading matches")]
public void ExtractSubRule_ShouldReturnNull_WhenSubRuleMissing()
{
    // Arrange
    var subRule = new SubRuleRef(405, 'a', "Team Composition");

    // Act
    var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, "/tournaments/rules", includeStyles: false);

    // Assert
    html.ShouldBeNull();
}
```

- **Playwright** (real browser and HTTP; `tests/e2e`): `TeamEligibility.spec.ts` (search, pick two bowlers, check, see the result; ineligible shows `info@bowlneba.com`; not-found control; defending champions result); extend `TournamentDetail.spec.ts` and `Tournaments.spec.ts` (link and defending line), and `CreateTournament.spec.ts` and `EditTournament.spec.ts` (previous tournament picker).
- **Mock server:** add routes for the two new endpoints and sample defending champions and team size to `tests/e2e/mock-api/mock-api-server.ts`. The new response fields default to empty and are not `required`, so existing specs keep passing.

### Mockups

Static HTML for review, under `docs/plans/mockups/team-eligibility-checker/`. Open each file in a browser. A yellow "Mockup controls" bar at the top of each page switches the format and result states; it is not part of the design. Shared styles use the site's own tokens (`neba_theme.css`) and fonts.

- `checker-option-3.html` (chosen): checker page with a "Mark as not in our system" link under each field and inside the "no bowler found" message. Switch between doubles, over/under, trios, and baker; type 3 or more letters of a name (try "smith", "chris", or "zzz"); pick the result shown by "Check team" to review every result and its wording.
- `defending-option-2.html` (chosen): defending champions as a separate strip on the list card (each name in a pill) and a panel in the detail hero, for singles, doubles, trios, and baker, with a switch for a truncated previous tournament (two teams). Completed, cancelled, and other non-scheduled tournaments show no defending strip and look as they do today.

Wording in the result states (including the age messages) is placeholder text for your review.

## Commit plan

Commit each step as soon as its build and tests pass, in this order. Each commit builds and passes the tests on its own. Every commit message ends with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run the affected tests with the component filter (for example `dotnet test --filter-trait "Component=Tournaments"`) before each commit.

| # | Commit | Contents |
| --- | --- | --- |
| 1 | `Plan the team eligibility checker` | This plan file and `docs/plans/mockups/team-eligibility-checker/` (the two chosen mockups and shared files). |
| 2 | `Add USBC ID to bowlers` | `Bowler.UsbcId`, EF config and index, migration `AddBowlerUsbcId`, `LegacyBowlerRow.UsbcId`, the three legacy bowler jobs, `LegacyBowlerMapping.MapUsbcId`, `Bowler.CreateFromLegacy`/`ApplyLegacyUpdate`, the weekly sync Unchanged check, test factory, tests. |
| 3 | `Let a tournament link to its previous tournament` | `Tournament.PreviousTournamentId` with `ValidatePreviousTournament`, EF config (self-FK, `OnDelete(SetNull)`, index), migration `AddTournamentPreviousTournament`, `PreviousTournamentResolver`, create and edit commands, mappers, `TournamentInput.PreviousTournamentId`, `TournamentDetailResponse.PreviousTournamentId`, factory, tests. |
| 4 | `Add the team eligibility rules` | `TeamEligibilityEvaluator`, `TeamMemberFacts`, `TitleWin`, `TeamEligibilityOutcome`, `TeamEligibilityReasons`, `TeamEligibilityContext`, and the evaluator case table as unit tests. Pure domain code, nothing wired yet. |
| 5 | `Add the team eligibility check endpoint` | `DefendingTeamsReader`, `BowlerTitlesReader`, the `CheckTeamEligibility` use case (request, validator, query, handler, endpoint, summary), contracts, `ITournamentsApi.CheckTeamEligibilityAsync`, handler and endpoint tests. |
| 6 | `Add public bowler search` | `SearchBowlers` use case, `IBowlersApi.SearchBowlersAsync`, `BowlerSearchResponse`, the `bowler-search` rate limit policy, tests. |
| 7 | `Show defending champions on tournament reads` | `TeamSize` and `DefendingChampions` on the list and detail DTOs and contracts, `DefendingTeamResponse`, the two cached reads, `TournamentSuccessorCacheEviction` and its call sites (results sync, truncate, delete, edit, status sync), cache descriptor changes, tests. |
| 8 | `Cut team rules out of the rules document` | `SubRuleRef`, `TeamRulesAnchor`, `TryGetTeamRules`, `RulesSectionExtractor.ExtractSubRule` and `Cut`, `GetTeamRulesAsync`, extractor and anchor tests. |
| 9 | `Add the team eligibility service and autocomplete support` | `NebaAutocomplete` (`ItemsAreFiltered`, `NoResultsContent`) with tests, `ITeamEligibilityService`/`TeamEligibilityService`, result and search view models and mapping, DI registration, service tests. |
| 10 | `Show defending champions and the eligibility link` | `DefendingTeamViewModel` and mapping, `TeamSize`/`DefendingChampions`/`IsScheduled`/`ShowDefendingChampions`/`ShowTeamEligibilityLink` on both view models, `DefendingChampions` component, `TournamentUpcomingCard` and `TournamentDetail` edits, scoped CSS, bUnit tests. |
| 11 | `Pick a previous tournament on the tournament form` | `PreviousTournamentPicker`, create and edit page wiring (`MarkDirty`, input mapping, load), bUnit tests. |
| 12 | `Add the team eligibility page` | `TeamEligibility.razor`, `BowlerPicker`, `BowlerNotFoundControl`, `TeamRules`, `TeamEligibilityResultPanel`, scoped CSS, bUnit tests. |
| 13 | `Add team eligibility end-to-end tests` | Mock server routes and sample data in `tests/e2e/mock-api/mock-api-server.ts`, `TeamEligibility.spec.ts`, and the extended detail, list, create, and edit specs. |
| 14 | `Document the team eligibility checker` | Help doc for the checker page and the previous tournament field (`/help-documentation`), `docs/ubiquitous-language.md` entries (defending champions, previous tournament, team eligibility), and a one-line rule in `CLAUDE.md` if the work taught one. |

Notes:

- Commits 2 and 3 each carry a migration; the self-FK in commit 3 needs a check of the generated SQL before it merges.
- Commit 7 must follow commit 3 (it reads `PreviousTournamentId`) and commit 5 (it reuses `DefendingTeamsReader`).
- Commits 9 to 12 need the contracts from commits 5 to 7; commit 13 needs all of them.
- At the 1.2.0 release: run your LINQPad backfill of `Bowler.UsbcId` after commit 2 ships, and check the release version bump in `Directory.Build.props`.
- Before commit 4's reason wording is final, review the age messages with the team (open item from scoping).
