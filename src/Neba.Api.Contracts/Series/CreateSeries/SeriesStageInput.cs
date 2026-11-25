namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Series stage input.</summary>
public sealed record SeriesStageInput
{
    /// <summary>Existing Series tournament ID when editing; null for a new tournament.</summary>
    public string? Id { get; init; }

    /// <summary>1 = Qualifying, 2 = Survivor, 3 = Placement.</summary>
    public required int Kind { get; init; }

    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }

    /// <summary>Required on Qualifying and Survivor tournaments; a Placement tournament has none.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor tournaments only.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor tournaments only.</summary>
    public TimeOnly? SquadStartTime { get; init; }
}