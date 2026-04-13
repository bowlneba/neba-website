using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceSeriesViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";

    public static PointsRaceSeriesViewModel Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        IReadOnlyCollection<PointsRaceTournamentViewModel>? results = null)
        => new()
        {
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Results = results ?? [PointsRaceTournamentViewModelFactory.Create()]
        };

    public static IReadOnlyCollection<PointsRaceSeriesViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PointsRaceSeriesViewModel>()
            .CustomInstantiator(f => new PointsRaceSeriesViewModel
            {
                BowlerId = Ulid.Bogus(f),
                BowlerName = f.Name.FullName(),
                Results = PointsRaceTournamentViewModelFactory.Bogus(f.Random.Int(1, 5), seed)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}