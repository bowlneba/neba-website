using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Saves bowlers, finalized tournaments with results, and Series over them for the advancement reader tests.</summary>
internal static class AdvancementReaderSeed
{
    public static async Task<IReadOnlyList<Bowler>> AddBowlersAsync(AppDbContext dbContext, int count, CancellationToken ct)
    {
        var bowlers = Enumerable.Range(1, count)
            .Select(index => BowlerFactory.Create(name: NameFactory.Create(firstName: $"First{index:00}", lastName: $"Last{index:00}")))
            .ToList();

        dbContext.Bowlers.AddRange(bowlers);
        await dbContext.SaveChangesAsync(ct);

        return bowlers;
    }

    public static async Task<Tournament> AddTournamentAsync(
        AppDbContext dbContext,
        Season season,
        string name,
        DateOnly start,
        DateOnly? end,
        CancellationToken ct,
        TournamentStatus? status = null,
        IReadOnlyCollection<TournamentResult>? results = null,
        IReadOnlyCollection<Squad>? squads = null,
        TournamentType? type = null,
        bool statsEligible = true)
    {
        var tournament = TournamentFactory.Create(
            name: name,
            tournamentType: type,
            startDate: start,
            endDate: end ?? start,
            statsEligible: statsEligible,
            seasonId: season.Id,
            squads: squads,
            status: status,
            results: results);

        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    public static async Task<SeriesAggregate> AddSeriesAsync(
        AppDbContext dbContext, Season season, Tournament qualifier, Tournament survivor, Tournament placement, CancellationToken ct,
        string? name = null,
        SeriesFinaleDefinition? finale = null)
    {
        var series = SeriesFactory.CreateFor(
            season,
            [
                (qualifier, StageKind.Qualifying, 8, null, null),
                (survivor, StageKind.Survivor, 20, survivor.StartDate, new TimeOnly(9, 0)),
                (placement, StageKind.Placement, null, null, null),
            ],
            finale,
            name: name);

        dbContext.Series.Add(series);
        await dbContext.SaveChangesAsync(ct);

        return series;
    }

    public static TournamentResult Result(Bowler bowler, int place, int points = 10)
        => TournamentResultFactory.Create(bowler.Id, place: place, points: points);
}