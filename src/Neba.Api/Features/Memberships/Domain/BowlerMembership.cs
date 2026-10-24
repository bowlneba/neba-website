using ErrorOr;

using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Features.Memberships.Domain;

/// <summary>
/// A membership a bowler bought or was granted: a <see cref="MembershipType"/> held over a date range.
/// </summary>
/// <remarks>
/// Owned by the bowler. <see cref="Create"/> is internal and runs the eligibility rules, and
/// <c>Bowler.AddMembership</c> is the only caller, so no code can skip them. <see cref="CreateFromLegacy"/>
/// copies legacy records as stored, for the migration and feed only, through <c>Bowler.AddLegacyMembership</c>.
/// </remarks>
public sealed class BowlerMembership
{
    private BowlerMembership()
    { }

    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public BowlerMembershipId Id { get; private set; }

    /// <summary>
    /// The identifier of this membership in the organization management software (nebamgmt-v3).
    /// Ties the record to the legacy database; null for memberships created in this system.
    /// </summary>
    public int? LegacyId { get; private set; }

    /// <summary>
    /// The kind of membership held.
    /// </summary>
    public MembershipType Type { get; private set; } = null!;

    /// <summary>
    /// The amount paid, fixed at purchase. Never derived from <see cref="Type"/>, whose cost can change.
    /// </summary>
    public decimal Cost { get; private set; }

    /// <summary>
    /// The first day the membership is valid.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// The last day the membership is valid. Inclusive.
    /// </summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Whether the membership is valid on <paramref name="date"/>. The end date is inclusive.
    /// </summary>
    public bool IsValidOn(DateOnly date)
        => date <= EndDate;

    internal MembershipSnapshot ToSnapshot()
        => new(Type, StartDate, EndDate);

    /// <summary>
    /// Creates a membership of <paramref name="type"/> starting on <paramref name="startDate"/>,
    /// or returns the first eligibility rule the bowler's <paramref name="history"/> breaks.
    /// </summary>
    /// <param name="type">The membership type to buy.</param>
    /// <param name="startDate">The first day of the membership.</param>
    /// <param name="history">The bowler's existing memberships.</param>
    /// <param name="id">A fixed identifier, for tests that need reproducible data. Generated when omitted.</param>
    internal static ErrorOr<BowlerMembership> Create(
        MembershipType type,
        DateOnly startDate,
        IReadOnlyCollection<MembershipSnapshot> history,
        BowlerMembershipId? id = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(history);

        var endDate = type.CalculateEndDate(startDate);

        var eligibility = MembershipRules.CheckEligibility(type, startDate, endDate, history);
        if (eligibility.IsError)
        {
            return eligibility.Errors;
        }

        return new BowlerMembership
        {
            Id = id ?? BowlerMembershipId.New(),
            Type = type,
            Cost = type.Cost,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <summary>
    /// Copies a membership recorded in the organization management software exactly as stored.
    /// Runs no eligibility rules: the legacy system already enforced them, and the migration and
    /// feed must keep its cost and dates. Only structural checks apply.
    /// </summary>
    /// <param name="legacyId">The membership's identifier in the legacy database.</param>
    /// <param name="type">The membership type, resolved with <c>MembershipType.FromLegacyId</c>.</param>
    /// <param name="cost">The cost as stored.</param>
    /// <param name="startDate">The start date as stored.</param>
    /// <param name="endDate">The end date as stored. Never recalculated.</param>
    internal static ErrorOr<BowlerMembership> CreateFromLegacy(
        int legacyId,
        MembershipType type,
        decimal cost,
        DateOnly startDate,
        DateOnly endDate)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (legacyId <= 0)
        {
            return BowlerMembershipErrors.InvalidLegacyId;
        }

        if (cost < 0)
        {
            return BowlerMembershipErrors.NegativeCost;
        }

        if (endDate < startDate)
        {
            return BowlerMembershipErrors.EndDateBeforeStartDate(startDate, endDate);
        }

        return new BowlerMembership
        {
            Id = BowlerMembershipId.New(),
            LegacyId = legacyId,
            Type = type,
            Cost = cost,
            StartDate = startDate,
            EndDate = endDate
        };
    }
}
