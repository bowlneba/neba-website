using Microsoft.EntityFrameworkCore;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using Npgsql;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Database.Configurations;

/// <summary>
/// Seeds a Series with Stages and a bowler, and inserts advancement rows with SQL. The advancement entities have no
/// public factory yet (the domain logic comes later), so the configuration tests write their rows directly.
/// </summary>
internal static class SeriesAdvancementTestData
{
    internal sealed record Seeded(
        SeriesAggregate Series,
        SeriesStage QualifyingStage,
        SeriesStage SurvivorStage,
        SeriesStage PlacementStage,
        Bowler Bowler);

    internal static async Task<Seeded> SeedAsync(AppDbContextFixture fixture)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        var tournaments = Enumerable.Range(0, 3)
            .Select(i => TournamentFactory.Create(
                name: $"Tournament {i + 1}",
                startDate: new DateOnly(2025, 1, 10).AddMonths(i),
                endDate: new DateOnly(2025, 1, 10).AddMonths(i),
                seasonId: season.Id))
            .ToList();
        var series = SeriesFactory.CreateFor(
            season,
            [
                (tournaments[0], StageKind.Qualifying, 8, null, null),
                (tournaments[1], StageKind.Survivor, 20, tournaments[1].StartDate, new TimeOnly(9, 0)),
                (tournaments[2], StageKind.Placement, null, null, null),
            ]);
        var bowler = BowlerFactory.Create();

        await using var context = fixture.CreateDbContext();
        context.Seasons.Add(season);
        context.Tournaments.AddRange(tournaments);
        context.Bowlers.Add(bowler);
        context.Series.Add(series);
        await context.SaveChangesAsync(ct);

        var stages = series.Stages.OrderBy(stage => stage.Order).ToList();

        return new Seeded(series, stages[0], stages[1], stages[2], bowler);
    }

    /// <summary>Inserts a Stage entry from the Qualifying Stage, or a Backfill entry when <paramref name="backfill"/> is true. Other arguments are SQL literals.</summary>
    internal static Task<int> InsertFieldEntryAsync(
        AppDbContextFixture fixture,
        Seeded seeded,
        Bowler? bowler = null,
        bool backfill = false,
        string place = "1",
        string backfillPoints = "NULL",
        string viaOverride = "FALSE")
    {
        List<object> parameters = [Ulid.NewUlid().ToString(), (bowler ?? seeded.Bowler).Id.ToString(), seeded.Series.Id.ToString()];

        if (!backfill)
        {
            parameters.Add(seeded.QualifyingStage.Id.ToString());
        }

        return ExecuteAsync(
            fixture,
            $$"""
            INSERT INTO app.series_field_entries (domain_id, bowler_id, series_id, source_stage_id, place, backfill_points, via_override)
            SELECT {0}, {1}, s.id, {{(backfill ? "NULL" : "{3}")}}, {{place}}, {{backfillPoints}}, {{viaOverride}}
            FROM app.series s WHERE s.domain_id = {2}
            """,
            [.. parameters]);
    }

    internal static Task<int> InsertSurvivorOutcomeAsync(
        AppDbContextFixture fixture, Seeded seeded, string blockTotal = "1200", string advanced = "TRUE", Bowler? bowler = null)
        => ExecuteAsync(
            fixture,
            $$"""
            INSERT INTO app.series_survivor_outcomes (series_stage_id, bowler_id, block_total, advanced)
            SELECT s.id, {0}, {{blockTotal}}, {{advanced}}
            FROM app.series_stages s WHERE s.domain_id = {1}
            """,
            [(bowler ?? seeded.Bowler).Id.ToString(), seeded.SurvivorStage.Id.ToString()]);

    internal static Task<int> InsertPositionAsync(
        AppDbContextFixture fixture, Seeded seeded, string position = "1", string bowled = "FALSE", string isFinalist = "NULL", Bowler? bowler = null)
        => ExecuteAsync(
            fixture,
            $$"""
            INSERT INTO app.series_positions (series_id, bowler_id, position, bowled, is_finalist)
            SELECT s.id, {0}, {{position}}, {{bowled}}, {{isFinalist}}
            FROM app.series s WHERE s.domain_id = {1}
            """,
            [(bowler ?? seeded.Bowler).Id.ToString(), seeded.Series.Id.ToString()]);

    internal static Task<int> InsertOverrideAsync(
        AppDbContextFixture fixture, Seeded seeded, string applied = "FALSE", int kind = 1, Bowler? bowler = null)
        => ExecuteAsync(
            fixture,
            $$"""
            INSERT INTO app.series_stage_overrides (domain_id, series_stage_id, bowler_id, kind, reason, created_by, created_at_utc, applied)
            SELECT {0}, s.id, {1}, {{kind}}, 'reason', 'admin', now(), {{applied}}
            FROM app.series_stages s WHERE s.domain_id = {2}
            """,
            [Ulid.NewUlid().ToString(), (bowler ?? seeded.Bowler).Id.ToString(), seeded.QualifyingStage.Id.ToString()]);

    internal static async Task<int> CountAsync(AppDbContextFixture fixture, string table)
    {
        await using var context = fixture.CreateDbContext();

        // The table name is a constant from the calling test, not user input.
#pragma warning disable EF1002
        return await context.Database
            .SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM app.{table}")
            .SingleAsync(TestContext.Current.CancellationToken);
#pragma warning restore EF1002
    }

    /// <summary>Runs <paramref name="action"/> and returns the Postgres SQLSTATE it fails with, or null when it succeeds.</summary>
    internal static async Task<string?> SqlStateOfAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgres)
                {
                    return postgres.SqlState;
                }
            }

            throw;
        }

        return null;
    }

    internal static async Task<int> ExecuteSqlAsync(AppDbContextFixture fixture, string sql)
    {
        await using var context = fixture.CreateDbContext();

        return await context.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);
    }

    private static async Task<int> ExecuteAsync(AppDbContextFixture fixture, string sql, object[] parameters)
    {
        await using var context = fixture.CreateDbContext();

        return await context.Database.ExecuteSqlRawAsync(sql, parameters, TestContext.Current.CancellationToken);
    }
}
