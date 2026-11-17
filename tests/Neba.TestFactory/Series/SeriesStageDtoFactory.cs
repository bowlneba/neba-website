using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

internal static class SeriesStageDtoFactory
{
    public static SeriesStageDto Create(
        SeriesStageId? id = null,
        int? order = null,
        string? kind = null,
        string? status = null,
        int? advancingCount = 8,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentStartDate = null,
        DateOnly? tournamentEndDate = null,
        string? tournamentType = null,
        int? teamSize = null)
        => new()
        {
            Id = id ?? SeriesStageId.New(),
            Order = order ?? 1,
            Kind = kind ?? SeriesStageResponseFactory.ValidKind,
            Status = status ?? SeriesStageResponseFactory.ValidStatus,
            AdvancingCount = advancingCount,
            SquadDate = squadDate,
            SquadStartTime = squadStartTime,
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? SeriesStageResponseFactory.ValidTournamentName,
            TournamentStartDate = tournamentStartDate ?? SeriesStageResponseFactory.ValidStartDate,
            TournamentEndDate = tournamentEndDate ?? tournamentStartDate ?? SeriesStageResponseFactory.ValidStartDate,
            TournamentType = tournamentType ?? SeriesStageResponseFactory.ValidTournamentType,
            TeamSize = teamSize ?? 1,
        };

    public static IReadOnlyCollection<SeriesStageDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var start = faker.Date.FutureDateOnly(1);
            return Create(
                order: index + 1,
                advancingCount: faker.Random.Int(1, 40),
                tournamentName: faker.Company.CompanyName(),
                tournamentStartDate: start,
                tournamentEndDate: start.AddDays(faker.Random.Int(0, 1)),
                teamSize: faker.Random.Int(1, 3));
        })];
    }
}
