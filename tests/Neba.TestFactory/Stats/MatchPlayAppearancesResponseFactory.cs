using Bogus;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAppearancesResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000008";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidFinals = 5;
    public const int ValidTournaments = 10;
    public const int ValidEntries = 12;

    public static MatchPlayAppearancesResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? tournaments = null,
        int? entries = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Finals = finals ?? ValidFinals,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries
        };

    public static IReadOnlyCollection<MatchPlayAppearancesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<MatchPlayAppearancesResponse>()
            .CustomInstantiator(f => new MatchPlayAppearancesResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                Finals = f.Random.Int(1, 15),
                Tournaments = f.Random.Int(1, 15),
                Entries = f.Random.Int(1, 20)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}