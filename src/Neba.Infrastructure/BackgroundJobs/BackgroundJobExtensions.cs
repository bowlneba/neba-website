using Hangfire;
using Hangfire.PostgreSql;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Application.BackgroundJobs;

namespace Neba.Infrastructure.BackgroundJobs;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static

internal static class BackgroundJobsExtensions
{
    extension(IServiceCollection services)
    {
        public void AddBackgroundJobs(IConfiguration config)
        {
            services.AddOptions<HangfireSettings>()
                .Bind(config.GetSection("Hangfire"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<HangfireSettings>>().Value;

                return settings;
            });

            services.AddHangfireInfrastructure(config);

            string[] tags = ["infrastructure", "background-jobs"];

            services.AddHealthChecks()
                .AddHangfire(options => options.MinimumAvailableServers = 1,
                name: "Background Jobs",
                tags: tags);

            services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        }

        private void AddHangfireInfrastructure(IConfiguration config)
        {
            string connectionString = config.GetConnectionString("neba-website")
                ?? throw new InvalidOperationException("Connection string 'neba-website' not found.");

            services.AddHangfire((serviceProvider, options) =>
            {
                HangfireSettings settings = serviceProvider.GetRequiredService<HangfireSettings>();

                options
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseFilter(new AutomaticRetryAttribute { Attempts = settings.AutomaticRetryAttempts })
                    .UseFilter(new JobExpirationFilterAttribute(settings))
                    .UsePostgreSqlStorage(postgres => postgres
                        .UseNpgsqlConnection(connectionString),
                        new PostgreSqlStorageOptions
                        {
                            SchemaName = "hangfire",
                            PrepareSchemaIfNecessary = true,
                            EnableTransactionScopeEnlistment = true,
                            DeleteExpiredBatchSize = 1000,
                            QueuePollInterval = TimeSpan.FromSeconds(30),
                            JobExpirationCheckInterval = TimeSpan.FromHours(1),
                            CountersAggregateInterval = TimeSpan.FromMinutes(5),
                            TransactionSynchronisationTimeout = TimeSpan.FromSeconds(60)
                        });
            });

            services.AddHangfireServer((serviceProvider, options) =>
            {
                HangfireSettings settings = serviceProvider.GetRequiredService<HangfireSettings>();

                options.WorkerCount = settings.WorkerCount;
                options.ServerName = $"API - {Environment.MachineName}";
                options.Queues = ["default"];
            });
        }
    }

    extension(WebApplication app)
    {
        public void UseBackgroundJobsDashboard()
        {
            // Only register the dashboard if Hangfire has been configured
            // This allows test to remove Hangfire services without breaking middleware
            if (app.Services.GetService(typeof(JobStorage)) is not null)
            {
                app.UseHangfireDashboard("/background-jobs", new DashboardOptions
                {
                    Authorization = [new HangfireDashboardAuthorizationFilter()],
                    DashboardTitle = "Background Jobs - API",
                    StatsPollingInterval = 5000,
                    DisplayStorageConnectionString = false,
                    IsReadOnlyFunc = _ => false
                });
            }
        }
    }
}