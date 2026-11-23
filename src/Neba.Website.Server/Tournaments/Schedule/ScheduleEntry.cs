namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>One line of the tournament list: a tournament or a Series Finale.</summary>
internal sealed record ScheduleEntry
{
    public SeasonTournamentViewModel? Tournament { get; private init; }

    public SeriesFinaleItemViewModel? Finale { get; private init; }

    /// <summary>A tournament's start date, or a Finale's date.</summary>
    public DateOnly SortDate => Tournament?.StartDate ?? Finale!.Date;

    /// <summary>A Finale's start time; a tournament has none, so it sorts first on its day.</summary>
    public TimeOnly SortTime => Finale?.StartTime ?? TimeOnly.MinValue;

    public static ScheduleEntry ForTournament(SeasonTournamentViewModel tournament)
        => new() { Tournament = tournament };

    public static ScheduleEntry ForFinale(SeriesFinaleItemViewModel finale)
        => new() { Finale = finale };
}