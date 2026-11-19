using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesSummaryViewModelFactory
{
    public const string ValidId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";
    public const string ValidName = "2025 Vincent Hall Memorial Series";
    public const int ValidSeasonYear = 2025;
    public static readonly DateOnly ValidSeasonEndDate = new(2025, 12, 31);
    public const string ValidSeasonDateRange = "1/1/2025 to 12/31/2025";

    internal static SeriesSummaryViewModel Create(
        string? id = null,
        string? name = null,
        int? seasonYear = null,
        DateOnly? seasonEndDate = null,
        string? seasonDateRange = null,
        int? qualifyingStageCount = null,
        int? survivorStageCount = null,
        int? placementStageCount = null,
        bool? isLocked = null,
        Uri? logoUrl = null,
        SeriesNextStageViewModel? nextStage = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            SeasonYear = seasonYear ?? ValidSeasonYear,
            SeasonEndDate = seasonEndDate ?? ValidSeasonEndDate,
            SeasonDateRange = seasonDateRange ?? ValidSeasonDateRange,
            QualifyingStageCount = qualifyingStageCount ?? 8,
            SurvivorStageCount = survivorStageCount ?? 2,
            PlacementStageCount = placementStageCount ?? 1,
            IsLocked = isLocked ?? false,
            LogoUrl = logoUrl,
            NextStage = nextStage,
        };

    internal static IReadOnlyCollection<SeriesSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Random.Int(2000, 2040);
            return Create(
                id: Ulid.BogusString(faker),
                name: year.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + faker.Company.CompanyName() + " Series",
                seasonYear: year,
                seasonEndDate: new DateOnly(year, 12, 31),
                qualifyingStageCount: faker.Random.Int(1, 8),
                survivorStageCount: faker.Random.Int(0, 3),
                placementStageCount: faker.Random.Int(0, 1));
        })];
    }
}