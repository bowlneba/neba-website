using Audit.AzureStorageTables.ConfigurationApi;
using Audit.AzureStorageTables.Providers;
using Audit.Core;
using Audit.EntityFramework;
using Audit.WebApi;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;
using Neba.Api.Security.Domain;

namespace Neba.Api.Auditing;

internal static class AuditingConfiguration
{
    private static readonly string[] ExcludedPathPrefixes =
    [
        "/health",
        "/scalar",
        "/background-jobs",
        "/debug"
    ];

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAuditing()
        {
            builder.AddAzureTableServiceClient("tables");

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
            builder.Services.AddSingleton<AuditEnrichmentAction>();
            builder.Services.AddSingleton<ApiAuditPayloadScrubbingAction>();

            var defaultProvider = new AzureTableDataProvider(config => config
                .ConnectionString(builder.Configuration.GetConnectionString("tables"))
                .TableName(_ => "EFAuditEvents")
                // EntityMapper (not EntityBuilder) is required to retain the event payload:
                // AuditEventTableEntity serializes the full event to JSON in an "AuditEvent"
                // column. EntityBuilder without a .Columns(...) call produces a TableEntity
                // with no properties at all, silently discarding the payload.
                .EntityMapper(ev => new AuditEventTableEntity(ev.EventType ?? "unknown", Ulid.NewUlid().ToString(), ev)));

            var securityProvider = new AzureTableDataProvider(config => config
                .ConnectionString(builder.Configuration.GetConnectionString("tables"))
                .TableName(_ => "SecurityAuditEvents")
                .EntityMapper(ev => new AuditEventTableEntity(ev.EventType ?? "unknown", Ulid.NewUlid().ToString(), ev)));

            Audit.Core.Configuration.Setup()
                .Use(new SecurityAuditDataProviderRouter(securityProvider, defaultProvider))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<AppDbContext>(auditConfig => auditConfig
                    .AuditEventType("EF:{context}")
                    // Must be true so AuditEnrichmentAction.Enrich can read entry.Entity to build
                    // scrubbed ColumnValues; it clears entry.Entity afterward so the raw,
                    // unscrubbed entity is never itself serialized into the audit event.
                    .IncludeEntityObjects(true))
                .UseOptIn()
                .Include<Bowler>()
                .Include<Season>()
                .Include<Tournament>()
                .Include<HallOfFameInduction>()
                .Include<HighAverageAward>()
                .Include<HighBlockAward>()
                .Include<BowlerOfTheYearAward>()
                .Include<BowlingCenter>()
                .Include<Sponsor>()
                .Include<Article>();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<SecurityDbContext>(auditConfig => auditConfig
                    .AuditEventType("EF:{context}")
                    .IncludeEntityObjects(true))
                .UseOptIn()
                .Include<ApplicationUser>()
                .Include<IdentityUserRole<Ulid>>();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseApiAuditMiddleware()
        {
            var enrichmentAction = app.Services.GetRequiredService<AuditEnrichmentAction>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, enrichmentAction.OnEventSaving);

            var apiScrubbingAction = app.Services.GetRequiredService<ApiAuditPayloadScrubbingAction>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, apiScrubbingAction.OnEventSaving);

            var providerLogger = app.Services.GetRequiredService<ILogger<ResilientAuditDataProvider>>();
            Audit.Core.Configuration.DataProvider = new ResilientAuditDataProvider(Audit.Core.Configuration.DataProvider, providerLogger);

            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next(context);
            });

            app.UseAuditMiddleware(config => config
                .WithEventType(context => $"Api:{context.Request.Method}:{context.Request.Path}")
                .FilterByRequest(request => !HttpMethods.IsGet(request.Method)
                    && !ExcludedPathPrefixes.Any(prefix => request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)))
                .IncludeRequestBody(true)
                .IncludeResponseBody(true));

            return app;
        }
    }
}