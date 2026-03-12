using Bogus;

using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Domain.HallOfFame;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionResponseFactory
{
    public static HallOfFameInductionResponse Create(
        int? year = null,
        string? bowlerName = null,
        IReadOnlyCollection<string>? categories = null,
        Uri? photoUri = null)
        => new()
        {
            Year = year ?? 2024,
            BowlerName = bowlerName ?? "John Doe",
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance.Name, HallOfFameCategory.MeritoriousService.Name],
            PhotoUri = photoUri
        };

    public static IReadOnlyCollection<HallOfFameInductionResponse> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<HallOfFameInductionResponse>()
            .CustomInstantiator(f => new()
            {
                Year = f.Date.PastDateOnly(50).Year,
                BowlerName = bowlerNamePool.GetNext().ToFormalName(),
                Categories = [.. f.PickRandom(HallOfFameCategory.List.Select(c => c.Name), 2)],
                PhotoUri = f.Random.Bool() ? new Uri(f.Internet.Url()) : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}