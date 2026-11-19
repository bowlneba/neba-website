using System.Globalization;

namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>
/// A run of upcoming tournaments that belong to one season.
/// </summary>
/// <param name="Season">The season the tournaments belong to.</param>
/// <param name="Tournaments">The tournaments, in start-date order.</param>
/// <param name="ShowDivider">True when a season divider goes above the run.</param>
internal sealed record UpcomingSeasonSection(
    SeasonViewModel Season,
    IReadOnlyList<SeasonTournamentViewModel> Tournaments,
    bool ShowDivider);

/// <summary>
/// One timeline strip: a season and the tournaments it plots.
/// </summary>
/// <param name="Season">The season the strip covers.</param>
/// <param name="Tournaments">The tournaments to plot.</param>
/// <param name="UpcomingOnly">True when every tournament is upcoming, so the strip has no completed dots.</param>
internal sealed record UpcomingTimelineStrip(
    SeasonViewModel Season,
    IReadOnlyList<SeasonTournamentViewModel> Tournaments,
    bool UpcomingOnly);

/// <summary>
/// A run of upcoming entries (tournaments and Finales) that belong to one season.
/// </summary>
/// <param name="Season">The season the entries belong to.</param>
/// <param name="Entries">The entries, in date order.</param>
/// <param name="ShowDivider">True when a season divider goes above the run.</param>
internal sealed record UpcomingEntrySection(
    SeasonViewModel Season,
    IReadOnlyList<ScheduleEntry> Entries,
    bool ShowDivider);

internal static class UpcomingSchedule
{
    /// <summary>
    /// Puts tournaments and Finales in one list, ordered by date (then Finale start time).
    /// </summary>
    /// <param name="tournaments">The tournaments to show.</param>
    /// <param name="finales">The Finales to show.</param>
    /// <param name="newestFirst">True to order the latest date first.</param>
    public static List<ScheduleEntry> Merge(
        IEnumerable<SeasonTournamentViewModel> tournaments,
        IEnumerable<SeriesFinaleItemViewModel> finales,
        bool newestFirst)
    {
        var entries = tournaments.Select(ScheduleEntry.ForTournament)
            .Concat(finales.Select(ScheduleEntry.ForFinale));

        return newestFirst
            ? [.. entries.OrderByDescending(e => e.SortDate).ThenByDescending(e => e.SortTime)]
            : [.. entries.OrderBy(e => e.SortDate).ThenBy(e => e.SortTime)];
    }

    /// <summary>
    /// Groups merged entries by season; a Finale sits under the season of its Series. Dividers follow
    /// the same rule as <see cref="GroupBySeason"/>.
    /// </summary>
    /// <param name="entries">The entries to show, in date order.</param>
    /// <param name="seasons">All known seasons.</param>
    /// <param name="leadingSeasonLabel">The season label the full upcoming list starts with.</param>
    public static List<UpcomingEntrySection> GroupEntriesBySeason(
        IReadOnlyList<ScheduleEntry> entries,
        IReadOnlyCollection<SeasonViewModel> seasons,
        string? leadingSeasonLabel)
        => [.. entries
            .GroupBy(e => SeasonLabelOf(e, seasons), StringComparer.Ordinal)
            .Select(group => new UpcomingEntrySection(
                seasons.FirstOrDefault(s => string.Equals(s.Label, group.Key, StringComparison.Ordinal))
                    ?? FromDates(group.Key, group.Select(e => e.SortDate)),
                [.. group],
                ShowDivider: !string.Equals(group.Key, leadingSeasonLabel, StringComparison.Ordinal)))];

    private static string SeasonLabelOf(ScheduleEntry entry, IReadOnlyCollection<SeasonViewModel> seasons)
        => entry.Tournament?.Season
           ?? seasons.FirstOrDefault(s => string.Equals(s.Id, entry.Finale!.SeasonId, StringComparison.Ordinal))?.Label
           ?? ResolveSeasonLabel(entry.SortDate, seasons);

