using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAppearancesRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidFinals = 5;
    public const int ValidTournaments = 6;
    public const int ValidEntries = 8;

    public static MatchPlayAppearancesRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? tournaments = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Finals = finals ?? ValidFinals,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries
        };

    public static IReadOnlyCollection<MatchPlayAppearancesRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;

        var faker = new Faker<MatchPlayAppearancesRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var finals = Math.Max(0, count - currentRank + 1);
                var tournaments = finals + f.Random.Int(0, 5);
                var entries = tournaments + f.Random.Int(0, 8);

                return new MatchPlayAppearancesRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Finals = finals,
                    Tournaments = tournaments,
                    Entries = entries
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}