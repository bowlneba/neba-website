using Hangfire.PostgreSql;

using Npgsql;

namespace Neba.Website.Server.BackgroundJobs;

internal sealed class HangfireConnectionFactory(NpgsqlDataSource dataSource)
    : IConnectionFactory
{
    public NpgsqlConnection GetOrCreateConnection()
        => dataSource.CreateConnection();
}
