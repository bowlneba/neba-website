using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class FinalsPerEntryRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidFinals = 5;
    public const int ValidEntries = 10;

    public static FinalsPerEntryRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Finals = finals ?? ValidFinals,
            Entries = entries ?? ValidEntries
        };

    internal static IReadOnlyCollection<FinalsPerEntryRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var rank = 1;
        const int entries = 20;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var currentRank = rank++;
            var finals = Math.Max(0, count - currentRank + 1);
            return new FinalsPerEntryRowViewModel
            {
                Rank = currentRank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Finals = finals,
                Entries = entries
            };
        })];
    }

    public static IReadOnlyCollection<FinalsPerEntryRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}