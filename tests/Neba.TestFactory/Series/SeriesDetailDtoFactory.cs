using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;

namespace Neba.TestFactory.Series;

internal static class SeriesDetailDtoFactory
{
    /// <summary>One Qualifying Stage, no Finale and no logo.</summary>
    public static SeriesDetailDto Create(
        SeriesId? id = null,
        string? name = null,
        SeasonId? seasonId = null,
        string? seasonDescription = null,
        DateOnly? seasonStartDate = null,
        DateOnly? seasonEndDate = null,
        bool? isLocked = null,
        SeriesFinaleDto? finale = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null,
        string? logoContentType = null,
        long? logoSizeInBytes = null,
        IReadOnlyCollection<SeriesStageDto>? stages = null)
        => new()
        {
            Id = id ?? SeriesId.New(),
            Name = name ?? SeriesSummaryResponseFactory.ValidName,
            SeasonId = seasonId ?? SeasonId.New(),
            SeasonDescription = seasonDescription ?? SeriesSummaryResponseFactory.ValidSeasonDescription,
            SeasonStartDate = seasonStartDate ?? SeriesSummaryResponseFactory.ValidSeasonStartDate,
            SeasonEndDate = seasonEndDate ?? SeriesSummaryResponseFactory.ValidSeasonEndDate,
            IsLocked = isLocked ?? false,
            Finale = finale,
            LogoUrl = logoUrl,
            LogoContainer = logoContainer,
            LogoPath = logoPath,
            LogoContentType = logoContentType,
            LogoSizeInBytes = logoSizeInBytes,
            Stages = stages ?? [SeriesStageDtoFactory.Create()],
        };

    public static IReadOnlyCollection<SeriesDetailDto> Bogus(int count, int? seed = null)
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
                isLocked: faker.Random.Bool(),
                finale: faker.Random.Bool() ? SeriesFinaleDtoFactory.Bogus(1, faker.Random.Int()).Single() : null,
                stages: SeriesStageDtoFactory.Bogus(faker.Random.Int(1, 11), faker.Random.Int()));
        })];
    }
}
