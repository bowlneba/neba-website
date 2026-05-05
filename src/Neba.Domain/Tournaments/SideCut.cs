using System.Drawing;

using ErrorOr;

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

    private readonly List<SideCutCriteriaGroup> _criteriaGroups = [];

    /// <summary>
    /// The collection of criteria groups associated with this Side Cut. Each criteria group defines a specific set of qualifiers and how they are combined with the Main Cut qualifiers. This collection is read-only to ensure that criteria groups are managed through the appropriate methods on the tournament round configuration, which maintain the integrity of the relationships and the uniqueness of Sort Order values within the same round.
    /// </summary>
    public IReadOnlyCollection<SideCutCriteriaGroup> CriteriaGroups
        => _criteriaGroups.AsReadOnly();

    //todo: need to rethink this w/ some help w/ claude about how to handle add new group vs add new criteria to existing group.
    public ErrorOr<Success> AddCriteria(int sortOrder, LogicalOperator logicalOperator, int? minimumAge, int? maximumAge)
    {
        var criteriaSortOrderLookup = _criteriaGroups.Where(group => group.SortOrder == sortOrder).ToList();

        if (criteriaSortOrderLookup.Count > 1)
        {
            return SideCutCriteriaGroupErrors.MultipleCriteriaGroupsWithSameSortOrder(sortOrder);
        }

        var criteriaGroupResult = criteriaSortOrderLookup.Count == 1
            ? criteriaSortOrderLookup.Single()
            : SideCutCriteriaGroup.Create(Id, logicalOperator, sortOrder);

        if (criteriaGroupResult.IsError)
        {
            return criteriaGroupResult.Errors;
        }

        var criteriaGroup = criteriaGroupResult.Value;

        var result = criteriaGroup.AddCriteria(minimumAge, maximumAge);

        if (result.IsError)
        {
            return result.Errors;
        }


    }
}

internal static class SideCutCriteriaGroupErrors
{
    public static Error MultipleCriteriaGroupsWithSameSortOrder(int sortOrder)
        => Error.Validation(
        code: "SideCutCriteriaGroup.DuplicateSortOrder",
        description: "Multiple Side Cut Criteria Groups with the same Sort Order value exist within the same tournament round, which violates the requirement for unique Sort Order values to ensure a clear and deterministic evaluation sequence.",
        metadata: new Dictionary<string, object>
        {
            {"sortOrder", sortOrder}
        }
    );
}