using System.Text.RegularExpressions;

using Ardalis.SmartEnum;

namespace Neba.Api.Contracts.Memberships;

/// <summary>
/// A kind of NEBA membership a bowler can hold. Defined in code and holds data only;
/// rules (end date, eligibility) live in the API.
/// </summary>
/// <remarks>
/// <see cref="SmartEnum{TEnum, TValue}.Value"/> is the permanent code and the persisted value
/// (UPPER_SNAKE_CASE, at most 32 characters). Renaming a code needs a data migration.
/// <see cref="SmartEnum{TEnum, TValue}.Name"/> is the display label and may change freely.
/// </remarks>
[SmartEnumStringComparer(StringComparison.InvariantCultureIgnoreCase)]
public sealed partial class MembershipType
    : SmartEnum<MembershipType, string>
{
    /// <summary>
    /// Longest allowed code.
    /// </summary>
    public const int MaxCodeLength = 32;

    private const int GuestMinimumDaysBetweenUses = 730;

    private static readonly Lazy<IReadOnlyDictionary<int, MembershipType>> ByLegacyId
        = new(() => List.ToDictionary(t => t.LegacyId));

    private static readonly Lazy<IReadOnlyList<MembershipType>> PurchasableTypes
        = new(() => [.. List.Where(t => t.Active)]);

    [GeneratedRegex("^[A-Z]+(_[A-Z]+)*$")]
    private static partial Regex UpperSnakeCase();

    /// <summary>
    /// A bowler's first yearly membership; once per lifetime.
    /// </summary>
    public static readonly MembershipType New = new(
        "NEW", "New Member", legacyId: 1, MembershipKind.Yearly, cost: 25.00m, lengthInDays: null,
        RepeatPolicy.OncePerLifetime, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

    /// <summary>
    /// A yearly renewal; needs a prior qualifying membership.
    /// </summary>
    public static readonly MembershipType Renewal = new(
        "RENEWAL", "Renewal", legacyId: 2, MembershipKind.Yearly, cost: 25.00m, lengthInDays: null,
        RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: true);

    /// <summary>
    /// A no-cost yearly membership for military service.
    /// </summary>
    public static readonly MembershipType Military = new(
        "MILITARY", "Military Service", legacyId: 3, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
        RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

    /// <summary>
    /// A no-cost yearly honorary membership.
    /// </summary>
    public static readonly MembershipType Honorary = new(
        "HONORARY", "Honorary Member", legacyId: 4, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
        RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

    /// <summary>
    /// A short paid membership for a bowler who is not a member.
    /// </summary>
    public static readonly MembershipType NonMember = new(
        "NON_MEMBER", "Non-Member", legacyId: 5, MembershipKind.Temporary, cost: 15.00m, lengthInDays: 2,
        RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: true);

    /// <summary>
    /// A short free membership for a guest, limited by a minimum gap between uses.
    /// </summary>
    public static readonly MembershipType Guest = new(
        "GUEST", "Guest", legacyId: 6, MembershipKind.Temporary, cost: 0.00m, lengthInDays: 2,
        RepeatPolicy.MinimumDaysSinceLast(GuestMinimumDaysBetweenUses), requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: true);

    /// <summary>
    /// Inactive; no reservation membership has been issued since 2022. Kept so history resolves.
    /// </summary>
    public static readonly MembershipType Reservation = new(
        "RESERVATION", "Reservation", legacyId: 7, MembershipKind.Temporary, cost: 0.00m, lengthInDays: 2,
        RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: false);

    /// <summary>
    /// Inactive; retired in the legacy system. Kept so history resolves.
    /// </summary>
    public static readonly MembershipType Courtesy = new(
        "COURTESY", "Courtesy", legacyId: 9, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
        RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: false);

    /// <summary>
    /// A no-cost yearly membership for a time honored member; needs a prior qualifying membership.
    /// </summary>
    public static readonly MembershipType TimeHonored = new(
        "TIME_HONORED", "Time Honored Member", legacyId: 10, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
        RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: true);

    /// <summary>
    /// The types that can be bought today: every <see cref="Active"/> type. Use this for pickers.
    /// </summary>
    public static IReadOnlyList<MembershipType> Purchasable => PurchasableTypes.Value;

    /// <summary>
    /// The legacy system's membership id. Used only to match the legacy feed and migration; remove after cutover.
    /// </summary>
    public int LegacyId { get; }

    /// <summary>
    /// Whether the membership is temporary or yearly.
    /// </summary>
    public MembershipKind Kind { get; }

    /// <summary>
    /// The current price. A bowler's membership keeps its own cost snapshot.
    /// </summary>
    public decimal Cost { get; }

    /// <summary>
    /// Whether new memberships of this type can be bought.
    /// </summary>
    public bool Active { get; }

    /// <summary>
    /// Days a temporary membership is valid, counting the start date. Null for yearly types.
    /// </summary>
    public int? LengthInDays { get; }

    /// <summary>
    /// How often a bowler may buy this type.
    /// </summary>
    public RepeatPolicy RepeatPolicy { get; }

    /// <summary>
    /// Whether the bowler needs a prior qualifying membership first.
    /// </summary>
    public bool RequiresPriorMembership { get; }

    /// <summary>
    /// Whether holding this type counts as a prior membership for types that require one.
    /// </summary>
    public bool QualifiesAsPriorMembership { get; }

    private MembershipType(
        string value,
        string name,
        int legacyId,
        MembershipKind kind,
        decimal cost,
        int? lengthInDays,
        RepeatPolicy repeatPolicy,
        bool requiresPriorMembership,
        bool qualifiesAsPriorMembership,
        bool active)
        : base(name, value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cost);
        ArgumentNullException.ThrowIfNull(repeatPolicy);

        if (value.Length > MaxCodeLength)
        {
            throw new ArgumentException($"Code '{value}' exceeds {MaxCodeLength} characters.", nameof(value));
        }

        if (!UpperSnakeCase().IsMatch(value))
        {
            throw new ArgumentException($"Code '{value}' must be UPPER_SNAKE_CASE.", nameof(value));
        }

        switch (kind)
        {
            case MembershipKind.Temporary when lengthInDays is null or < 1:
                throw new ArgumentException("A temporary membership needs a length of at least 1 day.", nameof(lengthInDays));
            case MembershipKind.Yearly when lengthInDays is not null:
                throw new ArgumentException("A yearly membership cannot have a length in days.", nameof(lengthInDays));
            case MembershipKind.Temporary:
            case MembershipKind.Yearly:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown membership kind.");
        }

        LegacyId = legacyId;
        Kind = kind;
        Cost = cost;
        LengthInDays = lengthInDays;
        RepeatPolicy = repeatPolicy;
        RequiresPriorMembership = requiresPriorMembership;
        QualifiesAsPriorMembership = qualifiesAsPriorMembership;
        Active = active;
    }

    /// <summary>
    /// Finds the type for a legacy membership id. Throws if none matches.
    /// </summary>
    /// <param name="legacyId">The legacy membership id.</param>
    public static MembershipType FromLegacyId(int legacyId)
        => TryFromLegacyId(legacyId, out var type)
            ? type
            : throw new SmartEnumNotFoundException($"No {nameof(MembershipType)} has legacy id {legacyId}.");

    /// <summary>
    /// Finds the type for a legacy membership id.
    /// </summary>
    /// <param name="legacyId">The legacy membership id.</param>
    /// <param name="type">The matching type, or null.</param>
    /// <returns>True if a type matched.</returns>
    public static bool TryFromLegacyId(int legacyId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out MembershipType? type)
    {
        return ByLegacyId.Value.TryGetValue(legacyId, out type);
    }
}