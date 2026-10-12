using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            SchemasToInclude = [AppDbContext.DefaultSchema, AppDbContext.StagingSchema],
            TablesToIgnore = [new Table(AppDbContext.DefaultSchema, AppDbContext.MigrationsHistoryTableName)]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = await _postgres.OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    internal AppDbContext CreateDbContext(params IReadOnlyList<IInterceptor> extraInterceptors)
        => new(CreateDbContextOptions(extraInterceptors));

    /// <summary>
    /// Creates a context configured with the same <c>EnableRetryOnFailure</c> execution strategy as
    /// production (<c>DatabaseConfiguration</c>). The default test context omits it, so code that
    /// opens its own transaction without going through the strategy only fails against this one.
    /// </summary>
    internal AppDbContext CreateRetryingDbContext(params IReadOnlyList<IInterceptor> extraInterceptors)
        => new(CreateDbContextOptions(extraInterceptors, enableRetryOnFailure: true));

    internal DbContextOptions<AppDbContext> CreateDbContextOptions(params IReadOnlyList<IInterceptor> extraInterceptors)
        => CreateDbContextOptions(extraInterceptors, enableRetryOnFailure: false);

    private DbContextOptions<AppDbContext> CreateDbContextOptions(IReadOnlyList<IInterceptor> extraInterceptors, bool enableRetryOnFailure)
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            IncludeErrorDetail = true
        };

        var slowQueryInterceptor = new SlowQueryInterceptor(
            NullLogger<SlowQueryInterceptor>.Instance,
            new SlowQueryOptions());

        IInterceptor[] interceptors = [slowQueryInterceptor, .. extraInterceptors];

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString, options =>
            {
                options.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema);

                if (enableRetryOnFailure)
                {
                    options.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                }
            })
            .UseSnakeCaseNamingConvention()
            .UseExceptionProcessor()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .AddInterceptors(interceptors)
            .Options;
    }

    public async ValueTask DisposeAsync()
        => await _postgres.DisposeAsync();
}