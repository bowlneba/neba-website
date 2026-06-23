using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceSeriesViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";

    public static PointsRaceSeriesViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        IReadOnlyCollection<PointsRaceTournamentViewModel>? results = null)
        => new()
        {
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Results = results ?? [PointsRaceTournamentViewModelFactory.Create()]
        };

    public static IReadOnlyCollection<PointsRaceSeriesViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new PointsRaceSeriesViewModel
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            Results = PointsRaceTournamentViewModelFactory.Bogus(faker.Random.Int(1, 5), faker)
        })];
    }

    public static IReadOnlyCollection<PointsRaceSeriesViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}