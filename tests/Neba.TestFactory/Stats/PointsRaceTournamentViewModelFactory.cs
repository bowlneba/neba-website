using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceTournamentViewModelFactory
{
    public const string ValidTournamentName = "Test Tournament";
    public const int ValidCumulativePoints = 100;
    public static readonly DateOnly ValidTournamentDate = new(2026, 1, 1);

    public static PointsRaceTournamentViewModel Create(
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        int? cumulativePoints = null)
        => new()
        {
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            CumulativePoints = cumulativePoints ?? ValidCumulativePoints
        };

    public static IReadOnlyCollection<PointsRaceTournamentViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var cumulativePoints = 0;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            cumulativePoints += faker.Random.Int(5, 20);
            return new PointsRaceTournamentViewModel
            {
                TournamentName = faker.Company.CompanyName(),
                TournamentDate = faker.Date.PastDateOnly(2),
                CumulativePoints = cumulativePoints
            };
        })];
    }

    public static IReadOnlyCollection<PointsRaceTournamentViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}