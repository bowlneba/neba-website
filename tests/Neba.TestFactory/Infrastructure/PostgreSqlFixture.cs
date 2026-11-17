using System.Diagnostics.CodeAnalysis;

using Npgsql;

using Testcontainers.PostgreSql;

namespace Neba.TestFactory.Infrastructure;

/// <summary>
/// A database on one Postgres container shared by the whole test process. Starting a container for every test
/// class (over a hundred of them) made a slow Docker start fail whole classes at random, so the container
/// starts once and each fixture creates, and later drops, its own database. Fixtures stay isolated from each
/// other and can run in parallel.
/// </summary>
[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
    Justification = "The database name is a GUID this class generates, not user input. DDL cannot parameterize identifiers.")]
internal sealed class PostgreSqlFixture : IAsyncLifetime
{
    private const int StartAttempts = 3;

    private static readonly SemaphoreSlim StartLock = new(1, 1);
    private static PostgreSqlContainer? _sharedContainer;

    private readonly string _databaseName = $"test_{Guid.NewGuid():N}";
    private string? _adminConnectionString;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var container = await GetSharedContainerAsync();
        _adminConnectionString = WithTimeouts(container.GetConnectionString());

        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = _databaseName }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_adminConnectionString is null)
        {
            return;
        }

        // Pooled connections to the database would block dropping it.
        await using (var pooled = new NpgsqlConnection(ConnectionString))
        {
            NpgsqlConnection.ClearPool(pooled);
        }

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The caller owns the connection and disposes it.")]
    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<PostgreSqlContainer> GetSharedContainerAsync()
    {
        await StartLock.WaitAsync();

        try
        {
            if (_sharedContainer is not null)
            {
                return _sharedContainer;
            }

            var attempt = 0;

            while (true)
            {
                attempt++;
                var container = new PostgreSqlBuilder("postgres:17").Build();

                try
                {
                    await container.StartAsync();
                    _sharedContainer = container;

                    return container;
                }
                catch (Exception) when (attempt < StartAttempts)
                {
                    await container.DisposeAsync();
                }
            }
        }
        finally
        {
            StartLock.Release();
        }
    }

    // A loaded Docker host can take longer than Npgsql's 15 second default to accept a connection.
    private static string WithTimeouts(string connectionString)
        => new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 60, CommandTimeout = 60 }.ConnectionString;
}
