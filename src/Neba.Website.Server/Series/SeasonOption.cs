using Neba.Api.Contracts.Seasons.ListSeasons;

namespace Neba.Website.Server.Series;

/// <summary>A season the create page can pick.</summary>
/// <param name="Id">The season ID.</param>
/// <param name="Label">The picker text with the date range.</param>
/// <param name="StartDate">The first day of the season.</param>
/// <param name="EndDate">The last day of the season.</param>
public sealed record SeasonOption(string Id, string Label, DateOnly StartDate, DateOnly EndDate)
{
    /// <summary>Builds the option for a season from the API.</summary>
    public static SeasonOption From(SeasonResponse season)
    {
        ArgumentNullException.ThrowIfNull(season);
        return new(season.Id, season.Description + " (" + SeriesDisplay.SeasonRange(season.StartDate, season.EndDate) + ")", season.StartDate, season.EndDate);
    }

    /// <summary>The season containing today, else the next one to start, else the newest.</summary>
    public static SeasonOption? Default(IReadOnlyList<SeasonOption> seasons, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(seasons);
        return seasons.FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate)
            ?? seasons.Where(s => s.StartDate > today).MinBy(s => s.StartDate)
            ?? seasons.MaxBy(s => s.StartDate);
    }
}