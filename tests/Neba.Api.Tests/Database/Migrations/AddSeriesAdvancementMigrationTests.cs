using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Migrations;

[IntegrationTest]
[Component("Series")]
public sealed class AddSeriesAdvancementMigrationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string PreviousMigration = "20261117182308_AddSeries";

    // Each test migrates down, so bring the schema back to the latest migration and empty the tables before the next one.
    public async ValueTask InitializeAsync()
    {
        await using var context = fixture.CreateDbContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await fixture.ResetAsync();
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "Up should give existing Series and Stages the default advancement values")]
    public async Task Up_ShouldDefaultExistingRows()
    {
        // Arrange
        // The model already holds the new columns, so seed through EF on the latest schema, then step back to
        // the previous migration (the new columns drop, the rows stay) and migrate up again.
        var ct = TestContext.Current.CancellationToken;
        var seeded = await Configurations.SeriesAdvancementTestData.SeedAsync(fixture);

        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, ct);

        // Act
        await migrator.MigrateAsync(cancellationToken: ct);

        // Assert
        await using var read = fixture.CreateDbContext();
        var series = await read.Series.AsNoTracking().SingleAsync(s => s.Id == seeded.Series.Id, ct);
        var stages = await read.Set<SeriesStage>().AsNoTracking().ToListAsync(ct);

        series.Version.ShouldBe(0);
        series.BackfillAsOf.ShouldBeNull();
        stages.Count.ShouldBe(3);
        stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending);
        stages.ShouldAllBe(stage => !stage.IsStale);
        stages.ShouldAllBe(stage => stage.ComputedAt == null && stage.OfficialAt == null && stage.SlotCount == null);
    }

    [Fact(DisplayName = "Down should drop the advancement tables and columns and keep the Series and Stage rows")]
    public async Task Down_ShouldDropAdvancementSchema_AndKeepRows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Configurations.SeriesAdvancementTestData.SeedAsync(fixture);

        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        // Act
        await migrator.MigrateAsync(PreviousMigration, ct);

        // Assert
        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'app' AND table_name LIKE 'series%'")
            .ToListAsync(ct);
        var rows = await context.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM app.series_stages").SingleAsync(ct);

        tables.Order().ShouldBe(["series", "series_stages"]);
        rows.ShouldBe(3);
    }
}
