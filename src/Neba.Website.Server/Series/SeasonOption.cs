using Neba.Api.Contracts.Seasons.ListSeasons;

namespace Neba.Website.Server.Series;

internal sealed record SeasonOption(string Id, string Label, DateOnly StartDate, DateOnly EndDate)
{
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
