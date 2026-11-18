using System.Globalization;

using Humanizer;

namespace Neba.Website.Server.Series;

internal sealed record AdvancingLabel(string Primary, string? Secondary);

internal sealed record FieldStep(string Label, string Detail, int Count, string Kind, bool IsCeiling);

internal static class SeriesDisplay
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>"1/1/2025 to 12/31/2025".</summary>
    public static string SeasonRange(DateOnly start, DateOnly end)
        => start.ToString("M/d/yyyy", Culture) + " to " + end.ToString("M/d/yyyy", Culture);

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
                "Top " + "team".ToQuantity(n),
                "(" + "bowler".ToQuantity(n * teamSize) + ")"),
            (StageKinds.Qualifying, { } n) => new AdvancingLabel("Top " + n.ToString(Culture), null),
            (StageKinds.Survivor, { } n) => new AdvancingLabel(n.ToString(Culture) + " advance", null),
            _ => new AdvancingLabel("—", null), // a saved Qualifying/Survivor Stage always has a count
        };

    /// <summary>"9:00 AM squad on 7/12"; null when the Stage has no squad.</summary>
    public static string? Squad(DateOnly? date, TimeOnly? startTime)
        => date is { } d && startTime is { } t
            ? t.ToString("h:mm tt", Culture) + " squad on " + d.ToString("M/d", Culture)
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
            bowlers = "(" + "bowler".ToQuantity(n * teamSize) + ")";
        }

        return new AdvancingLabel("Teams of " + teamSize.ToString(Culture), bowlers);
    }

    /// <summary>"Sat 9/27 at 2:00 PM": when the Finale is bowled.</summary>
    public static string FinaleWhen(DateOnly date, TimeOnly startTime)
        => date.ToString("ddd M/d", Culture) + " at " + startTime.ToString("h:mm tt", Culture);

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
            steps.Add(new FieldStep("Qualifiers", "event".ToQuantity(qualifying.Count) + ", top finishers",
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

    /// <summary>"1 Stage needs changes…" / "2 Stages need changes…" for the editor banner.</summary>
    public static string RowsNeedChanges(int count)
        => "Stage".ToQuantity(count) + (count == 1 ? " needs" : " need") + " changes before this Series can be saved.";

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
}
