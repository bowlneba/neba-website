namespace Neba.Website.Server.Series;

/// <summary>One Stage row in the Series form.</summary>
public sealed class SeriesStageRowModel
{
    /// <summary>Stable key for Razor <c>@key</c>; never sent to the API.</summary>
    public Guid RowKey { get; } = Guid.NewGuid();

    /// <summary>Existing Stage ID on edit; null for a new row.</summary>
    public string? StageId { get; init; }

    /// <summary>Qualifying, Survivor or Placement.</summary>
    public string Kind { get; set; } = StageKinds.Qualifying;
    /// <summary>The tournament this Stage uses, or null until chosen.</summary>
    public string? TournamentId { get; set; }

    /// <summary>Qualifying and Survivor rows only; the Placement row has no count (the Finale carries its own). The server enforces the rest.</summary>
    public int? AdvancingCount { get; set; } = 8;
    /// <summary>Survivor rows only: the squad date.</summary>
    public DateOnly? SquadDate { get; set; }
    /// <summary>Survivor rows only: the squad start time.</summary>
    public TimeOnly? SquadStartTime { get; set; }

    /// <summary>Server errors named <c>Series.Stages[i]</c> for this row.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification="A mutable form model the editor binds to.")]
    public List<string> Errors { get; } = [];
}
