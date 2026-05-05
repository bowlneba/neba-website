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

    private readonly List<SideCutCriteria> _criteria = [];

    /// <summary>
    /// The collection of criteria that define the conditions for this Side Cut. This is a read-only collection, as criteria should be managed through the Side Cut's methods to ensure consistency and validation.
    /// </summary>
    public IReadOnlyCollection<SideCutCriteria> Criteria
        => _criteria;

    /// <summary>
    /// Factory method to create a new Side Cut Criteria Group with the specified properties. This method ensures that all required properties are provided and can include any necessary validation logic to maintain the integrity of the Side Cut configuration.
    /// </summary>
    /// <param name="sideCutId"></param>
    /// <param name="logicalOperator"></param>
    /// <param name="sortOrder"></param>
    /// <returns></returns>
    public static ErrorOr<SideCutCriteriaGroup> Create(SideCutId sideCutId, LogicalOperator logicalOperator, int sortOrder)
    {
        return new SideCutCriteriaGroup
        {
            SideCutId = sideCutId,
            LogicalOperator = logicalOperator,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Adds a new criterion to this Side Cut Criteria Group. This method includes validation to ensure that the criterion is valid and that it does not violate any constraints of the Side Cut configuration. For example, if adding an age requirement, it should validate that the minimum age is less than or equal to the maximum age. If adding
    /// </summary>
    /// <param name="minimumAge"></param>
    /// <param name="maximumAge"></param>
    /// <returns></returns>
    public ErrorOr<Success> AddCriteria(int? minimumAge, int? maximumAge)
    {
        var criteria = SideCutCriteria.CreateAgeRequirement(minimumAge, maximumAge);

        if (criteria.IsError)
        {
            return criteria.Errors;
        }

        _criteria.Add(criteria.Value);

        return Result.Success;
    }

    /// <summary>
    /// Adds a new criterion to this Side Cut Criteria Group based
    /// </summary>
    /// <param name="gender"></param>
    /// <returns></returns>
    public ErrorOr<Success> AddCriteria(Gender gender)
    {
        var criteria = SideCutCriteria.CreateGenderRequirement(gender);

        if (criteria.IsError)
        {
            return criteria.Errors;
        }

        _criteria.Add(criteria.Value);

        return Result.Success;
    }
}