using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionResponseFactory
{
    public const int ValidYear = 2024;
    public const string ValidBowlerName = "John Doe";

    public static HallOfFameInductionResponse Create(
        int? year = null,
        string? bowlerName = null,
        IReadOnlyCollection<string>? categories = null,
        Uri? photoUri = null)
        => new()
        {
            Year = year ?? ValidYear,
            BowlerName = bowlerName ?? ValidBowlerName,
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance.Name, HallOfFameCategory.MeritoriousService.Name],
            PhotoUri = photoUri
        };

    internal static IReadOnlyCollection<HallOfFameInductionResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new HallOfFameInductionResponse
        {
            Year = faker.Date.PastDateOnly(50).Year,
            BowlerName = bowlerNamePool.GetNext().ToFormalName(),
            Categories = [.. faker.PickRandom(HallOfFameCategory.List.Select(c => c.Name), 2)],
            PhotoUri = faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null
        })];
    }

    public static IReadOnlyCollection<HallOfFameInductionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}