using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;

using Neba.Infrastructure.Database;

using Npgsql;

using Respawn;
using Respawn.Graph;

using Testcontainers.PostgreSql;

namespace Neba.TestFactory.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    private Respawner _respawner = null!;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = CreateDbContextOptions();
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [AppDbContext.DefaultSchema],
            TablesToIgnore = [new Table(AppDbContext.DefaultSchema, AppDbContext.MigrationsHistoryTableName)]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    internal DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            IncludeErrorDetail = true
        };

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString, options =>
                options.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema))
            .UseSnakeCaseNamingConvention()
            .UseExceptionProcessor()
            .Options;
    }

    public async ValueTask DisposeAsync()
        => await _container.DisposeAsync();
}
