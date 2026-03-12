using Bogus;

using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionDtoFactory
{
    public static HallOfFameInductionDto Create(
        int? year = null,
        Name? bowlerName = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        string? photoContainer = null,
        string? photoPath = null,
        Uri? photoUri = null)
        => new()
        {
            Year = year ?? 2025,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance],
            PhotoContainer = photoContainer,
            PhotoPath = photoPath,
            PhotoUri = photoUri
        };

    public static HallOfFameInductionDto Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HallOfFameInductionDto>()
            .CustomInstantiator(f =>
            {
                var hasPhoto = f.Random.Bool();

                return new HallOfFameInductionDto
                {
                    Year = f.Date.PastDateOnly().Year,
                    BowlerName = NameFactory.Bogus(seed: seed),
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