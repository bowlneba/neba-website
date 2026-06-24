using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Security.Domain;

using Respawn;
using Respawn.Graph;

namespace Neba.TestFactory.Infrastructure;

public sealed class SecurityDbContextFixture : IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres = new();
    private Respawner _respawner = null!;
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.InitializeAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SecurityDbContext>(options =>
            options
                .UseNpgsql(_postgres.ConnectionString, npgsql => npgsql
                    .MigrationsHistoryTable(
                        SecurityDbContext.MigrationsHistoryTableName,
                        SecurityDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SecurityDbContext>()
            .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();

        await using var context = _serviceProvider.GetRequiredService<SecurityDbContext>();
        await context.Database.MigrateAsync();

        await using var connection = await _postgres.OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [SecurityDbContext.Schema],
            TablesToIgnore = [new Table(SecurityDbContext.Schema, SecurityDbContext.MigrationsHistoryTableName)]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = await _postgres.OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    public IServiceScope CreateScope() => _serviceProvider.CreateScope();

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
