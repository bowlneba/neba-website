using System.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

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