using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Migrations;

[IntegrationTest]
[Component("Tournaments")]
public sealed class UniquePreviousTournamentMigrationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string PreviousMigration = "20261120185102_AddSeriesAdvancement";
    private const string IndexName = "ix_tournaments_previous_tournament_id";

    // Each test migrates down, so bring the schema back to the latest migration before the next one.
    public async ValueTask InitializeAsync()
    {
        await using var context = fixture.CreateDbContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await fixture.ResetAsync();
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static Task<string> ReadIndexDefinitionAsync(DbContext context, CancellationToken ct)
        => context.Database
            .SqlQueryRaw<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'app' AND indexname = '{IndexName}'")
            .SingleAsync(ct);

    [Fact(DisplayName = "Up should make the previous tournament index unique")]
    public async Task Up_ShouldMakeIndexUnique()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, ct);

        // Act
        await migrator.MigrateAsync(cancellationToken: ct);

        // Assert
        var definition = await ReadIndexDefinitionAsync(context, ct);
        definition.ShouldContain("CREATE UNIQUE INDEX");
        definition.ShouldContain("previous_tournament_id");
    }

    [Fact(DisplayName = "Down should bring back the plain previous tournament index")]
    public async Task Down_ShouldRestorePlainIndex()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        // Act
        await migrator.MigrateAsync(PreviousMigration, ct);

        // Assert
        var definition = await ReadIndexDefinitionAsync(context, ct);
        definition.ShouldNotContain("UNIQUE");
        definition.ShouldContain("previous_tournament_id");
    }
}