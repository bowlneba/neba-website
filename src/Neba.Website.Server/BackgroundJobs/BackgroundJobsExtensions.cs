using Hangfire;
using Hangfire.PostgreSql;

namespace Neba.Website.Server.BackgroundJobs;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static

internal static class BackgroundJobsExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddBackgroundJobs(IConfiguration config)
        {
            services.AddHangfire(options => options
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(postgres => postgres
                    .UseNpgsqlConnection(config.GetConnectionString("neba-website")
                        ?? throw new InvalidOperationException("Hangfire connection string is not configured.")), new PostgreSqlStorageOptions
                        {
                            SchemaName = "hangfire"
                        }));

            return services;
        }
    }

    extension(WebApplication app)
    {
        internal WebApplication UseBackgroundJobsDashboard()
        {
            app.UseHangfireDashboard("/admin/background-jobs", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
                DashboardTitle = "Background Jobs - Admin",
                StatsPollingInterval = 5000,
                DisplayStorageConnectionString = false,
                IsReadOnlyFunc = _ => false
            });

            return app;
        }
    }
}