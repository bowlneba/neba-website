using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace Neba.TestFactory.Infrastructure;

// Real SQL Server, not a Postgres stand-in: neba-fwk (the legacy database every job under
// Neba.Api.Legacy reads from via Dapper) is SQL Server. Postgres and SQL Server disagree on how
// reserved keywords behave as unbracketed, alias-qualified column references (e.g. `t.End` - see
// NewTournamentSyncJob's `t.[End]`) and on native array parameter support, among other things; a
// Postgres-backed test can pass on SQL text that fails against the real database. This fixture
// exists so legacy-connection integration tests exercise the same engine production talks to.
public sealed class LegacySqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    // Production's raw SQL text references real, unprefixed table names (Tournaments, Squads, ...)
    // with no per-test namespacing available, so isolating one test's fake neba-fwk schema from
    // another's has to happen at the database level rather than the table-name level. Each test
    // gets its own throwaway database on the one shared container instead.
    [SuppressMessage("Design", "CA2000:Dispose objects before losing scope",
        Justification = "Ownership of the connection transfers to the returned LegacySqlServerDatabase, which disposes it.")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    public async Task<LegacySqlServerDatabase> CreateDatabaseAsync()
    {
        var databaseName = $"legacy_{Guid.NewGuid():N}";
        var masterConnectionString = _container.GetConnectionString();

        await using (var master = new SqlConnection(masterConnectionString))
        {
            await master.OpenAsync();
            await using var create = master.CreateCommand();
            create.CommandText = BuildCreateDatabaseCommand(databaseName);
            await create.ExecuteNonQueryAsync();
        }

        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = databaseName
        };

        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        return new LegacySqlServerDatabase(masterConnectionString, databaseName, connection);
    }

    [SuppressMessage("Security", "DAP241:Data values should not be interpolated into SQL string",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    internal static string BuildCreateDatabaseCommand(string databaseName)
        => $"CREATE DATABASE [{databaseName}]";
}

public sealed class LegacySqlServerDatabase(string masterConnectionString, string databaseName, SqlConnection connection) : IAsyncDisposable
{
    public SqlConnection Connection { get; } = connection;

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like DROP DATABASE can't parameterize identifiers.")]
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();

        await using var master = new SqlConnection(masterConnectionString);
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        drop.CommandText = BuildDropDatabaseCommand(databaseName);
        await drop.ExecuteNonQueryAsync();
    }

    [SuppressMessage("Security", "DAP241:Data values should not be interpolated into SQL string",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like DROP DATABASE can't parameterize identifiers.")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like DROP DATABASE can't parameterize identifiers.")]
    private static string BuildDropDatabaseCommand(string databaseName)
        => $"""
            ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{databaseName}];
            """;
}