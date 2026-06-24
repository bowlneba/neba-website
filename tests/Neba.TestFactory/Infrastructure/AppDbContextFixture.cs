using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;

using Npgsql;

using Respawn;
using Respawn.Graph;

namespace Neba.TestFactory.Infrastructure;

public sealed class AppDbContextFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres = new();
    private Respawner _respawner = null!;

    public string ConnectionString => _postgres.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _postgres.InitializeAsync();

        await using var context = new AppDbContext(CreateDbContextOptions());
        await context.Database.MigrateAsync();

        await using var connection = await _postgres.OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [AppDbContext.DefaultSchema],
            TablesToIgnore = [new Table(AppDbContext.DefaultSchema, AppDbContext.MigrationsHistoryTableName)]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = await _postgres.OpenConnectionAsync();
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
        => await _postgres.DisposeAsync();
}
