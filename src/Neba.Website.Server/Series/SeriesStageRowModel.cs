namespace Neba.Website.Server.Series;

internal sealed class SeriesStageRowModel
{
    /// <summary>Stable key for Razor <c>@key</c>; never sent to the API.</summary>
    public Guid RowKey { get; } = Guid.NewGuid();

    /// <summary>Existing Stage ID on edit; null for a new row.</summary>
    public string? StageId { get; init; }

    public string Kind { get; set; } = StageKinds.Qualifying;
    public string? TournamentId { get; set; }

    /// <summary>Qualifying and Survivor rows only; the Placement row has no count (the Finale carries its own). The server enforces the rest.</summary>
    public int? AdvancingCount { get; set; } = 8;
    public DateOnly? SquadDate { get; set; }
    public TimeOnly? SquadStartTime { get; set; }

    /// <summary>Server errors named <c>Series.Stages[i]</c> for this row.</summary>
    public List<string> Errors { get; } = [];
}
