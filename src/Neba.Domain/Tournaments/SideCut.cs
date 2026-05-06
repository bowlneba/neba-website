using System.Drawing;

using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Tournaments;

/// <summary>
/// A named, reusable eligibility predicate that identifies a subgroup of bowlers who qualify
/// for an alternative advancement path in a tournament round. A Side Cut has its own Cut Line,
/// evaluated independently of the Main Cut.
/// </summary>
public sealed class SideCut
    : AggregateRoot
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
    /// entries in tournament management interfaces.
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
    /// The collection of criteria groups associated with this Side Cut.
    /// </summary>
    public IReadOnlyCollection<SideCutCriteriaGroup> CriteriaGroups
        => _criteriaGroups.AsReadOnly();

    /// <summary>
    /// Adds a new criteria group to this Side Cut with the given logical operator and sort order.
    /// Returns the new group's ID so callers can subsequently add criteria to it.
    /// Sort order must be unique among all groups on this Side Cut.
    /// </summary>
    public ErrorOr<SideCutCriteriaGroupId> AddCriteriaGroup(LogicalOperator logicalOperator, int sortOrder)
    {
        if (_criteriaGroups.Any(g => g.SortOrder == sortOrder))
        {
            return SideCutErrors.DuplicateSortOrder(sortOrder);
        }

        var groupResult = SideCutCriteriaGroup.Create(this, logicalOperator, sortOrder);
        if (groupResult.IsError)
        {
            return groupResult.Errors;
        }

        _criteriaGroups.Add(groupResult.Value);

        return groupResult.Value.Id;
    }

    /// <summary>
    /// Adds or replaces the age requirement on the identified criteria group.
    /// </summary>
    public ErrorOr<Success> AddCriteria(SideCutCriteriaGroupId groupId, int? minimumAge, int? maximumAge)
    {
        var group = _criteriaGroups.SingleOrDefault(g => g.Id.Equals(groupId));

        return group is null
            ? SideCutErrors.CriteriaGroupNotFound(groupId)
            : group.AddCriteria(minimumAge, maximumAge);
    }

    /// <summary>
    /// Adds or replaces the gender requirement on the identified criteria group.
    /// </summary>
    public ErrorOr<Success> AddCriteria(SideCutCriteriaGroupId groupId, Gender gender)
    {
        var group = _criteriaGroups.SingleOrDefault(g => g.Id.Equals(groupId));

        return group is null
            ? SideCutErrors.CriteriaGroupNotFound(groupId)
            : group.AddCriteria(gender);
    }
}

internal static class SideCutErrors
{
    public static Error DuplicateSortOrder(int sortOrder)
        => Error.Conflict(
            code: "SideCut.DuplicateSortOrder",
            description: $"A criteria group with sort order {sortOrder} already exists on this Side Cut.",
            metadata: new Dictionary<string, object> { { "sortOrder", sortOrder } });

    public static Error CriteriaGroupNotFound(SideCutCriteriaGroupId groupId)
        => Error.NotFound(
            code: "SideCut.CriteriaGroupNotFound",
            description: $"No criteria group with ID {groupId} exists on this Side Cut.",
            metadata: new Dictionary<string, object> { { "groupId", groupId.Value.ToString() } });
}