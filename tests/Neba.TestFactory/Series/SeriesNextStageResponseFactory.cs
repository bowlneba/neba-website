using Neba.Api.Contracts.Series.ListSeries;

namespace Neba.TestFactory.Series;

public static class SeriesNextStageResponseFactory
{
    public const string ValidKind = "Qualifying";
    public const string ValidTournamentId = SeriesStageInputFactory.ValidTournamentId;
    public const string ValidTournamentName = "Westgate Lanes";
    public static readonly DateOnly ValidStartDate = new(2025, 5, 31);
    public static readonly DateOnly ValidEndDate = new(2025, 5, 31);

    public static SeriesNextStageResponse Create(
        string? kind = null,
        string? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentStartDate = null,
        DateOnly? tournamentEndDate = null)
        => new()
        {
            Kind = kind ?? ValidKind,
            TournamentId = tournamentId ?? ValidTournamentId,
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentStartDate = tournamentStartDate ?? ValidStartDate,
            TournamentEndDate = tournamentEndDate ?? ValidEndDate,
        };

    public static IReadOnlyCollection<SeriesNextStageResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var start = faker.Date.FutureDateOnly(1);
            return Create(
                kind: faker.PickRandom("Qualifying", "Survivor", "Placement"),
                tournamentId: Ulid.BogusString(faker),
                tournamentName: faker.Company.CompanyName(),
                tournamentStartDate: start,
                tournamentEndDate: start.AddDays(faker.Random.Int(0, 1)));
        })];
    }
}