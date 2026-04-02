using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;

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

        await using var context = new AppDbContext(CreateDbContextOptions());
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

    internal AppDbContext CreateDbContext() => new(CreateDbContextOptions());

    internal DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            IncludeErrorDetail = true
        };

        var slowQueryInterceptor = new SlowQueryInterceptor(
            NullLogger<SlowQueryInterceptor>.Instance,
            new SlowQueryOptions());

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString, options =>
                options.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema))
            .UseSnakeCaseNamingConvention()
            .UseExceptionProcessor()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .AddInterceptors(slowQueryInterceptor)
            .Options;
    }

    public async ValueTask DisposeAsync()
        => await _container.DisposeAsync();
}