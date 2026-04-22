namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>View model representing a season for display in the tournament schedule.</summary>
public sealed record SeasonViewModel
{
    /// <summary>Unique season identifier (ULID string).</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable season description, e.g. "2026 Season" or "2020-21 Season".</summary>
    public required string Description { get; init; }

    /// <summary>First day of the season.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Last day of the season.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>URL-safe season label derived from the date range, e.g. "2026" or "2020-21".</summary>
    public string Label => StartDate.Year == EndDate.Year
        ? StartDate.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : $"{StartDate.Year}-{EndDate.Year % 100:D2}";

    /// <summary>True when the season spans more than one calendar year.</summary>
    public bool IsMergedSeason => StartDate.Year != EndDate.Year;

    /// <summary>True when <paramref name="year"/> falls within the season's start and end year, inclusive.</summary>
    public bool ContainsYear(int year) => year >= StartDate.Year && year <= EndDate.Year;
}
