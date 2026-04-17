using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearStandingDtoFactory
{
    public const int ValidPoints = 500;
    public const int ValidTournaments = 10;
    public const int ValidEntries = 15;
    public const int ValidFinals = 3;
    public const decimal ValidAverageFinish = 4.5m;
    public const decimal ValidWinnings = 2500m;

    public static BowlerOfTheYearStandingDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        int? entries = null,
        int? finals = null,
        decimal? averageFinish = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            Entries = entries ?? ValidEntries,
            Finals = finals ?? ValidFinals,
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<BowlerOfTheYearStandingDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerOfTheYearStandingDto>()
            .CustomInstantiator(f => new BowlerOfTheYearStandingDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = NameFactory.Bogus(1, seed).Single(),
                Points = f.Random.Int(50, 1000),
                Tournaments = f.Random.Int(1, 15),
                Entries = f.Random.Int(1, 20),
                Finals = f.Random.Int(0, 10),
                AverageFinish = f.Random.Bool() ? f.Random.Decimal(1, 10) : null,
                Winnings = f.Random.Decimal(0, 5000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
