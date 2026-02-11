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
            var connectionString = config.GetConnectionString("bowlneba");
            if (string.IsNullOrEmpty(connectionString))
            {
                return services;
            }

            services.AddHangfire(options => options
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(postgres => postgres
                    .UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = true
                    }));

            return services;
        }
    }

    extension(WebApplication app)
    {
        internal WebApplication UseBackgroundJobsDashboard()
        {
            if (app.Services.GetService<JobStorage>() is null)
            {
                return app;
            }

            app.UseHangfireDashboard("/admin/background-jobs", new DashboardOptions
            {
                Authorization = [new HangfireUiDashboardAuthorizationFilter()],
                DashboardTitle = "Background Jobs - Admin",
                StatsPollingInterval = 5000,
                DisplayStorageConnectionString = false,
                IsReadOnlyFunc = _ => false
            });

            return app;
        }
    }
}