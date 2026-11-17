using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private async Task<(Season Season, IReadOnlyList<Tournament> Tournaments)> SeedAsync(int tournamentCount)
    {
        var season = SeasonFactory.Create();
        var tournaments = Enumerable.Range(0, tournamentCount)
            .Select(i => TournamentFactory.Create(
                name: $"Tournament {i + 1}",
                startDate: new DateOnly(2025, 1, 10).AddMonths(i),
                endDate: new DateOnly(2025, 1, 10).AddMonths(i),
                seasonId: season.Id))
            .ToList();

        await using var context = fixture.CreateDbContext();
        context.Seasons.Add(season);
        context.Tournaments.AddRange(tournaments);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (season, tournaments);
    }

    private static SeriesAggregate Build(Season season, IReadOnlyList<Tournament> tournaments, SeriesFinaleDefinition? finale = null, string? name = null, Neba.Api.Features.Storage.Domain.StoredFile? logo = null)
    {
        var series = SeriesFactory.CreateFor(
            season,
            [
                (tournaments[0], StageKind.Qualifying, 8, null, null),
                (tournaments[1], StageKind.Survivor, 20, tournaments[1].StartDate, new TimeOnly(9, 0)),
                (tournaments[2], StageKind.Placement, null, null, null),
            ],
            finale,
            name);

        if (logo is not null)
        {
            series.ChangeLogo(logo);
        }

        return series;
    }

    [Fact(DisplayName = "A Series with mixed Stages, a Finale with a center and a logo should round-trip")]
    public async Task RoundTrip_ShouldPersistEverything()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var center = BowlingCenterFactory.Create();
        await using (var setup = fixture.CreateDbContext())
        {
            setup.BowlingCenters.Add(center);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var finale = SeriesFinaleDefinitionFactory.Create(bowlingCenterId: center.CertificationNumber, date: new DateOnly(2025, 12, 1));
        var logo = StoredFileFactory.Create();
        var series = Build(season, tournaments, finale, logo: logo);

        await using (var write = fixture.CreateDbContext())
        {
            write.Series.Add(series);
            await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.Id.ShouldBe(series.Id);
        loaded.SeasonId.ShouldBe(season.Id);
        loaded.Logo.ShouldBe(logo);
        loaded.Finale.ShouldNotBeNull();
        loaded.Finale.FinalistCount.ShouldBe(4);
        loaded.Finale.Date.ShouldBe(new DateOnly(2025, 12, 1));
        loaded.Finale.StartTime.ShouldBe(new TimeOnly(14, 0));
        loaded.Finale.BowlingCenterId.ShouldBe(center.CertificationNumber);

        var stages = loaded.Stages.OrderBy(s => s.Order).ToList();
        stages.Select(s => s.Kind).ShouldBe([StageKind.Qualifying, StageKind.Survivor, StageKind.Placement]);
        stages.Select(s => s.AdvancingCount).ShouldBe([8, 20, null]);
        stages[0].SquadDesignation.ShouldBeNull();
        stages[1].SquadDesignation!.Date.ShouldBe(tournaments[1].StartDate);
        stages[1].SquadDesignation!.StartTime.ShouldBe(new TimeOnly(9, 0));
        stages[1].SquadDesignation!.SquadId.ShouldBeNull();
        stages[2].Status.ShouldBe(StageStatus.Pending);
        loaded.Stages.Select(s => s.Id).ShouldBe(series.Stages.Select(s => s.Id), ignoreOrder: true);
    }

    [Fact(DisplayName = "A Series with a Finale and no center should round-trip with a null center")]
    public async Task RoundTrip_ShouldPersistFinaleWithoutCenter()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var series = Build(season, tournaments, SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));

        await using (var write = fixture.CreateDbContext())
        {
            write.Series.Add(series);
            await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.Finale.ShouldNotBeNull();
        loaded.Finale.BowlingCenterId.ShouldBeNull();
        loaded.Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "A Series with no Finale should read back with a null Finale")]
    public async Task RoundTrip_ShouldReadNullFinale_WhenSeriesHasNone()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var series = Build(season, tournaments);

        await using (var write = fixture.CreateDbContext())
        {
            write.Series.Add(series);
            await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.Finale.ShouldBeNull();
        loaded.HasFinale.ShouldBeFalse();
    }

    [Fact(DisplayName = "Removing the Finale and saving should clear its columns")]
    public async Task Update_ShouldClearFinale_WhenFinaleIsRemoved()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var series = Build(season, tournaments, SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));

        await using (var write = fixture.CreateDbContext())
        {
            write.Series.Add(series);
            await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using (var edit = fixture.CreateDbContext())
        {
            var tracked = await edit.Series.Include(s => s.Stages).SingleAsync(TestContext.Current.CancellationToken);
            var facts = tournaments.ToDictionary(t => t.Id, t => StageTournamentFactsFactory.Create(t.Id, t.Name, t.SeasonId, t.StartDate, t.EndDate));
            var submitted = tracked.Stages.Select(s => SeriesStageDefinitionFactory.Create(
                s.Id, s.Kind, s.TournamentId, s.AdvancingCount, s.SquadDesignation?.Date, s.SquadDesignation?.StartTime)).ToList();

            tracked.ReplaceConfiguration(submitted, null, facts).IsError.ShouldBeFalse();
            await edit.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var read = fixture.CreateDbContext();
        (await read.Series.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "The Finale center foreign key should reject an unknown certification number")]
    public async Task Save_ShouldReject_WhenFinaleCenterDoesNotExist()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var unknown = CertificationNumberFactory.Create("99999");
        var series = Build(season, tournaments, SeriesFinaleDefinitionFactory.Create(bowlingCenterId: unknown, date: new DateOnly(2025, 12, 1)));

        await using var write = fixture.CreateDbContext();
        write.Series.Add(series);

        // Act & Assert
        await Should.ThrowAsync<DbUpdateException>(() => write.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "The unique tournament_id index should reject a second Stage on the same tournament in another Series")]
    public async Task Save_ShouldReject_WhenTournamentIsAStageInTwoSeries()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        await using (var first = fixture.CreateDbContext())
        {
            first.Series.Add(Build(season, tournaments, name: "First"));
            await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var second = fixture.CreateDbContext();
        second.Series.Add(Build(season, tournaments, name: "Second"));

        // Act & Assert
        await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "The check constraint should reject a partly filled Finale")]
    public async Task Save_ShouldReject_WhenFinaleColumnsArePartlyFilled()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        var series = Build(season, tournaments);
        await using var write = fixture.CreateDbContext();
        write.Series.Add(series);
        await write.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => write.Database.ExecuteSqlRawAsync(
            "UPDATE app.series SET finale_finalist_count = 4", TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Deleting a Series should delete its Stages")]
    public async Task Delete_ShouldCascadeToStages()
    {
        // Arrange
        var (season, tournaments) = await SeedAsync(3);
        await using (var write = fixture.CreateDbContext())
        {
            write.Series.Add(Build(season, tournaments));
            await write.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using (var delete = fixture.CreateDbContext())
        {
            await delete.Series.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var read = fixture.CreateDbContext();
        (await read.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM app.series_stages").SingleAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }
}
