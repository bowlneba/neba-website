using Neba.Api.Contracts.Tournaments;

namespace Neba.Website.Server.Tournaments;

/// <summary>The Series a tournament is a Stage of, as the tournament pages show it.</summary>
public sealed record TournamentSeriesViewModel
{
    /// <summary>The Series ID.</summary>
    public required string SeriesId { get; init; }
    /// <summary>The Series name.</summary>
    public required string Name { get; init; }
    /// <summary>The Series logo, or null (the page shows initials).</summary>
    public Uri? LogoUrl { get; init; }
    /// <summary>Qualifying, Survivor or Placement.</summary>
    public required string Kind { get; init; }
    /// <summary>Top N (Qualifying) or bowlers advancing (Survivor).</summary>
    public int? AdvancingCount { get; init; }
    /// <summary>Survivor Stages only: the squad date.</summary>
    public DateOnly? SquadDate { get; init; }
    /// <summary>Survivor Stages only: the squad start time.</summary>
    public TimeOnly? SquadStartTime { get; init; }

    /// <summary>Placement Stages only, when the Series has a Finale.</summary>
    public int? FinaleCount { get; init; }
    /// <summary>The Finale date.</summary>
    public DateOnly? FinaleDate { get; init; }
    /// <summary>The Finale start time.</summary>
    public TimeOnly? FinaleStartTime { get; init; }

    /// <summary>Link to the Series page.</summary>
    public string Href => "/series/" + SeriesId;
}

internal static class TournamentSeriesMappingExtensions
{
    extension(TournamentSeriesResponse response)
    {
        public TournamentSeriesViewModel ToViewModel() => new()
        {
            SeriesId = response.SeriesId,
            Name = response.Name,
            LogoUrl = response.LogoUrl,
            Kind = response.Kind,
            AdvancingCount = response.AdvancingCount,
            SquadDate = response.SquadDate,
            SquadStartTime = response.SquadStartTime,
            FinaleCount = response.FinaleCount,
            FinaleDate = response.FinaleDate,
            FinaleStartTime = response.FinaleStartTime,
        };
    }
}
