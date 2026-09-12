namespace Neba.Website.Server.History.Champions;

/// <summary>View model summarising a bowler's championship record for display in the title-count leaderboard.</summary>
public sealed record BowlerTitleSummaryViewModel
{
    /// <summary>The bowler's unique identifier.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The bowler's family or surname, used to sort/group by last name rather than the formatted display name.</summary>
    public required string BowlerLastName { get; init; }

    /// <summary>The bowler's given first name, used as a tiebreaker when sorting by last name.</summary>
    public required string BowlerFirstName { get; init; }

    /// <summary>Total number of tournament titles won.</summary>
    public required int TitleCount { get; init; }

    /// <summary>Whether the bowler has been inducted into the Hall of Fame.</summary>
    public required bool HallOfFame { get; init; }
}