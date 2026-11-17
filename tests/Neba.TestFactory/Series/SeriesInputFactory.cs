using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.TestFactory.Series;

public static class SeriesInputFactory
{
    public const string ValidName = "Vincent Hall Memorial Series";
    public const string ValidSeasonId = "01JBQ5Y7K3M9N2P4R6T8V0W1X3";

    /// <summary>One Qualifying Stage, no logo and no Finale.</summary>
    public static SeriesInput Create(
        string? name = null,
        string? seasonId = null,
        SeriesLogoInput? logo = null,
        IReadOnlyList<SeriesStageInput>? stages = null,
        SeriesFinaleInput? finale = null)
        => new()
        {
            Name = name ?? ValidName,
            SeasonId = seasonId ?? ValidSeasonId,
            Logo = logo,
            Stages = stages ?? [SeriesStageInputFactory.Create()],
            Finale = finale,
        };

    public static IReadOnlyCollection<SeriesInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: $"{faker.Company.CompanyName()} Series",
            seasonId: Ulid.BogusString(faker),
            stages: [.. SeriesStageInputFactory.Bogus(faker.Random.Int(1, 5), faker.Random.Int())]))];
    }
}
