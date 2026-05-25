namespace Neba.Website.Server.History.Champions;

/// <summary>View model containing a bowler's full title history, used by the champion record modal.</summary>
public sealed record BowlerTitlesViewModel
{
    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>Whether the bowler has been inducted into the Hall of Fame.</summary>
    public required bool HallOfFame { get; init; }

    /// <summary>All titles in the bowler's career, ordered most-recent first.</summary>
    public required IReadOnlyCollection<TitleViewModel> Titles { get; init; }
}