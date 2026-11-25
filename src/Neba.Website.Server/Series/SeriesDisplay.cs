using System.Globalization;

using Humanizer;

namespace Neba.Website.Server.Series;

internal sealed record AdvancingLabel(string Primary, string? Secondary);

/// <summary>A bold lead and the plain text after it, for summary lines.</summary>
internal sealed record SummaryLine(string Lead, string Detail);

/// <summary>The Series squad a Survivor Stage names.</summary>
internal sealed record SquadSlot(DateOnly? Date, TimeOnly? StartTime);

/// <summary>What a Placement Stage says about the Finale.</summary>
internal sealed record FinaleSlot(int? Count, DateOnly? Date, TimeOnly? StartTime);

/// <summary>One bar in the "How the field narrows" chart.</summary>
/// <param name="Label">The step name, such as "Qualifiers" or a tournament name.</param>
/// <param name="Detail">The line under the name.</param>
/// <param name="Count">How many bowlers are in the field at this step.</param>
/// <param name="Kind">The Stage kind name, used for the bar color.</param>
/// <param name="IsCeiling">True when <paramref name="Count"/> is a most-possible figure.</param>
public sealed record FieldStep(string Label, string Detail, int Count, string Kind, bool IsCeiling);

internal static class SeriesDisplay
{
    private const string DateFormat = "M/d/yyyy";
    private const string TimeFormat = "h:mm tt";
    private const string Bowler = "bowler";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>"1/1/2025 to 12/31/2025".</summary>
    public static string SeasonRange(DateOnly start, DateOnly end)
        => start.ToString(DateFormat, Culture) + " to " + end.ToString(DateFormat, Culture);

    /// <summary>"1/24 to 1/25", or "2/21" for a one-day tournament.</summary>
    public static string TournamentDates(DateOnly start, DateOnly end)
        => start == end
            ? start.ToString("M/d", Culture)
            : start.ToString("M/d", Culture) + " to " + end.ToString("M/d", Culture);

    /// <summary>
    /// "Top 8"; "Top 2 teams" + "(6 bowlers)"; "40 advance"; "Top 4 to the Finale";
    /// "Final standings" + "(no Finale)" for a Placement Stage when the Series has no Finale.
    /// A Placement Stage has no count of its own, so the caller passes the Finale's finalist count.
    /// </summary>
    public static AdvancingLabel Advancing(string kind, int? count, int teamSize, int? finaleCount = null)
        => (kind, count) switch
        {
            (StageKinds.Placement, _) => finaleCount is { } finalists
                ? new AdvancingLabel("Top " + finalists.ToString(Culture) + " to the Finale", null)
                : new AdvancingLabel("Final standings", "(no Finale)"),
            (StageKinds.Qualifying, { } n) when teamSize > 1 => new AdvancingLabel(
                "Top " + "team".ToQuantity(n, formatProvider: DisplayCulture.English),
                "(" + Bowler.ToQuantity((double)n * teamSize, formatProvider: DisplayCulture.English) + ")"),
            (StageKinds.Qualifying, { } n) => new AdvancingLabel("Top " + n.ToString(Culture), null),
            (StageKinds.Survivor, { } n) => new AdvancingLabel(n.ToString(Culture) + " advance", null),
            _ => new AdvancingLabel("—", null), // a saved Qualifying/Survivor Stage always has a count
        };

    /// <summary>"9:00 AM squad on 7/12"; null when the Stage has no squad.</summary>
    public static string? Squad(DateOnly? date, TimeOnly? startTime)
        => date is { } d && startTime is { } t
            ? t.ToString(TimeFormat, Culture) + " squad on " + d.ToString("M/d", Culture)
            : null;

    /// <summary>"Teams of 3" + "(6 bowlers)" under a Qualifying row's advancing input.</summary>
    public static AdvancingLabel? TeamHint(string kind, int? count, int teamSize)
    {
        if (kind != StageKinds.Qualifying || teamSize <= 1)
        {
            return null;
        }

        string? bowlers = null;
        if (count is { } n)
        {
            bowlers = "(" + Bowler.ToQuantity((double)n * teamSize, formatProvider: DisplayCulture.English) + ")";
        }

        return new AdvancingLabel("Teams of " + teamSize.ToString(Culture), bowlers);
    }

    /// <summary>"Sat 9/27 at 2:00 PM": when the Finale is bowled.</summary>
    public static string FinaleWhen(DateOnly date, TimeOnly startTime)
        => date.ToString("ddd M/d", Culture) + " at " + startTime.ToString(TimeFormat, Culture);

