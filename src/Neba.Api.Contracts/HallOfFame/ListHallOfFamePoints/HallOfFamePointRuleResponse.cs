namespace Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;

/// <summary>
/// One line of the Hall of Fame point table.
/// </summary>
public sealed record HallOfFamePointRuleResponse
{
    /// <summary>
    /// The table section the rule is shown under.
    /// </summary>
    public required string Group { get; init; }

    /// <summary>
    /// The title or award name.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The points earned.
    /// </summary>
    public required int Points { get; init; }
}