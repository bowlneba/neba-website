using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Npgsql;

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
                // Ensure connection string has SSL for non-local PostgreSQL hosts.
                // Local development hosts are allowed to omit SSL.
                if (!(HasExplicitSslMode(settings.ConnectionString)
                    || !IsLocalConnectionString(settings.ConnectionString)))
                {
                    settings.ConnectionString += ";Ssl Mode=Require";
                }
            });
            builder.AddAzureNpgsqlDbContext<AppDbContext>(connectionStringName);

            return builder;
        }

        private static bool HasExplicitSslMode(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                return connectionStringBuilder.SslMode is not SslMode.Disable and not SslMode.Prefer;
            }
            catch (ArgumentException)
            {
                return connectionString.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsLocalConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                return IsLocalHost(connectionStringBuilder.Host);
            }
            catch (ArgumentException)
            {
                return connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains("::1", StringComparison.OrdinalIgnoreCase)
                    || connectionString.Contains(".local", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsLocalHost(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            var hosts = host.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (hosts.Length != 0) 
                && hosts.All(static value =>
                    value.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("::1", StringComparison.OrdinalIgnoreCase)
                    || value.EndsWith(".local", StringComparison.OrdinalIgnoreCase));
        }
    }
}