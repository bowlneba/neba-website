using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class FinalsPerEntryResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000011";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidFinals = 5;
    public const int ValidEntries = 12;
    public const decimal ValidFinalsPerEntry = 0.42m;

    public static FinalsPerEntryResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? entries = null,
        decimal? finalsPerEntry = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Finals = finals ?? ValidFinals,
            Entries = entries ?? ValidEntries,
            FinalsPerEntry = finalsPerEntry ?? ValidFinalsPerEntry
        };

    public static IReadOnlyCollection<FinalsPerEntryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var finals = faker.Random.Int(1, 10);
            var entries = faker.Random.Int(finals, 20);
            return new FinalsPerEntryResponse
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Finals = finals,
                Entries = entries,
                FinalsPerEntry = Math.Round((decimal)finals / entries, 2)
            };
        })];
    }

    public static IReadOnlyCollection<FinalsPerEntryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}