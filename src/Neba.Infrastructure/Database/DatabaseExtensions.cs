using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace Neba.Infrastructure.Database;

#pragma warning disable S1144 // Unused private types or members should be removed
internal static class DatabaseExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabase(string connectionString)
        {
            services.AddDatabaseTelemetry();
            services.AddDatabaseHealthChecks(connectionString);

            return services;
        }

        private void AddDatabaseTelemetry()
            => services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddNpgsql())
                .WithMetrics(metrics => metrics.AddNpgsqlInstrumentation());

        private void AddDatabaseHealthChecks(string connectionString)
        {
            string[] tags = ["database"];

            services.AddHealthChecks()
                .AddNpgSql(
                    connectionString: connectionString,
                    name: "database",
                    tags: tags);
        }
    }
}