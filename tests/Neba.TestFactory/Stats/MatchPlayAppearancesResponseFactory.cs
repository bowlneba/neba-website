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

    public static IReadOnlyCollection<MatchPlayAppearancesResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new MatchPlayAppearancesResponse
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            Finals = faker.Random.Int(1, 15),
            Tournaments = faker.Random.Int(1, 15),
            Entries = faker.Random.Int(1, 20)
        })];
    }

    public static IReadOnlyCollection<MatchPlayAppearancesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}