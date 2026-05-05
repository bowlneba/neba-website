using ErrorOr;

using Neba.Domain.Bowlers;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Associates a Side Cut with a specific tournament round and defines how its qualifiers are combined with the Main Cut qualifiers.
/// </summary>
public sealed class SideCutCriteriaGroup
{
    /// <summary>
    /// The unique identifier for this Side Cut Criteria Group.
    /// </summary>
    public required SideCutCriteriaGroupId Id { get; init; }

    /// <summary>
    /// The Side Cut this group belongs to.
    /// </summary>
    public required SideCutId SideCutId { get; init; }

    /// <summary>
    /// The Side Cut associated with this criteria group.
    /// </summary>
    internal SideCut SideCut { get; init; } = null!;

    /// <summary>
    /// The logical operator that defines how this Side Cut's qualifiers are combined with the Main Cut qualifiers.
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

    internal static ErrorOr<SideCutCriteriaGroup> Create(SideCutId sideCutId, LogicalOperator logicalOperator, int sortOrder)
    {
        return new SideCutCriteriaGroup
        {
            Id = SideCutCriteriaGroupId.New(),
            SideCutId = sideCutId,
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