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

    internal static IReadOnlyCollection<PointsRaceTournamentResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var date = faker.Date.PastDateOnly(2);
            return new PointsRaceTournamentResponse
            {
                TournamentName = $"{faker.Address.City()} {faker.PickRandom("Open", "Classic", "Invitational")}",
                TournamentDate = date,
                CumulativePoints = faker.Random.Int(20, 500)
            };
        })];
    }

    public static IReadOnlyCollection<PointsRaceTournamentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}