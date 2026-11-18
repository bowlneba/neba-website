using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>The Series a tournament is a Stage of, shared by the season list and the tournament detail.</summary>
public sealed record TournamentSeriesDto
{
    /// <summary>The Series ID.</summary>
    public required SeriesId SeriesId { get; init; }

    /// <summary>The Series name.</summary>
    public required string Name { get; init; }

    /// <summary>The Series' own logo, or null.</summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>The Stage kind name (a string, never the SmartEnum, because the DTO is cached).</summary>
    public required string Kind { get; init; }

    /// <summary>Top N (Qualifying) or bowlers advancing (Survivor); null on a Placement Stage.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor Stages only: the squad's date.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor Stages only: the squad's start time.</summary>
    public TimeOnly? SquadStartTime { get; init; }

    /// <summary>Placement Stages of a Series with a Finale: how many finalists go to it.</summary>
    public int? FinaleCount { get; init; }

    /// <summary>Placement Stages of a Series with a Finale: its date.</summary>
    public DateOnly? FinaleDate { get; init; }

    /// <summary>Placement Stages of a Series with a Finale: its start time.</summary>
    public TimeOnly? FinaleStartTime { get; init; }
}