using System.Data;

using FluentValidation;

using Hangfire;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.Seasons.Complete;
using Neba.Api.Legacy.Sync;
using Neba.Api.Legacy.Tournaments;
using Neba.Api.Legacy.Tournaments.Complete;
using Neba.Api.Legacy.Tournaments.Stats;

namespace Neba.Api.Legacy;

internal static class LegacyConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddLegacy()
        {
            builder.Services
                .AddOptions<LegacySettings>()
                .Bind(builder.Configuration.GetSection("Legacy"))
                .ValidateOnStart();

            builder.Services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(sp.GetRequiredService<IOptions<LegacySettings>>().Value.ConnectionString));

            // Locally the Legacy connection string is typically unset (no access to the Software's
            // database outside production) - only register the check when one is actually configured.
            if (HasConnectionString(builder.Configuration))
            {
                builder.Services.AddHealthChecks()
                    .AddSqlServer(
                        sp => sp.GetRequiredService<IOptions<LegacySettings>>().Value.ConnectionString,
                        name: "Software (nebamgmt-v3)",
                        tags: ["infrastructure", "legacy"]);
            }

            builder.Services.AddScoped<IValidator<NewBowlerRequest>, NewBowlerRequestValidator>();
            builder.Services.AddScoped<IValidator<UpdateBowlerRequest>, UpdateBowlerRequestValidator>();
            builder.Services.AddScoped<IValidator<NewTournamentRequest>, NewTournamentRequestValidator>();
            builder.Services.AddScoped<IValidator<SyncSquadScoresRequest>, SyncSquadScoresRequestValidator>();
            builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();
            builder.Services.AddScoped<IValidator<UpdateTournamentStatsRequest>, UpdateTournamentStatsRequestValidator>();
            builder.Services.AddScoped<IValidator<CompleteSeasonRequest>, CompleteSeasonRequestValidator>();

            return builder;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        public void MapLegacyGroup()
        {
            var group = app.MapGroup("/legacy")
                .AddEndpointFilter<LegacyApiKeyFilter>();

            group.MapLegacyEndpoints();
        }
    }

    extension(WebApplication app)
    {
        public void UseLegacyWeeklySync()
        {
            using var scope = app.Services.CreateScope();
            var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            // Locally the Legacy connection string is usually unset (no access to the Software's
            // database); a job that can only fail every Monday is noise, so register it only when
            // the same condition AddLegacy() uses for the health check holds. Remove any job an
            // earlier config registered, or it keeps firing against the missing connection.
            if (!HasConnectionString(app.Configuration))
            {
                recurringJobs.RemoveIfExists(WeeklyLegacySync.RecurringJobId);
                return;
            }

            recurringJobs.AddOrUpdate<WeeklyLegacySyncJob>(
                WeeklyLegacySync.RecurringJobId,
                job => job.RunAsync(),
                WeeklyLegacySync.Cron,
                new RecurringJobOptions { TimeZone = LegacyTimeZones.Eastern });
        }
    }

    private static bool HasConnectionString(IConfiguration configuration)
        => !string.IsNullOrWhiteSpace(configuration.GetSection("Legacy")["ConnectionString"]);
}
