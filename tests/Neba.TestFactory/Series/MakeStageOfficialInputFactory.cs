using Neba.Api.Contracts.Series.MakeStageOfficial;

namespace Neba.TestFactory.Series;

public static class MakeStageOfficialInputFactory
{
    public const int ValidVersion = 3;

    public static MakeStageOfficialInput Create(int? version = null)
        => new() { Version = version ?? ValidVersion };

    internal static IReadOnlyCollection<MakeStageOfficialInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<MakeStageOfficialInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
