using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Neba.Api.Database;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Migrations;

[IntegrationTest]
[Component("Tournaments")]
public sealed class AddTournamentOilPatternAnnouncedAtMigrationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>
{
    private const string PreviousMigration = "20261027170310_AddArticleAnnouncedAt";

    private static readonly DateTimeOffset RevealDate = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Backfills oil_pattern_announced_at_utc with the reveal date for tournaments that have one and leaves the rest null")]
    public async Task Up_ShouldBackfillTournamentsWithRevealDate_AndLeaveOthersNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousMigration, ct);

        await InsertTournamentAsync(context, "01JTOURNAMENTREVEAL0000000", "with-reveal", RevealDate, ct);
        await InsertTournamentAsync(context, "01JTOURNAMENTNOREVEAL00000", "no-reveal", revealDate: null, ct);

        // Act
        await migrator.MigrateAsync(cancellationToken: ct);

        // Assert
        var withReveal = await ReadAnnouncedAtAsync(context, "with-reveal", ct);
        var noReveal = await ReadAnnouncedAtAsync(context, "no-reveal", ct);

        withReveal.ShouldBe(RevealDate);
        noReveal.ShouldBeNull();
    }

    private static async Task InsertTournamentAsync(
        AppDbContext context, string domainId, string name, DateTimeOffset? revealDate, CancellationToken ct)
    {
        // The row needs no season for this check, so skip the foreign key for the insert.
        // The same connection must run the SET and the INSERT, so hold it open between them.
        await context.Database.OpenConnectionAsync(ct);
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;", ct);

        // EF cannot bind a null value without a type, so a missing reveal date is left out of the INSERT.
        var revealColumn = revealDate.HasValue ? ", oil_pattern_reveal_date_utc" : string.Empty;
        var revealValue = revealDate.HasValue ? ", {2}" : string.Empty;
        object[] parameters = revealDate.HasValue ? [domainId, name, revealDate.Value] : [domainId, name];

#pragma warning disable EF1002 // Only test constants are interpolated into the SQL text.
        await context.Database.ExecuteSqlRawAsync(
            $$"""
            INSERT INTO app.tournaments (
                domain_id, name, season_id, tournament_type, start_date, end_date, stats_eligible,
                title_eligible, entry_fee, neba_added_money, status{{revealColumn}})
            VALUES ({0}, {1}, '01JSEASONNOTINSERTED00000', 1, '2026-04-01', '2026-04-02', true,
                true, 100, 0, 0{{revealValue}});
            """,
            parameters,
            ct);
#pragma warning restore EF1002

        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;", ct);
        await context.Database.CloseConnectionAsync();
    }

    private static async Task<DateTimeOffset?> ReadAnnouncedAtAsync(AppDbContext context, string name, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT oil_pattern_announced_at_utc FROM app.tournaments WHERE name = @name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var value = await command.ExecuteScalarAsync(ct);

        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => null
        };
    }
}
