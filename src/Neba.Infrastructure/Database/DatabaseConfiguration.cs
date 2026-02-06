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
            const string connectionStringName = "bowlneba-db";

            builder.AddNpgsqlDbContext<AppDbContext>(connectionStringName);

            return builder;
        }
    }
}