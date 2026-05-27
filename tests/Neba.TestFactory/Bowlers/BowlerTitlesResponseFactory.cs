using Neba.Api.Contracts.Bowlers.GetBowlerTitles;

namespace Neba.TestFactory.Bowlers;

public static class BowlerTitlesResponseFactory
{
    public const string ValidBowlerName = "John Smith";
    public const bool ValidHallOfFame = false;

    public static BowlerTitlesResponse Create(
        string? bowlerName = null,
        bool? hallOfFame = null,
        IReadOnlyCollection<BowlerTitleResponse>? titles = null)
        => new()
        {
            BowlerName = bowlerName ?? ValidBowlerName,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
            Titles = titles ?? [],
        };

    public static IReadOnlyCollection<BowlerTitlesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitlesResponse>()
            .CustomInstantiator(f => new BowlerTitlesResponse
            {
                BowlerName = f.Name.FullName(),
                HallOfFame = f.Random.Bool(),
                Titles = BowlerTitleResponseFactory.Bogus(f.Random.Int(0, 10), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}