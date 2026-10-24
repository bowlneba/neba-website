using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Memberships.Domain;

namespace Neba.TestFactory.Memberships;

public static class BowlerMembershipFactory
{
    public static readonly MembershipType ValidType = MembershipType.New;
    public static readonly DateOnly ValidStartDate = new(2025, 3, 15);

    /// <summary>
    /// Builds a membership through <c>BowlerMembership.Create</c>. A type that requires a prior membership
    /// gets a qualifying one in its history, so the result is always valid. Pass only active types.
    /// </summary>
    public static BowlerMembership Create(
        MembershipType? type = null,
        DateOnly? startDate = null)
    {
        var membershipType = type ?? ValidType;
        var start = startDate ?? ValidStartDate;

        return BowlerMembership.Create(membershipType, start, PriorHistoryFor(membershipType, start)).Value;
    }

    internal static IReadOnlyCollection<BowlerMembership> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            faker.PickRandom(MembershipType.Purchasable.ToArray()),
            faker.Date.PastDateOnly(10)))];
    }

    public static IReadOnlyCollection<BowlerMembership> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }

    private static IReadOnlyCollection<MembershipSnapshot> PriorHistoryFor(MembershipType type, DateOnly start)
    {
        if (!type.RequiresPriorMembership)
        {
            return [];
        }

        var priorStart = start.AddYears(-2);

        return [new MembershipSnapshot(MembershipType.New, priorStart, MembershipType.New.CalculateEndDate(priorStart))];
    }
}
