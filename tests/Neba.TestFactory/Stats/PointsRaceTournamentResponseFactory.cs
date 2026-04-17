using Bogus;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceTournamentResponseFactory
{
    public const string ValidTournamentName = "Spring Open";
    public static readonly DateOnly ValidTournamentDate = new(2024, 3, 15);
    public const int ValidCumulativePoints = 80;

    public static PointsRaceTournamentResponse Create(
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        int? cumulativePoints = null)
        => new()
        {
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            CumulativePoints = cumulativePoints ?? ValidCumulativePoints
        };

    public static IReadOnlyCollection<PointsRaceTournamentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PointsRaceTournamentResponse>()
            .CustomInstantiator(f =>
            {
                var date = f.Date.PastDateOnly(2);
                return new PointsRaceTournamentResponse
                {
                    TournamentName = $"{f.Address.City()} {f.PickRandom("Open", "Classic", "Invitational")}",
                    TournamentDate = date,
                    CumulativePoints = f.Random.Int(20, 500)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}