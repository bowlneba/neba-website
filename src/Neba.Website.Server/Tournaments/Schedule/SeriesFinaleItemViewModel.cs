using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Website.Server.Series;

namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>A Series Finale as the tournament list shows it.</summary>
public sealed record SeriesFinaleItemViewModel
{
    /// <summary>The Series ID.</summary>
    public required string SeriesId { get; init; }
    /// <summary>The Series name.</summary>
    public required string SeriesName { get; init; }
    /// <summary>The season the Series belongs to.</summary>
    public required string SeasonId { get; init; }
    /// <summary>The Series logo, or null (the card shows initials).</summary>
    public Uri? LogoUrl { get; init; }
    /// <summary>How many finalists bowl the Finale.</summary>
    public required int FinalistCount { get; init; }
    /// <summary>The Finale date.</summary>
    public required DateOnly Date { get; init; }
    /// <summary>The Finale start time.</summary>
    public required TimeOnly StartTime { get; init; }
    /// <summary>The host center, or null while to be announced.</summary>
    public string? Host { get; init; }

    /// <summary>"{Series name} Finale".</summary>
    public string Title => SeriesDisplay.FinaleName(SeriesName);

    /// <summary>Link to the Series page.</summary>
    public string Href => "/series/" + SeriesId;
}

internal static class SeriesFinaleItemMappingExtensions
{
    extension(SeriesFinaleSummaryResponse response)
    {
        public SeriesFinaleItemViewModel ToFinaleItem() => new()
        {
            SeriesId = response.SeriesId,
            SeriesName = response.SeriesName,
            SeasonId = response.SeasonId,
            LogoUrl = response.LogoUrl,
            FinalistCount = response.Finale.FinalistCount,
            Date = response.Finale.Date,
            StartTime = response.Finale.StartTime,
            Host = SeriesDisplay.Host(
                response.Finale.BowlingCenterName, response.Finale.BowlingCenterCity, response.Finale.BowlingCenterState),
        };
    }
}