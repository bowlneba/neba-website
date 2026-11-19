namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Series stage input.</summary>
public sealed record SeriesStageInput
{
    /// <summary>Existing Stage ID when editing; null for a new Stage.</summary>
    public string? Id { get; init; }

    /// <summary>1 = Qualifying, 2 = Survivor, 3 = Placement.</summary>
    public required int Kind { get; init; }

    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }

    /// <summary>Required on Qualifying and Survivor Stages; a Placement Stage has none.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public TimeOnly? SquadStartTime { get; init; }
}