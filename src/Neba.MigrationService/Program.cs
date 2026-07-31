using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Api.Database;

const string connectionStringName = "bowlneba";

if (args is not [var target] || (target != "app" && target != "security"))
{
    throw new InvalidOperationException("Specify which DbContext to migrate as the single argument: 'app' or 'security'.");
}

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

if (target == "app")
{
    builder.AddAzureNpgsqlDbContext<AppDbContext>(connectionStringName, configureDbContextOptions: options =>
        options.UseNpgsql(npgsql => npgsql
                .MigrationsHistoryTable(AppDbContext.MigrationsHistoryTableName, AppDbContext.DefaultSchema))
            .UseSnakeCaseNamingConvention());
}
else
{
    builder.AddAzureNpgsqlDbContext<SecurityDbContext>(connectionStringName, configureDbContextOptions: options =>
        options.UseNpgsql(npgsql => npgsql
                .MigrationsHistoryTable(SecurityDbContext.MigrationsHistoryTableName, SecurityDbContext.Schema))
            .UseSnakeCaseNamingConvention());
}

using var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();

if (target == "app")
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
else
{
    await scope.ServiceProvider.GetRequiredService<SecurityDbContext>().Database.MigrateAsync();
}
