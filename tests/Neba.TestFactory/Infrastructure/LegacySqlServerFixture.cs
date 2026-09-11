using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;

using Respawn;

using Testcontainers.MsSql;

namespace Neba.TestFactory.Infrastructure;

// Real SQL Server, not a Postgres stand-in: neba-fwk (the legacy database every job under
// Neba.Api.Legacy reads from via Dapper) is SQL Server. Postgres and SQL Server disagree on how
// reserved keywords behave as unbracketed, alias-qualified column references (e.g. `t.End` - see
// NewTournamentSyncJob's `t.[End]`) and on native array parameter support, among other things; a
// Postgres-backed test can pass on SQL text that fails against the real database. This fixture
// exists so legacy-connection integration tests exercise the same engine production talks to.
//
// One container for the whole run (mirroring AppDbContextFixture/PostgreSqlFixture), but unlike
// that pair there's no single shared schema every test can reuse - each Legacy*SyncJobTests class
// stands up its own ad hoc slice of neba-fwk's schema (different tables, and the same table name
// with different columns across classes, e.g. "Tournaments" in NewTournamentTests vs.
// GenerateSeasonStatsJobTests). So isolation happens one level down: one persistent database per
// test class (created once, schema included) rather than one shared database for every class.
// Respawn then resets that database's data between the class's individual test methods, the same
// role it plays for AppDbContextFixture's single Postgres database.
public sealed class LegacySqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly ConcurrentDictionary<string, Task<LegacySqlServerDatabase>> _databases = new();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        List<Exception>? exceptions = null;

        foreach (var databaseTask in _databases.Values)
        {
            try
            {
                var database = await databaseTask;
                await database.DisposeAsync();
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception ex)
        {
            (exceptions ??= []).Add(ex);
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(exceptions);
        }
    }

    // `name` scopes the persistent database to one test class (pass nameof(YourTestClass)) so
    // schema created by one class's ad hoc tables never collides with another's. The schema
    // delegate only runs once per name, on first use - subsequent calls return the same database
    // and connection, with data reset expected to happen via LegacySqlServerDatabase.ResetAsync().
    // Every Legacy*SyncJobTests class shares this one collection fixture but its test methods (and
    // the other test classes in the same collection) run sequentially by xunit's default collection
    // behavior, so GetOrAdd's factory racing itself for the same name is not a practical concern.
    public Task<LegacySqlServerDatabase> GetOrCreateDatabaseAsync(string name, Func<SqlConnection, Task> createSchemaAsync)
        => _databases.GetOrAdd(name, key => CreateDatabaseAsync(key, createSchemaAsync));

    [SuppressMessage("Design", "CA2000:Dispose objects before losing scope",
        Justification = "Ownership of the connection transfers to the returned LegacySqlServerDatabase, which disposes it.")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - name is a caller-supplied test class name (nameof(...)), not user input. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    private async Task<LegacySqlServerDatabase> CreateDatabaseAsync(string name, Func<SqlConnection, Task> createSchemaAsync)
    {
        var databaseName = $"legacy_{name}_{Guid.NewGuid():N}";
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

        try
        {
            await connection.OpenAsync();

            await createSchemaAsync(connection);

            var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer
            });

            return new LegacySqlServerDatabase(masterConnectionString, databaseName, connection, respawner);
        }
        catch
        {
            await connection.DisposeAsync();

            await using var master = new SqlConnection(masterConnectionString);
            await master.OpenAsync();
            await using var drop = master.CreateCommand();
            drop.CommandText = LegacySqlServerDatabase.BuildDropDatabaseCommand(databaseName);
            await drop.ExecuteNonQueryAsync();

            throw;
        }
    }

    [SuppressMessage("Security", "DAP241:Data values should not be interpolated into SQL string",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Not a data value - a generated (Guid-based, non-user-supplied) database identifier. DDL statements like CREATE DATABASE can't parameterize identifiers.")]
    internal static string BuildCreateDatabaseCommand(string databaseName)
        => $"CREATE DATABASE [{databaseName}]";
}

public sealed class LegacySqlServerDatabase(
    string masterConnectionString, string databaseName, SqlConnection connection, Respawner respawner) : IAsyncDisposable
{
    public SqlConnection Connection { get; } = connection;

    // Resets this database's data between test methods within the owning test class - the
    // schema itself is created once (see LegacySqlServerFixture.GetOrCreateDatabaseAsync) and
    // reused for the lifetime of the run, mirroring AppDbContextFixture.ResetAsync for Postgres.
    public Task ResetAsync() => respawner.ResetAsync(Connection);

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
    internal static string BuildDropDatabaseCommand(string databaseName)
        => $"""
            ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{databaseName}];
            """;
}