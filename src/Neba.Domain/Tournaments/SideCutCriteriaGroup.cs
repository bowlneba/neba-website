namespace Neba.Domain.Tournaments;

/// <summary>
/// Associates a Side Cut with a specific tournament round and defines how its qualifiers are combined with the Main Cut qualifiers.
/// </summary>
public sealed class SideCutCriteriaGroup
{
    /// <summary>
    /// The unique identifier for this Side Cut Criteria Group.
    /// </summary>
    public required SideCutId SideCutId { get; init; }

    /// <summary>
    /// The Side Cut associated with this criteria group. This is a required relationship, as a Side Cut Criteria Group cannot exist without an associated Side Cut.
    /// </summary>
    internal SideCut SideCut { get; init; } = null!;

    /// <summary>
    /// The logical operator that defines how this Side Cut's qualifiers are combined with the Main Cut qualifiers. This is a required property, as it determines the fundamental logic of how bowlers qualify through this Side Cut in relation to the Main Cut.
    /// </summary>
    public required LogicalOperator LogicalOperator { get; init; }

    /// <summary>
    /// The sort order of this Side Cut Criteria Group within the tournament round's configuration. This is a required property, as it determines the evaluation sequence of multiple Side Cuts when applied to the same round. A lower Sort Order value indicates that this Side Cut is evaluated earlier in the qualification process, which can affect how bowlers are categorized and how the Main Cut is applied to remaining bowlers. The Sort Order must be unique among all Side Cut Criteria Groups within the same round to ensure a clear and deterministic evaluation sequence.
    /// </summary>
    public required int SortOrder { get; init; }
}