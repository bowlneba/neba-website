using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class FieldStepFactory
{
    public const string ValidLabel = "Qualifiers";
    public const string ValidDetail = "2 events, top finishers";
    public const int ValidCount = 16;

    /// <summary>A Qualifying step of 16 bowlers, not a ceiling.</summary>
    public static FieldStep Create(
        string? label = null,
        string? detail = null,
        int? count = null,
        string? kind = null,
        bool isCeiling = false)
        => new(label ?? ValidLabel, detail ?? ValidDetail, count ?? ValidCount, kind ?? StageKinds.Qualifying, isCeiling);

    public static IReadOnlyCollection<FieldStep> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            label: faker.Commerce.Department(),
            detail: faker.Lorem.Sentence(3),
            count: faker.Random.Int(1, 100),
            kind: StageKinds.All[faker.Random.Int(0, StageKinds.All.Count - 1)]))];
    }
}