using ErrorOr;

using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Features.Memberships.Domain;

/// <summary>
/// A membership a bowler bought or was granted: a <see cref="MembershipType"/> held over a date range.
/// </summary>
/// <remarks>
/// Owned by the bowler. <see cref="Create"/> is internal and runs the eligibility rules, and
/// <c>Bowler.AddMembership</c> is the only caller, so no code can skip them.
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
    internal static ErrorOr<BowlerMembership> Create(
        MembershipType type,
        DateOnly startDate,
        IReadOnlyCollection<MembershipSnapshot> history)
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
            Id = BowlerMembershipId.New(),
            Type = type,
            Cost = type.Cost,
            StartDate = startDate,
            EndDate = endDate
        };
    }
}
