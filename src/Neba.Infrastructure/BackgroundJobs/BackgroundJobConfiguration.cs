using Hangfire;
using Hangfire.PostgreSql;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Application;
using Neba.Application.BackgroundJobs;

using Npgsql;

namespace Neba.Infrastructure.BackgroundJobs;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static

internal static class BackgroundJobsConfiguration
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

            services.AddHangfireInfrastructure();

            string[] tags = ["infrastructure", "background-jobs"];

            services.AddHealthChecks()
                .AddHangfire(options => options.MinimumAvailableServers = 1,
                name: "Background Jobs",
                tags: tags);

            services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

            services.Scan(scan => scan
                .FromAssemblies(typeof(IApplicationAssemblyMarker).Assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IBackgroundJobHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        private void AddHangfireInfrastructure()
        {
            services.AddHangfire((serviceProvider, options) =>
            {
                HangfireSettings settings = serviceProvider.GetRequiredService<HangfireSettings>();
                var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();

                options
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseFilter(new AutomaticRetryAttribute { Attempts = settings.AutomaticRetryAttempts })
                    .UseFilter(new HangfireJobExpirationFilterAttribute(settings))
                    .UsePostgreSqlStorage(postgres => postgres
                        .UseConnectionFactory(new HangfireConnectionFactory(dataSource)),
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
                    Authorization = [new HangfireApiDashboardAuthorizationFilter()],
                    DashboardTitle = "Background Jobs - API",
                    StatsPollingInterval = 5000,
                    DisplayStorageConnectionString = false,
                    IsReadOnlyFunc = _ => false
                });
            }
        }
    }
}