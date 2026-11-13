using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;

namespace Neba.TestFactory.HallOfFame;

internal static class MembershipRecordFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";

    public static readonly DateOnly ValidEndDate = new(2020, 12, 31);

    public static MembershipRecord Create(
        BowlerId? bowlerId = null,
        MembershipType? type = null,
        DateOnly? endDate = null)
        => new(
            bowlerId ?? new BowlerId(ValidBowlerId),
            type ?? MembershipType.Renewal,
            endDate ?? ValidEndDate);

    public static IReadOnlyCollection<MembershipRecord> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => new MembershipRecord(
            new BowlerId(Ulid.BogusString(faker)),
            faker.PickRandom(MembershipType.List.ToArray()),
            faker.Date.PastDateOnly(refDate: new DateOnly(2026, 1, 1))))];
    }
}