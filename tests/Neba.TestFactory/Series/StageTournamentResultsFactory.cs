using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class StageTournamentResultsFactory
{
    public static readonly DateOnly ValidStartDate = new(2025, 3, 22);

    /// <summary>A Completed one-day tournament with no results and no squads.</summary>
    public static StageTournamentResults Create(
        TournamentId? tournamentId = null,
        TournamentStatus? status = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        IReadOnlyList<StageResultRow>? results = null,
        IReadOnlyList<StageSquadFact>? squads = null)
    {
        var start = startDate ?? ValidStartDate;

        return new()
        {
            TournamentId = tournamentId ?? TournamentId.New(),
            Status = status ?? TournamentStatus.Completed,
            StartDate = start,
            EndDate = endDate ?? start,
            Results = results ?? [],
            Squads = squads ?? [],
        };
    }

    public static IReadOnlyList<StageTournamentResults> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new TournamentId(Ulid.BogusString(faker)),
            startDate: faker.Date.PastDateOnly(2),
            results: StageResultRowFactory.Bogus(faker.Random.Int(1, 20), faker.Random.Int())))];
    }
}