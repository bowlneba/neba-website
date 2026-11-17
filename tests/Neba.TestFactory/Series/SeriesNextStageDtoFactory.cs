using Neba.Api.Features.Series.ListSeries;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

internal static class SeriesNextStageDtoFactory
{
    public static SeriesNextStageDto Create(
        string? kind = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentStartDate = null,
        DateOnly? tournamentEndDate = null)
        => new()
        {
            Kind = kind ?? SeriesNextStageResponseFactory.ValidKind,
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? SeriesNextStageResponseFactory.ValidTournamentName,
            TournamentStartDate = tournamentStartDate ?? SeriesNextStageResponseFactory.ValidStartDate,
            TournamentEndDate = tournamentEndDate ?? tournamentStartDate ?? SeriesNextStageResponseFactory.ValidEndDate,
        };

    public static IReadOnlyCollection<SeriesNextStageDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var start = faker.Date.FutureDateOnly(1);
            return Create(
                kind: faker.PickRandom("Qualifying", "Survivor", "Placement"),
                tournamentName: faker.Company.CompanyName(),
                tournamentStartDate: start,
                tournamentEndDate: start.AddDays(faker.Random.Int(0, 1)));
        })];
    }
}
