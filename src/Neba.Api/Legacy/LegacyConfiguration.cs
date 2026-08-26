using System.Data;

using FluentValidation;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy.Bowlers;
using Neba.Api.Legacy.Seasons.Complete;
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
}