    /// <summary>"Callahan's Bowl-O-Rama, Seekonk, MA"; null while the host center is to be announced.</summary>
    public static string? Host(string? name, string? city, string? state)
        => name is null
            ? null
            : string.Join(", ", new[] { name, city, state }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>"Top 4 of the Placement Round go to the Finale".</summary>
    public static string FinaleCaption(int finalistCount)
        => "Top " + finalistCount.ToString(Culture) + " of the Placement Round";

    /// <summary>"Vincent Hall Memorial Series Finale" (the name is always derived, never stored).</summary>
    public static string FinaleName(string seriesName)
        => seriesName + " Finale";

    /// <summary>The most bowlers who can qualify: each Qualifying Stage's count times its team size.</summary>
    public static int QualifierCeiling(IEnumerable<SeriesStageViewModel> stages)
        => stages.Where(s => s.Kind == StageKinds.Qualifying).Sum(s => (s.AdvancingCount ?? 0) * s.TeamSize);

    /// <summary>Steps for "How the field narrows". Only steps that exist are returned.</summary>
    public static IReadOnlyList<FieldStep> FieldSteps(IReadOnlyList<SeriesStageViewModel> stages, SeriesFinaleViewModel? finale)
    {
        var steps = new List<FieldStep>();
        var qualifying = stages.Where(s => s.Kind == StageKinds.Qualifying).ToList();

        if (qualifying.Count > 0)
        {
            steps.Add(new FieldStep("Qualifiers", "event".ToQuantity(qualifying.Count, formatProvider: DisplayCulture.English) + ", top finishers",
                QualifierCeiling(qualifying), StageKinds.Qualifying, IsCeiling: true));
        }

        steps.AddRange(stages
            .Where(s => s.Kind == StageKinds.Survivor && s.AdvancingCount is not null)
            .Select(s => new FieldStep(s.TournamentName, "Survivor · " + TournamentDates(s.TournamentStartDate, s.TournamentEndDate),
                s.AdvancingCount!.Value, StageKinds.Survivor, IsCeiling: false)));

        // No Finale step without a Finale; the chart's caption explains the Placement Round instead.
        if (finale is not null)
        {
            steps.Add(new FieldStep("Finale", FinaleWhen(finale.Date, finale.StartTime),
                finale.FinalistCount, StageKinds.Placement, IsCeiling: false));
        }

        return steps;
    }

    /// <summary>Inline width for a field bar, scaled to the largest step, never below 8%.</summary>
    public static string BarWidthStyle(int count, int max)
        => "width:" + Math.Max(8, max == 0 ? 0 : count * 100 / max).ToString(Culture) + "%";

    /// <summary>"Qualifying Round", "Survivor Round", "Placement Round".</summary>
    public static string RoundLabel(string kind)
        => kind + " Round";

    /// <summary>What the round decides, for the detail callout. Secondary is the "(6 bowlers)" or squad line.</summary>
    public static AdvancingLabel CalloutRule(
        string kind, int? count, int teamSize, SquadSlot squad, FinaleSlot finale)
        => (kind, count) switch
        {
            (StageKinds.Placement, _) when finale.Count is null => new AdvancingLabel("Final standings (no Finale).", null),
            (StageKinds.Placement, _) => new AdvancingLabel(
                "Final standings. The top " + finale.Count.Value.ToString(Culture) + " go to the Finale.",
                finale.Date is { } fd && finale.StartTime is { } ft ? "Finale: " + FinaleWhen(fd, ft) + "." : null),
            (StageKinds.Qualifying, { } n) when teamSize > 1 => new AdvancingLabel(
                "Top " + "team".ToQuantity(n, formatProvider: DisplayCulture.English) + " enter the Series field.",
                "(" + Bowler.ToQuantity((double)n * teamSize, formatProvider: DisplayCulture.English) + ")"),
            (StageKinds.Qualifying, { } n) => new AdvancingLabel("Top " + n.ToString(Culture) + " finishers enter the Series field.", null),
            (StageKinds.Survivor, { } n) => new AdvancingLabel(
                n.ToString(Culture) + " bowlers advance.",
                squad.Date is { } d && squad.StartTime is { } t
                    ? "Series squad: " + t.ToString(TimeFormat, Culture) + " on " + d.ToString("M/d", Culture) + "."
                    : null),
            _ => new AdvancingLabel("Part of the Series.", null), // a saved Qualifying/Survivor Stage always has a count
        };

    public static string KindClass(string kind)
        => kind switch
        {
            StageKinds.Qualifying => "kind-qualifying",
            StageKinds.Survivor => "kind-survivor",
            _ => "kind-placement",
        };

    public static string GroupHeading(string kind)
        => kind switch
        {
            StageKinds.Qualifying => "Qualifying events",
            StageKinds.Survivor => "Survivor rounds",
            _ => "Placement round",
        };

    public static string GroupNote(string kind)
        => kind switch
        {
            StageKinds.Qualifying => "Top finishers enter the Series",
            StageKinds.Survivor => "Cut by the designated squad score",
            _ => "Ranked by tournament position",
        };

    /// <summary>"1 tournament needs changes…" / "2 tournaments need changes…" for the editor banner.</summary>
    public static string RowsNeedChanges(int count)
        => "tournament".ToQuantity(count, formatProvider: DisplayCulture.English) + (count == 1 ? " needs" : " need") + " changes before this Series can be saved.";

    /// <summary>Two initials for a logo-less tile: "2025 Vincent Hall Memorial Series" -> "VH".</summary>
    public static string Initials(string name)
        => string.Concat(name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => char.IsLetter(word[0]))
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));

    /// <summary>"Sat 7/12 · Callahan's Bowl-O-Rama" for the tournament picker.</summary>
    public static string TournamentOption(DateOnly start, string name)
        => start.ToString("ddd M/d", Culture) + " · " + name;

    // ---- Advancement section (Series.EditSeries holders) ----

    public const string AdvancementReloadFailed = "The tournament status could not refresh. Reload the page to see the latest.";
    public const string CancelledStageNote = "Tournament cancelled: remove it from the Series";
    public const string CancelledCheckFailed = "A cancelled tournament can't be removed until this check works. Reload the page to try again.";
    public const string StaleNote = "An earlier tournament changed after this one was official. Its output has not changed. Recompute from here to refresh it.";
    public const string StaleHint = "An earlier tournament changed after this one was official.";

    public const string NoFieldEntries = "No bowlers in the field from this tournament.";
    public const string NoSquads = "No squads yet. They arrive when the desktop software sends this tournament's squads.";
    public const string OverrideRemovedMessage = "Override removed.";
    public const string SlotClearedMessage = "Slot cleared. The Bowler of the Year point list will refill it.";

    private static string Num(int n) => n.ToString(Culture);

    /// <summary>"series-pill series-pill--computed". A Placement Stage never reaches Official.</summary>
    public static string StatusPillClass(string status)
        => status switch
        {
            StageStatuses.Official => "series-pill series-pill--official",
            StageStatuses.Computed => "series-pill series-pill--computed",
            _ => "series-pill series-pill--pending",
        };

    public static string SegmentClass(string status)
        => status switch
        {
            StageStatuses.Official => "series-strip__seg series-strip__seg--official",
            StageStatuses.Computed => "series-strip__seg series-strip__seg--computed",
            _ => "series-strip__seg",
        };

    /// <summary>"3rd tournament: Official".</summary>
    public static string SegmentTitle(int order, string status) => TournamentOrdinal(order) + ": " + status;

    /// <summary>The rail node's class: its status, plus cancelled when the tournament was.</summary>
    public static string NodeClass(string status, bool isCancelled)
    {
        var statusClass = status switch
        {
            StageStatuses.Official => "series-node--official",
            StageStatuses.Computed => "series-node--computed",
            _ => "series-node--pending",
        };

        return "series-node " + statusClass + (isCancelled ? " series-node--cancelled" : string.Empty);
    }

    /// <summary>"Qualifying · 3rd tournament".</summary>
    public static string StageChip(string kind, int order) => kind + " · " + TournamentOrdinal(order);

    /// <summary>"7/12 · 3-bowler teams · Top 2 teams (6 bowlers)".</summary>
    public static string RailMeta(SeriesStageViewModel stage, int? finaleCount)
    {
        var advancing = Advancing(stage.Kind, stage.AdvancingCount, stage.TeamSize, finaleCount);
        var parts = new List<string> { TournamentDates(stage.TournamentStartDate, stage.TournamentEndDate) };
        if (stage.TeamSize > 1)
        {
            parts.Add(Num(stage.TeamSize) + "-bowler teams");
        }

        parts.Add(advancing.Secondary is null ? advancing.Primary : advancing.Primary + " " + advancing.Secondary);
        return string.Join(" · ", parts);
    }

    /// <summary>"7 of 8 slots filled", "40 advance", "20 ranked"; null while Pending.</summary>
    public static string? Headline(StageAdvancementViewModel stage, int positionCount)
    {
        if (stage.Status == StageStatuses.Pending)
        {
            return null;
        }

        if (stage.Kind == StageKinds.Qualifying)
        {
            var slots = stage.SlotCount ?? 0;
            var filled = slots - (stage.VacantSlotCount ?? 0);
            return Num(filled) + " of " + "slot".ToQuantity(slots, formatProvider: DisplayCulture.English) + " filled";
        }

        return stage.Kind == StageKinds.Survivor
            ? Num(stage.Outcomes.Count(o => o.Advanced)) + " advance"
            : Num(positionCount) + " ranked";
    }

    /// <summary>"Bowler of the Year point list as of 6/14/2025"; null until the list was used.</summary>
    public static string? BackfillLabel(DateOnly? asOf)
        => asOf is { } date ? BoyPointList + " as of " + date.ToString(DateFormat, Culture) : null;

    /// <summary>"4 of 11 tournaments" + " computed or official (3 official). Bowler of the Year point list not used yet."</summary>
    public static SummaryLine Progress(IReadOnlyList<StageAdvancementViewModel> stages, DateOnly? backfillAsOf)
    {
        var done = stages.Count(s => s.Status != StageStatuses.Pending);
        var official = stages.Count(s => s.Status == StageStatuses.Official);
        var backfill = backfillAsOf is { } date
            ? BoyPointList + " used as of " + date.ToString(DateFormat, Culture) + "."
            : BoyPointList + " not used yet.";

        return new SummaryLine(
            Num(done) + " of " + "tournament".ToQuantity(stages.Count, formatProvider: DisplayCulture.English),
            " computed or official (" + Num(official) + " official). " + backfill);
    }

    /// <summary>"Hide output for the 3rd tournament".</summary>
    public static string ExpandLabel(bool isExpanded, int order)
        => (isExpanded ? "Hide" : "Show") + " output for the " + TournamentOrdinal(order);

    /// <summary>"Pick squad for the 3rd tournament".</summary>
    public static string PickSquadLabel(int order) => "Pick squad for the " + TournamentOrdinal(order);

    /// <summary>"Add bowler to the 2nd tournament".</summary>
    public static string AddBowlerLabel(int order) => "Add bowler to the " + TournamentOrdinal(order);

    /// <summary>"Clear slot for Marcus Alvarez".</summary>
    public static string ClearSlotLabel(string bowlerName) => "Clear slot for " + bowlerName;

    /// <summary>"Remove override for Marcus Alvarez".</summary>
    public static string RemoveOverrideLabel(string bowlerName) => "Remove override for " + bowlerName;

    /// <summary>"Recompute from the 3rd tournament".</summary>
    public static string RecomputeLabel(int order) => "Recompute from the " + TournamentOrdinal(order);

    /// <summary>Why a Pending Stage shows no output.</summary>
    public static string PendingReason(StageAdvancementViewModel stage) => stage switch
    {
        { IsTournamentCancelled: true } =>
            "This tournament was cancelled. Remove it from the Series on the Edit page, then recompute from the tournament before it.",
        { NeedsSquad: true } =>
            "Waiting for a squad. Pick the squad this round uses and the tournament computes.",
        _ when stage.Kind == StageKinds.Placement =>
            "Positions appear when the Placement Round computes.",
        _ => "Not computed yet. This tournament computes when it completes.",
    };

    /// <summary>"8 slots" (the bold lead of the Qualifying summary).</summary>
    public static string SlotsLead(int slots) => "slot".ToQuantity(slots, formatProvider: DisplayCulture.English);

    /// <summary>", 2 vacant slots. Vacant slots go to the Bowler of the Year point list." or ", none vacant."</summary>
    public static string VacantLabel(int vacant)
        => vacant > 0
            ? ", " + "vacant slot".ToQuantity(vacant, formatProvider: DisplayCulture.English) + ". Vacant slots go to the Bowler of the Year point list."
            : ", none vacant.";

    /// <summary>"Advanced in 1st tournament": the source of a bowler inside the cut who already advanced.</summary>
    public static string AlreadyAdvancedSource(int earlierStageOrder)
        => "Advanced in " + TournamentOrdinal(earlierStageOrder);

    /// <summary>"Field entries from this tournament, then the Bowler of the Year point list".</summary>
    public static string EntriesHeading(bool isLastQualifying)
        => "Field entries from this tournament" + (isLastQualifying ? ", then the Bowler of the Year point list" : string.Empty);

    /// <summary>"3rd", or a dash for a point-list entry.</summary>
    public static string OrdinalPlace(int? place) => place is { } p ? p.ToOrdinal() : "—";

    public static string EntrySource(FieldEntryViewModel entry)
        => entry switch
        {
            { ViaOverride: true } => "Added by admin",
            { IsBackfill: true } => BoyPointList,
            _ => "Qualifier",
        };

    public static string SourceClass(FieldEntryViewModel entry)
        => entry switch
        {
            { ViaOverride: true } => "series-src--admin",
            { IsBackfill: true } => "series-src--backfill",
            _ => string.Empty,
        };

    /// <summary>"Bowler of the Year point list", "Advanced in 1st tournament" or "Qualifier" for a bowler on the public Qualifying list.</summary>
    public static string AdvancerSource(AdvancerViewModel advancer)
        => advancer switch
        {
            { AlreadyAdvancedFromOrder: { } order } => "Advanced in " + TournamentOrdinal(order),
            { IsBackfill: true } => BoyPointList,
            _ => "Qualifier",
        };

    /// <summary>The public name for the Backfill.</summary>
    public const string BoyPointList = "Bowler of the Year point list";

    /// <summary>"3rd tournament": the public name for a Stage.</summary>
    public static string TournamentOrdinal(int order) => order.ToOrdinal() + " tournament";

    /// <summary>"Hide results for the 3rd tournament".</summary>
    public static string ResultsToggleLabel(bool isExpanded, int order)
        => (isExpanded ? "Hide" : "Show") + " results for the " + TournamentOrdinal(order);

    /// <summary>"Qualifying results for the 3rd tournament".</summary>
    public static string ResultsTableLabel(string kind, int order) => kind + " results for the " + TournamentOrdinal(order);

    public static string AdvancerSourceClass(AdvancerViewModel advancer)
        => advancer switch
        {
            { AlreadyAdvancedFromOrder: not null } => "series-advancers__already",
            { IsBackfill: true } => "series-advancers__backfill",
            _ => string.Empty,
        };

    /// <summary>Row class for the public lists: dimmed when the bowler did not advance, a dashed line at the Survivor cut.</summary>
    public static string AdvancerRowClass(bool isStanding, AdvancerViewModel advancer, bool isCut)
    {
        if (advancer.Advanced)
        {
            return isStanding ? "series-advancers__in" : string.Empty;
        }

        return isCut ? "series-advancers__out series-advancers__cut" : "series-advancers__out";
    }

    public static string Points(int? points) => points is { } p ? Num(p) : "—";

    public static string BlockTotal(int? total) => total is { } t ? Num(t) : "No full block";

    public static string OverrideChange(string kind) => kind == OverrideKinds.Add ? "Add bowler" : "Clear slot";

    public static string OverrideStatus(bool applied) => applied ? "Applied" : "No longer applies";

    /// <summary>"3/23/2025".</summary>
    public static string AddedDate(DateTimeOffset local) => local.ToString(DateFormat, Culture);

    /// <summary>"12 advance" + " of 40 in the field. 38 bowled; 2 did not."</summary>
    public static SummaryLine SurvivorSummary(IReadOnlyList<SurvivorOutcomeViewModel> outcomes)
    {
        var bowled = outcomes.Count(o => o.BlockTotal is not null);
        return new SummaryLine(
            Num(outcomes.Count(o => o.Advanced)) + " advance",
            " of " + Num(outcomes.Count) + " in the field. " + Num(bowled) + " bowled; " + Num(outcomes.Count - bowled) + " did not.");
    }

    /// <summary>"20 bowlers ranked." + the Finale note.</summary>
    public static SummaryLine PlacementSummary(int ranked, bool hasFinalists)
        => new(Bowler.ToQuantity(ranked, formatProvider: DisplayCulture.English) + " ranked.",
            hasFinalists ? " Finalist positions are provisional until the Finale is recorded." : " These positions are final.");

    /// <summary>"Recompute from the 3rd tournament?"</summary>
    public static string RecomputeTitle(int order) => "Recompute from the " + TournamentOrdinal(order) + "?";

    /// <summary>"1st, 2nd and 3rd".</summary>
    public static string StageWordList(IReadOnlyList<int> orders)
    {
        if (orders.Count == 0)
        {
            return string.Empty;
        }

        var words = orders.Select(order => order.ToOrdinal()).ToList();
        return words.Count == 1
            ? words[0]
            : string.Join(", ", words.Take(words.Count - 1)) + " and " + words[^1];
    }

    /// <summary>"The 3rd and 4th tournaments are official. Recomputing resets them to Computed."</summary>
    public static string OfficialResetMessage(IReadOnlyList<int> orders)
    {
        var one = orders.Count == 1;
        return "The " + StageWordList(orders) + (one ? " tournament is" : " tournaments are") + " official. Recomputing resets " + (one ? "it" : "them") + " to Computed.";
    }

    /// <summary>The toast after Pick squad (the Stage may stay Pending, so it says only that the squad is saved).</summary>
    public const string SquadSavedMessage = "Squad saved.";

    public static string MakeOfficialLabel(int order) => "Mark the " + TournamentOrdinal(order) + " official";

    public static string MakeOfficialTitle(int order) => "Mark the " + TournamentOrdinal(order) + " official?";

    public static string MakeOfficialMessage(int order)
        => "The bowlers who advanced from the " + TournamentOrdinal(order) + " will show on the public Series page. Check the field first. If it is wrong, recompute from this tournament to reopen it.";

    public static string MadeOfficialMessage(int order) => "The " + TournamentOrdinal(order) + " is official.";

    public static string RecomputedMessage(int order) => "Recomputed from the " + TournamentOrdinal(order) + ".";

    public static string RemoveOverrideMessage(StageOverrideViewModel item, int order)
        => item.Kind == OverrideKinds.Add
            ? item.BowlerName + " leaves the field for the " + TournamentOrdinal(order) + "."
            : item.BowlerName + " takes the slot back, and the Bowler of the Year point list no longer refills it.";

    public static string OverrideAddedMessage(string bowlerName, int order) => bowlerName + " added to the " + TournamentOrdinal(order) + ".";

    public static string OverrideTitle(string kind) => kind == OverrideKinds.Add ? "Add bowler" : "Clear slot";

    public static string OverrideLead(string kind, int order, string tournamentName)
        => TournamentOrdinal(order) + ", " + tournamentName + ". "
            + (kind == OverrideKinds.Add
                ? "Pick any bowler with a result in this tournament, whatever the place or membership. The field can then be larger than the target."
                : "The slot goes vacant and the Bowler of the Year point list fills it. This bowler can't come back through that list.");

    /// <summary>"Marcus Alvarez (12th)".</summary>
    public static string ResultOption(string name, int place) => name + " (" + place.ToOrdinal() + ")";

    /// <summary>"7th place".</summary>
    public static string ClearPlace(int? place) => place is { } p ? p.ToOrdinal() + " place" : BoyPointList;

    /// <summary>"120 / 500".</summary>
    public static string ReasonCount(int length) => Num(length) + " / " + Num(StageOverrideFormModel.ReasonMaxLength);

    /// <summary>"9:30 AM on 7/12" (the API already sends Eastern time).</summary>
    public static string SquadOption(DateOnly date, TimeOnly startTime)
        => startTime.ToString(TimeFormat, Culture) + " on " + date.ToString("M/d", Culture);

    /// <summary>"Has scores · Current".</summary>
    public static string SquadDetail(bool hasScores, bool isSelected)
        => (hasScores ? "Has scores" : "No scores yet") + (isSelected ? " · Current" : string.Empty);

    /// <summary>The Pick squad lead. <paramref name="expected"/> is the Series squad, such as "9:00 AM squad on 7/12".</summary>
    public static string SquadLead(int order, string tournamentName, string? expected, bool needsSquad)
        => TournamentOrdinal(order) + ", " + tournamentName + ". The Series expects "
            + (expected is null ? "a squad" : "a " + expected) + "."
            + (needsSquad ? " No squad matches it exactly, so pick the one this round uses." : " Choose the squad this round uses.");

    /// <summary>The locked editor's banner. A cancelled Stage can still go.</summary>
    public static string LockedMessage(bool hasRemovableStage)
        => "Processing has begun for this Series. Its tournaments and whether it has a Finale are fixed. The name, the logo, and the Finale's date, time and center can still change."
            + (hasRemovableStage
                ? " A cancelled tournament can still be removed. After you save, run Recompute from the tournament before it on the Series page."
                : string.Empty);
}