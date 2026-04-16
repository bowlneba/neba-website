using Bogus;

using Neba.Application.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceTournamentDtoFactory
{
    public const string ValidTournamentName = "Test Tournament";
    public const int ValidCumulativePoints = 100;
    public static readonly DateOnly ValidTournamentDate = new(2026, 1, 1);

    public static PointsRaceTournamentDto Create(
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        int? cumulativePoints = null)
        => new()
        {
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            CumulativePoints = cumulativePoints ?? ValidCumulativePoints
        };

    public static IReadOnlyCollection<PointsRaceTournamentDto> Bogus(int count, int? seed = null)
    {
        var cumulativePoints = 0;

        var faker = new Faker<PointsRaceTournamentDto>()
            .CustomInstantiator(f =>
            {
                cumulativePoints += f.Random.Int(5, 20);

                return new PointsRaceTournamentDto
                {
                    TournamentName = f.Company.CompanyName(),
                    TournamentDate = f.Date.PastDateOnly(2),
                    CumulativePoints = cumulativePoints
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
