using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Neba.Application.Awards;
using Neba.Application.BowlingCenters;
using Neba.Application.HallOfFame;
using Neba.Application.Sponsors;
using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;
using Neba.Infrastructure.Database.Queries;

using Npgsql;

namespace Neba.Infrastructure.Database;

#pragma warning disable S1144 // Unused private types or members should be removed
internal static class DatabaseConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDatabase()
        {
            const string connectionStringName = "bowlneba";

            builder.AddAzureNpgsqlDataSource(connectionStringName, settings =>
            {
                // Ensure connection string has SSL for non-local PostgreSQL hosts.
                // Local development hosts are allowed to omit SSL.
                if (!(HasExplicitSslMode(settings.ConnectionString)
                    || IsLocalConnectionString(settings.ConnectionString)))
                {
                    settings.ConnectionString += ";Ssl Mode=Require";
                }
            });
            // AddDbContextPool with (IServiceProvider, DbContextOptionsBuilder) overload is required
            // to resolve DI services (the interceptor) — AddAzureNpgsqlDbContext's configureDbContextOptions
            // only provides DbContextOptionsBuilder with no IServiceProvider access.
            builder.Services.AddDbContextPool<AppDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                var slowQuery = sp.GetRequiredService<SlowQueryInterceptor>();
                var queryTag = sp.GetRequiredService<QueryTagEnrichmentInterceptor>();
                var domainEvents = sp.GetRequiredService<DomainEventDispatcherInterceptor>();

                options
                    .UseNpgsql(dataSource, npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema))
                    .UseExceptionProcessor()
                    .UseSnakeCaseNamingConvention()
                    .EnableDetailedErrors()
                    .AddInterceptors(slowQuery, queryTag, domainEvents);

#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });

            builder.Services.Configure<SlowQueryOptions>(builder.Configuration.GetSection(SlowQueryOptions.SectionName));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SlowQueryOptions>>().Value);

            builder.Services.AddSingleton<SlowQueryInterceptor>();
            builder.Services.AddSingleton<QueryTagEnrichmentInterceptor>();
            builder.Services.AddSingleton<DomainEventDispatcherInterceptor>();

            builder.Services.AddQueries();

            return builder;
        }

        private static bool HasExplicitSslMode(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                return connectionStringBuilder.SslMode is not SslMode.Disable and not SslMode.Prefer;
            }
            catch (ArgumentException)
            {
                return connectionString.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsLocalConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                return IsLocalHost(connectionStringBuilder.Host);
            }
            catch (ArgumentException)
            {
                return connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains("::1", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains(".local", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsLocalHost(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            var hosts = host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (hosts.Length != 0)
                && hosts.All(static value =>
                    value.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("::1", StringComparison.OrdinalIgnoreCase)
                    || value.EndsWith(".local", StringComparison.OrdinalIgnoreCase));
        }
    }

    extension(IServiceCollection services)
    {
        public void AddQueries()
        {
            services.AddScoped<IBowlingCenterQueries, BowlingCenterQueries>();
            services.AddScoped<IHallOfFameQueries, HallOfFameQueries>();
            services.AddScoped<IAwardQueries, AwardQueries>();
            services.AddScoped<ISponsorQueries, SponsorQueries>();
        }
    }
}