    /// <summary>
    /// Groups upcoming tournaments by season. A divider goes above every season except the one the
    /// full (unfiltered) upcoming list starts with, so a list that starts in next season has no divider.
    /// </summary>
    /// <param name="tournaments">The tournaments to show, in start-date order.</param>
    /// <param name="seasons">All known seasons.</param>
    /// <param name="leadingSeasonLabel">The season label the full upcoming list starts with.</param>
    public static List<UpcomingSeasonSection> GroupBySeason(
        IReadOnlyList<SeasonTournamentViewModel> tournaments,
        IReadOnlyCollection<SeasonViewModel> seasons,
        string? leadingSeasonLabel)
        => [.. tournaments
            .GroupBy(t => t.Season, StringComparer.Ordinal)
            .Select(group => new UpcomingSeasonSection(
                FindSeason(group.Key, seasons, group),
                [.. group],
                ShowDivider: !string.Equals(group.Key, leadingSeasonLabel, StringComparison.Ordinal)))];

    /// <summary>
    /// Builds the timeline strips for the Upcoming tab. While the active season still has upcoming
    /// tournaments it gets a strip with its full schedule, followed by one upcoming-only strip per
    /// later season. Once the active season is finished, the list reads as the next season: one strip
    /// for it and nothing for the finished season.
    /// </summary>
    /// <param name="activeSeason">The current season.</param>
    /// <param name="activeSeasonTournaments">Every tournament in the current season, past and upcoming.</param>
    /// <param name="upcoming">Every upcoming tournament, across seasons, in start-date order.</param>
    /// <param name="seasons">All known seasons.</param>
    public static List<UpcomingTimelineStrip> BuildTimelineStrips(
        SeasonViewModel activeSeason,
        IReadOnlyList<SeasonTournamentViewModel> activeSeasonTournaments,
        IReadOnlyList<SeasonTournamentViewModel> upcoming,
        IReadOnlyCollection<SeasonViewModel> seasons)
    {
        var sections = GroupBySeason(upcoming, seasons, leadingSeasonLabel: null);
        var activeSeasonIsFinished = upcoming.Count > 0
            && !upcoming.Any(t => string.Equals(t.Season, activeSeason.Label, StringComparison.Ordinal));

        if (activeSeasonIsFinished)
        {
            return [.. sections.Select(s => new UpcomingTimelineStrip(s.Season, s.Tournaments, UpcomingOnly: true))];
        }

        List<UpcomingTimelineStrip> strips = [new(activeSeason, activeSeasonTournaments, UpcomingOnly: false)];
        strips.AddRange(sections
            .Where(s => !string.Equals(s.Season.Label, activeSeason.Label, StringComparison.Ordinal))
            .Select(s => new UpcomingTimelineStrip(s.Season, s.Tournaments, UpcomingOnly: true)));
        return strips;
    }

    private static SeasonViewModel FindSeason(
        string label, IReadOnlyCollection<SeasonViewModel> seasons, IEnumerable<SeasonTournamentViewModel> tournaments)
    {
        var match = seasons.FirstOrDefault(s => string.Equals(s.Label, label, StringComparison.Ordinal));
        if (match is not null)
        {
            return match;
        }

        // A tournament whose season is missing from the season list: describe it from its own dates.
        return FromDates(label, tournaments.Select(t => t.StartDate));
    }

    private static SeasonViewModel FromDates(string label, IEnumerable<DateOnly> dateSequence)
    {
        var dates = dateSequence.ToList();
        return new SeasonViewModel
        {
            Id = string.Empty,
            Description = label + " Season",
            StartDate = dates.Min(),
            EndDate = dates.Max(),
        };
    }

    /// <summary>
    /// Finds the label of the season that contains <paramref name="date"/>; falls back to the date's year.
    /// </summary>
    public static string ResolveSeasonLabel(DateOnly date, IEnumerable<SeasonViewModel> seasons)
        => seasons.FirstOrDefault(s => s.StartDate <= date && date <= s.EndDate)?.Label
           ?? date.Year.ToString(CultureInfo.InvariantCulture);
}