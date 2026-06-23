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

    public static IReadOnlyCollection<BowlerTitlesResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitlesResponse
        {
            BowlerName = faker.Name.FullName(),
            HallOfFame = faker.Random.Bool(),
            Titles = BowlerTitleResponseFactory.Bogus(faker.Random.Int(0, 10), faker),
        })];
    }

    public static IReadOnlyCollection<BowlerTitlesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}