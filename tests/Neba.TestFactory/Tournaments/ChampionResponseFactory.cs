using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.TestFactory.Tournaments;

public static class ChampionResponseFactory
{
    public const string ValidBowlerName = "Jane Smith";
    public const bool ValidHallOfFame = false;

    public static ChampionResponse Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    public static IReadOnlyCollection<ChampionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ChampionResponse>()
            .CustomInstantiator(f => new ChampionResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                HallOfFame = f.Random.Bool(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ChampionResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
