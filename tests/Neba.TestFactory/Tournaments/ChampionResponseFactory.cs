using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.TestFactory.Tournaments;

public static class ChampionResponseFactory
{
    public const string ValidBowlerName = "Jane Smith";
    public const string ValidBowlerLastName = "Smith";
    public const string ValidBowlerFirstName = "Jane";
    public const bool ValidHallOfFame = false;

    public static ChampionResponse Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        string? bowlerLastName = null,
        string? bowlerFirstName = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            BowlerLastName = bowlerLastName ?? ValidBowlerLastName,
            BowlerFirstName = bowlerFirstName ?? ValidBowlerFirstName,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    public static IReadOnlyCollection<ChampionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ChampionResponse>()
            .CustomInstantiator(f =>
            {
                var firstName = f.Name.FirstName();
                var lastName = f.Name.LastName();
                return new ChampionResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = $"{firstName} {lastName}",
                    BowlerLastName = lastName,
                    BowlerFirstName = firstName,
                    HallOfFame = f.Random.Bool(),
                };
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