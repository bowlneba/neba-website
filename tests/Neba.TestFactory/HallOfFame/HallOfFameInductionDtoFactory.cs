using Bogus;

using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionDtoFactory
{
    public const int ValidYear = 2025;

    public static HallOfFameInductionDto Create(
        int? year = null,
        Name? bowlerName = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        string? photoContainer = null,
        string? photoPath = null,
        Uri? photoUri = null)
        => new()
        {
            Year = year ?? ValidYear,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance],
            PhotoContainer = photoContainer,
            PhotoPath = photoPath,
            PhotoUri = photoUri
        };

    public static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, int? seed = null)
    {
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<HallOfFameInductionDto>()
            .CustomInstantiator(f =>
            {
                var hasPhoto = f.Random.Bool();

                return new HallOfFameInductionDto
                {
                    Year = f.Date.PastDateOnly().Year,
                    BowlerName = bowlerNames.GetNext(),
                    Categories = [.. f.PickRandom(HallOfFameCategory.List, f.Random.Int(1, HallOfFameCategory.List.Count))],
                    PhotoContainer = hasPhoto ? f.System.FileName() : null,
                    PhotoPath = hasPhoto ? f.System.FilePath() : null,
                    PhotoUri = hasPhoto ? new Uri(f.Internet.Url()) : null
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}