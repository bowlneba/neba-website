using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Neba.Api.Database;

internal sealed class AppDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var basePath = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.GetFullPath(Path.Join(basePath, "..", "Neba.Api"));
        var environmentSettingsFileName = $"appsettings.{environmentName}.json";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile(Path.Join(apiProjectPath, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Join(apiProjectPath, environmentSettingsFileName), optional: true)
            .AddUserSecrets<AppDbContextDesignTimeFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("bowlneba");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:bowlneba for EF design-time operations. " +
                "Set it via local user-secrets on Neba.Api or set environment variable ConnectionStrings__bowlneba.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                AppDbContext.MigrationsHistoryTableName,
                AppDbContext.DefaultSchema))
                .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}