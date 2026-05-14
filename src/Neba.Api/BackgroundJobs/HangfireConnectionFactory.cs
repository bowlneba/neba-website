using Hangfire.PostgreSql;

using Npgsql;

namespace Neba.Api.BackgroundJobs;

internal sealed class HangfireConnectionFactory(NpgsqlDataSource dataSource)
    : IConnectionFactory
{
    public NpgsqlConnection GetOrCreateConnection()
        => dataSource.CreateConnection();
}