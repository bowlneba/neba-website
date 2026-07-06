using System.Reflection;

using Audit.AzureStorageTables.Providers;
using Audit.Hangfire;

using Hangfire;
using Hangfire.PostgreSql;

using Microsoft.Extensions.Options;

using Npgsql;

namespace Neba.Api.BackgroundJobs;

internal static class BackgroundJobsConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddBackgroundJobs(IConfiguration configuration)
        {
            services.AddOptions<HangfireSettings>()
                .Bind(configuration.GetSection(HangfireSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<HangfireSettings>>().Value;

                return settings;
            });

            services.AddHangfireInfrastructure(configuration);

            string[] tags = ["infrastructure", "background-jobs"];

            services.AddHealthChecks()
                .AddHangfire(options => options.MinimumAvailableServers = 1,
                name: "Background Jobs",
                tags: tags);

            services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

            services.Scan(scan => scan
                .FromAssemblies(typeof(BackgroundJobsConfiguration).Assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IBackgroundJobHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblies(typeof(BackgroundJobsConfiguration).Assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IDomainEventJob<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        private void AddHangfireInfrastructure(IConfiguration configuration)
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
                    .AddAuditJobExecutionFilter(config => config
                        .EventType("Job:{type}.{method}")
                        .ExcludeArguments()
                        // AuditJobExecutionFilterAttribute stashes its IAuditScope in PerformContext.Items
                        // under fixed string keys (not per-instance keys). If a job's method/type already
                        // carries its own [AuditJobExecutionFilter] attribute, that instance and this global
                        // one would both run OnPerforming/OnPerformed for the same job, clobbering each
                        // other's Items entry - whichever OnPerformed runs second finds its scope already
                        // removed and returns early, leaving its own audit event stuck unfinalized. Skip
                        // globally auditing any job that already opted in explicitly via its own attribute.
                        .AuditWhen(context =>
                            context.BackgroundJob.Job.Method.GetCustomAttribute<AuditJobExecutionFilterAttribute>() is null
                            && context.BackgroundJob.Job.Method.DeclaringType?.GetCustomAttribute<AuditJobExecutionFilterAttribute>() is null)
                        .DataProvider(new AzureTableDataProvider(azureConfig => azureConfig
                            .ConnectionString(configuration.GetConnectionString("tables"))
                            .TableName(_ => "JobAuditEvents")
                            .EntityBuilder(entity => entity
                                .PartitionKey(ev => ev.EventType ?? "unknown")
                                .RowKey(_ => Ulid.NewUlid().ToString())))))
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