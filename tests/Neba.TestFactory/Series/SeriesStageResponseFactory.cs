using Neba.Api.Contracts.Series.GetSeries;

namespace Neba.TestFactory.Series;

public static class SeriesStageResponseFactory
{
    public const string ValidId = "01JBQ5Y7K3M9N2P4R6T8V0W1X5";
    public const string ValidKind = "Qualifying";
    public const string ValidStatus = "Pending";
    public const string ValidTournamentName = "Westgate Lanes";
    public const string ValidTournamentType = "Singles";
    public static readonly DateOnly ValidStartDate = new(2025, 5, 31);

    public static SeriesStageResponse Create(
        string? id = null,
        int? order = null,
        string? kind = null,
        string? status = null,
        int? advancingCount = 8,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null,
        string? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentStartDate = null,
        DateOnly? tournamentEndDate = null,
        string? tournamentType = null,
        int? teamSize = null)
        => new()
        {
            Id = id ?? ValidId,
            Order = order ?? 1,
            Kind = kind ?? ValidKind,
            Status = status ?? ValidStatus,
            AdvancingCount = advancingCount,
            SquadDate = squadDate,
            SquadStartTime = squadStartTime,
            TournamentId = tournamentId ?? SeriesStageInputFactory.ValidTournamentId,
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentStartDate = tournamentStartDate ?? ValidStartDate,
            TournamentEndDate = tournamentEndDate ?? tournamentStartDate ?? ValidStartDate,
            TournamentType = tournamentType ?? ValidTournamentType,
            TeamSize = teamSize ?? 1,
        };

    public static IReadOnlyCollection<SeriesStageResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var start = faker.Date.FutureDateOnly(1);
            return Create(
                id: Ulid.BogusString(faker),
                order: index + 1,
                advancingCount: faker.Random.Int(1, 40),
                tournamentId: Ulid.BogusString(faker),
                tournamentName: faker.Company.CompanyName(),
                tournamentStartDate: start,
                tournamentEndDate: start.AddDays(faker.Random.Int(0, 1)),
                teamSize: faker.Random.Int(1, 3));
        })];
    }
}