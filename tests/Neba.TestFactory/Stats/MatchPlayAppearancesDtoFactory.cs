using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAppearancesDtoFactory
{
    public const int ValidFinals = 3;
    public const int ValidTournaments = 10;
    public const int ValidEntries = 15;

    public static MatchPlayAppearancesDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? finals = null,
        int? tournaments = null,
        int? entries = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Finals = finals ?? ValidFinals,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries
        };

    public static IReadOnlyCollection<MatchPlayAppearancesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<MatchPlayAppearancesDto>()
            .CustomInstantiator(f => new MatchPlayAppearancesDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = NameFactory.Bogus(1, seed).Single(),
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
