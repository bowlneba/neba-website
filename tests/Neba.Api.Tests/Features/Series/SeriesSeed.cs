using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Saves a season, its tournaments and a Series over them for the Series read tests.</summary>
internal static class SeriesSeed
{
    public static async Task<Season> AddSeasonAsync(AppDbContext dbContext, int year, CancellationToken ct)
    {
        var season = SeasonFactory.Create(
            description: $"{year} Season",
            startDate: new DateOnly(year, 1, 1),
            endDate: new DateOnly(year, 12, 31));

        dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync(ct);

        return season;
    }

    public static async Task<Tournament> AddTournamentAsync(
        AppDbContext dbContext, Season season, string name, DateOnly start, DateOnly? end, CancellationToken ct,
        TournamentType? type = null)
    {
        var tournament = TournamentFactory.Create(
            name: name, tournamentType: type, startDate: start, endDate: end ?? start, seasonId: season.Id);

        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    /// <summary>One Series with a Qualifying, a Survivor and a Placement Stage over three new tournaments.</summary>
    public static async Task<SeriesAggregate> AddSeriesAsync(
        AppDbContext dbContext,
        Season season,
        DateOnly firstStart,
        CancellationToken ct,
        string? name = null,
        SeriesFinaleDefinition? finale = null,
        StoredFile? logo = null,
        StageStatus? status = null)
    {
        var first = await AddTournamentAsync(dbContext, season, $"{name ?? "S"} Qualifier", firstStart, null, ct);
        var second = await AddTournamentAsync(dbContext, season, $"{name ?? "S"} Survivor", firstStart.AddDays(30), null, ct);
        var third = await AddTournamentAsync(dbContext, season, $"{name ?? "S"} Placement", firstStart.AddDays(60), null, ct);

        var series = SeriesFactory.CreateFor(
            season,
            [
                (first, StageKind.Qualifying, 8, null, null),
                (second, StageKind.Survivor, 20, second.StartDate, new TimeOnly(9, 0)),
                (third, StageKind.Placement, null, null, null),
            ],
            finale,
            name);

        if (logo is not null)
        {
            series.ChangeLogo(logo);
        }

        dbContext.Series.Add(series);

        if (status is not null && status != StageStatus.Pending)
        {
            // The domain mutators set the timestamps the status check constraint requires.
            var stage = series.Stages.First();
            var now = new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero);

            stage.MarkComputed(now);

            if (status == StageStatus.Official)
            {
                stage.MarkOfficial(now);
            }
        }

        await dbContext.SaveChangesAsync(ct);

        return series;
    }
}