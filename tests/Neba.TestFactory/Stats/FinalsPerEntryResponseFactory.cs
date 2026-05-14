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

    public static IReadOnlyCollection<FinalsPerEntryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<FinalsPerEntryResponse>()
            .CustomInstantiator(f =>
            {
                var finals = f.Random.Int(1, 10);
                var entries = f.Random.Int(finals, 20);
                return new FinalsPerEntryResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Finals = finals,
                    Entries = entries,
                    FinalsPerEntry = Math.Round((decimal)finals / entries, 2)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}