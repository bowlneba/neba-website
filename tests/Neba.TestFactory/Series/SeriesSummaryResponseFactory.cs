using Neba.Api.Contracts.Series.ListSeries;

namespace Neba.TestFactory.Series;

public static class SeriesSummaryResponseFactory
{
    public const string ValidId = SeriesFinaleSummaryResponseFactory.ValidSeriesId;
    public const string ValidName = "2025 Vincent Hall Memorial Series";
    public const string ValidSeasonId = SeriesInputFactory.ValidSeasonId;
    public const string ValidSeasonDescription = "2025 Season";
    public static readonly DateOnly ValidSeasonStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidSeasonEndDate = new(2025, 12, 31);

    public static SeriesSummaryResponse Create(
        string? id = null,
        string? name = null,
        string? seasonId = null,
        string? seasonDescription = null,
        DateOnly? seasonStartDate = null,
        DateOnly? seasonEndDate = null,
        int? qualifyingStageCount = null,
        int? survivorStageCount = null,
        int? placementStageCount = null,
        bool? isLocked = null,
        Uri? logoUrl = null,
        SeriesNextStageResponse? nextStage = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            SeasonId = seasonId ?? ValidSeasonId,
            SeasonDescription = seasonDescription ?? ValidSeasonDescription,
            SeasonStartDate = seasonStartDate ?? ValidSeasonStartDate,
            SeasonEndDate = seasonEndDate ?? ValidSeasonEndDate,
            QualifyingStageCount = qualifyingStageCount ?? 8,
            SurvivorStageCount = survivorStageCount ?? 2,
            PlacementStageCount = placementStageCount ?? 1,
            IsLocked = isLocked ?? false,
            LogoUrl = logoUrl,
            NextStage = nextStage,
        };

    public static IReadOnlyCollection<SeriesSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Random.Int(2020, 2030);
            return Create(
                id: Ulid.BogusString(faker),
                name: $"{year} {faker.Company.CompanyName()} Series",
                seasonId: Ulid.BogusString(faker),
                seasonDescription: $"{year} Season",
                seasonStartDate: new DateOnly(year, 1, 1),
                seasonEndDate: new DateOnly(year, 12, 31),
                qualifyingStageCount: faker.Random.Int(0, 8),
                survivorStageCount: faker.Random.Int(0, 3),
                placementStageCount: faker.Random.Int(0, 1),
                isLocked: faker.Random.Bool());
        })];
    }
}
