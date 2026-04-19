namespace Neba.Website.Server.Stats;

/// <summary>
/// Represents a row in the match play appearance statistics, containing information about a bowler's rank, identity, and their performance in terms of finals appearances, tournaments participated in, and total entries.
/// </summary>
public sealed record MatchPlayAppearancesRowViewModel
{
    /// <summary>
    /// Gets the rank of the bowler in the match play appearance statistics. This is a required property that indicates the position of the bowler relative to others based on their performance in match play appearances.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// Gets the unique identifier of the bowler. This is a required property that serves as a reference to the bowler's identity in the system, allowing for association with other data related to the bowler.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// Gets the name of the bowler. This is a required property that provides a human-readable name for the bowler, which can be displayed in the user interface and used for identification purposes in the context of match play appearance statistics.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Gets the number of finals appearances the bowler has made in match play. This is a required property that indicates how many times the bowler has reached the finals stage in match play competitions, reflecting their success and consistency in advancing through the tournament stages.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Gets the number of tournaments the bowler has participated in for match play. This is a required property that indicates the level of experience and participation of the bowler in match play competitions, providing context for their finals appearances and overall performance in the sport.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// Gets the total number of entries the bowler has made in match play competitions. This is a required property that indicates the total number of times the bowler has entered match play tournaments, which can be used to assess their commitment and involvement in the sport, as well as to provide context for their finals appearances and tournament participation.
    /// </summary>
    public required int Entries { get; init; }
}