using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListSeries;

namespace Neba.TestFactory.Series;

internal static class SeriesSummaryDtoFactory
{
    public static SeriesSummaryDto Create(
        SeriesId? id = null,
        string? name = null,
        SeasonId? seasonId = null,
        string? seasonDescription = null,
        DateOnly? seasonStartDate = null,
        DateOnly? seasonEndDate = null,
        int? qualifyingStageCount = null,
        int? survivorStageCount = null,
        int? placementStageCount = null,
        bool? isLocked = null,
        Uri? logoUrl = null,
        SeriesNextStageDto? nextStage = null)
        => new()
        {
            Id = id ?? SeriesId.New(),
            Name = name ?? SeriesSummaryResponseFactory.ValidName,
            SeasonId = seasonId ?? SeasonId.New(),
            SeasonDescription = seasonDescription ?? SeriesSummaryResponseFactory.ValidSeasonDescription,
            SeasonStartDate = seasonStartDate ?? SeriesSummaryResponseFactory.ValidSeasonStartDate,
            SeasonEndDate = seasonEndDate ?? SeriesSummaryResponseFactory.ValidSeasonEndDate,
            QualifyingStageCount = qualifyingStageCount ?? 8,
            SurvivorStageCount = survivorStageCount ?? 2,
            PlacementStageCount = placementStageCount ?? 1,
            IsLocked = isLocked ?? false,
            LogoUrl = logoUrl,
            NextStage = nextStage,
        };

    public static IReadOnlyCollection<SeriesSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Random.Int(2020, 2030);
            return Create(
                name: $"{year} {faker.Company.CompanyName()} Series",
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