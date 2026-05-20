using ErrorOr;

using Neba.Api.Domain;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Represents an ordered collection of one or more criteria within a Side Cut, evaluated together using the group's logical operator.
/// </summary>
public sealed class SideCutCriteriaGroup
{
    /// <summary>
    /// The unique identifier for this Side Cut Criteria Group.
    /// </summary>
    public required SideCutCriteriaGroupId Id { get; init; }

    /// <summary>
    /// The Side Cut associated with this criteria group.
    /// </summary>
    internal SideCut SideCut { get; init; } = null!;

    /// <summary>
    /// The Group Operator — the logical operator applied within this Criterion Group to combine its
    /// individual Criteria (e.g., <c>And</c> requires all criteria to match; <c>Or</c> requires at
    /// least one to match).
    /// </summary>
    public required LogicalOperator LogicalOperator { get; init; }

    /// <summary>
    /// The sort order of this Side Cut Criteria Group within the Side Cut's configuration. A lower value indicates earlier evaluation. Must be unique among all groups on the same Side Cut.
    /// </summary>
    public required int SortOrder { get; init; }

    private readonly List<SideCutCriteria> _criteria = [];

    /// <summary>
    /// The collection of criteria that define the conditions for this Side Cut.
    /// </summary>
    public IReadOnlyCollection<SideCutCriteria> Criteria
        => _criteria;

    internal static ErrorOr<SideCutCriteriaGroup> Create(
        SideCut sideCut,
        LogicalOperator logicalOperator,
        int sortOrder,
        SideCutCriteriaGroupId? id = null)
    {
        ArgumentNullException.ThrowIfNull(sideCut);

        return new SideCutCriteriaGroup
        {
            Id = id ?? SideCutCriteriaGroupId.New(),
            SideCut = sideCut,
            LogicalOperator = logicalOperator,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Adds or replaces the age requirement for this group. Only one age criteria is allowed per group; calling this again replaces the existing one.
    /// </summary>
    internal ErrorOr<Success> AddCriteria(int? minimumAge, int? maximumAge)
    {
        var criteria = SideCutCriteria.CreateAgeRequirement(minimumAge, maximumAge);

        if (criteria.IsError)
        {
            return criteria.Errors;
        }

        var existing = _criteria.FirstOrDefault(c => c.GenderRequirement is null);
        if (existing is not null)
        {
            _criteria.Remove(existing);
        }

        _criteria.Add(criteria.Value);

        return Result.Success;
    }

    /// <summary>
    /// Adds or replaces the gender requirement for this group. Only one gender criteria is allowed per group; calling this again replaces the existing one.
    /// </summary>
    internal ErrorOr<Success> AddCriteria(Gender gender)
    {
        var criteria = SideCutCriteria.CreateGenderRequirement(gender);

        if (criteria.IsError)
        {
            return criteria.Errors;
        }

        var existing = _criteria.FirstOrDefault(c => c.GenderRequirement is not null);
        if (existing is not null)
        {
            _criteria.Remove(existing);
        }

        _criteria.Add(criteria.Value);

        return Result.Success;
    }
}