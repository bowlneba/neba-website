using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Neba.Infrastructure.Database;

#pragma warning disable S1144 // Unused private types or members should be removed
internal static class DatabaseConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDatabase()
        {
            const string connectionStringName = "bowlneba";

            builder.AddAzureNpgsqlDataSource(connectionStringName, settings =>
            {
                // Ensure connection string has SSL for Azure PostgreSQL (not needed for local Docker)
                var cs = settings.ConnectionString;
                if (cs?.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase) == false
                    && !cs.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    && !cs.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    settings.ConnectionString += ";Ssl Mode=Require";
                }
            });
            builder.AddAzureNpgsqlDbContext<AppDbContext>(connectionStringName);

            return builder;
        }
    }
}