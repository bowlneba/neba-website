using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One submitted Stage. Its order is its position in the submitted list.</summary>
public sealed record SeriesStageDefinition
{
    /// <summary>Existing Stage to update; null adds a new Stage.</summary>
    public SeriesStageId? StageId { get; init; }
    /// <summary>The Stage kind.</summary>
    public required StageKind Kind { get; init; }
    /// <summary>The tournament for this Stage.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>Required on Qualifying and Survivor Stages; a Placement Stage has none.</summary>
    public int? AdvancingCount { get; init; }
    /// <summary>Survivor squad date.</summary>
    public DateOnly? SquadDate { get; init; }
    /// <summary>Survivor squad start time.</summary>
    public TimeOnly? SquadStartTime { get; init; }
}