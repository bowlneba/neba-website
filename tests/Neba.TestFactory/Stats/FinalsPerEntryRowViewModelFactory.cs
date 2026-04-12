using Bogus;

using Neba.Domain.Bowlers;
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
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? finals = null,
        int? entries = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Finals = finals ?? ValidFinals,
            Entries = entries ?? ValidEntries
        };

    public static IReadOnlyCollection<FinalsPerEntryRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const int entries = 20;

        var faker = new Faker<FinalsPerEntryRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var finals = Math.Max(0, count - currentRank + 1);

                return new FinalsPerEntryRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Finals = finals,
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
