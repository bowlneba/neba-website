using System.Drawing;

namespace Neba.Domain.Tournaments;

/// <summary>
/// A named, reusable eligibility predicate that identifies a subgroup of bowlers who qualify
/// for an alternative advancement path in a tournament round. A Side Cut has its own Cut Line,
/// evaluated independently of the Main Cut.
/// </summary>
public sealed class SideCut
{
    /// <summary>
    /// The unique identifier for this Side Cut.
    /// </summary>
    public required SideCutId Id { get; init; }

    /// <summary>
    /// The display name of this Side Cut (e.g., "Senior", "Women").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The Display Color assigned to this Side Cut, used to visually distinguish qualifying
    /// entries in tournament management interfaces. Display Color is a property of the Side Cut,
    /// not of the bowler or the entry.
    /// </summary>
    public required Color Indicator { get; init; }

    /// <summary>
    /// The logical operator that defines how this Side Cut's qualifiers are combined with the Main Cut qualifiers
    /// </summary>
    public required LogicalOperator LogicalOperator { get; init; }

    /// <summary>
    /// Whether this Side Cut is active and available for use in tournament configuration.
    /// </summary>
    public required bool Active { get; init; }
}