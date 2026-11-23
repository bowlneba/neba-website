using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class RecomputeSeriesAdvancementInputFactory
{
    public const int ValidVersion = 3;
    public const bool ValidConfirmResetOfficial = false;

    public static RecomputeSeriesAdvancementInput Create(
        int? version = null,
        bool? confirmResetOfficial = null)
        => new()
        {
            Version = version ?? ValidVersion,
            ConfirmResetOfficial = confirmResetOfficial ?? ValidConfirmResetOfficial,
        };

    internal static IReadOnlyCollection<RecomputeSeriesAdvancementInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            version: faker.Random.Int(0, 50),
            confirmResetOfficial: faker.Random.Bool()))];
    }

    public static IReadOnlyCollection<RecomputeSeriesAdvancementInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}