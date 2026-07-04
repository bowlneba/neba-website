using Audit.Core;
using Audit.EntityFramework;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal static class AuditingConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAuditing()
        {
            builder.AddAzureTableServiceClient("tables");

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
            builder.Services.AddSingleton<EfAuditEnrichmentAction>();

            Audit.Core.Configuration.Setup()
                .UseAzureTableStorage(config => config
                    .ConnectionString(builder.Configuration.GetConnectionString("tables"))
                    .TableName(_ => "EFAuditEvents")
                    .EntityBuilder(entity => entity
                        .PartitionKey(ev => ev.EventType ?? "unknown")
                        .RowKey(_ => Ulid.NewUlid().ToString())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            using (var serviceProvider = builder.Services.BuildServiceProvider())
            {
                var enrichmentAction = serviceProvider.GetRequiredService<EfAuditEnrichmentAction>();
                Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, enrichmentAction.OnEventSaving);

                var providerLogger = serviceProvider.GetRequiredService<ILogger<ResilientAuditDataProvider>>();
                Audit.Core.Configuration.DataProvider = new ResilientAuditDataProvider(Audit.Core.Configuration.DataProvider, providerLogger);
            }

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<AppDbContext>(auditConfig => auditConfig
                    .AuditEventType("EF:{context}")
                    .IncludeEntityObjects(false)) // scrubbed snapshots are attached manually below
                .UseOptIn()
                .Include<Bowler>()
                .Include<Season>()
                .Include<Tournament>()
                .Include<HallOfFameInduction>()
                .Include<HighAverageAward>()
                .Include<HighBlockAward>()
                .Include<BowlerOfTheYearAward>()
                .Include<BowlingCenter>()
                .Include<Sponsor>();

            return builder;
        }
    }
}