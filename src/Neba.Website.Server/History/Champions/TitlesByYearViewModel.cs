namespace Neba.Website.Server.History.Champions;

/// <summary>View model grouping all championship titles won in a single calendar year.</summary>
public sealed record TitlesByYearViewModel
{
    /// <summary>The calendar year.</summary>
    public required int Year { get; init; }

    /// <summary>All titles awarded during this year.</summary>
    public required IReadOnlyCollection<BowlerTitleViewModel> Titles { get; init; }
}