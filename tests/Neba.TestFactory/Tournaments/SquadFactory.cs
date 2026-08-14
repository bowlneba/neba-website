using Bogus;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class SquadFactory
{
    public static readonly DateTimeOffset ValidBowlingDateTime = new(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);

    public static Squad Create(
        DateTimeOffset? bowlingDateTime = null,
        int? maxEntries = null,
        int? legacyId = null)
    {
        var result = Squad.Create(
            bowlingDateTime: bowlingDateTime ?? ValidBowlingDateTime,
            maxEntries: maxEntries,
            legacyId: legacyId);

        if (result.IsError)
        {
            throw new InvalidOperationException($"Failed to create squad: {result.Errors[0].Description}");
        }

        return result.Value;
    }

    internal static IReadOnlyCollection<Squad> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var result = Squad.Create(
                bowlingDateTime: faker.Date.FutureOffset(1),
                maxEntries: faker.Random.Bool() ? faker.Random.Int(1, 200) : null,
                legacyId: faker.Random.Bool() ? faker.Random.Int(1, 9999) : null);

            if (result.IsError)
            {
                throw new InvalidOperationException($"Failed to create squad: {result.Errors[0].Description}");
            }

            return result.Value;
        })];
    }

    public static IReadOnlyCollection<Squad> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}