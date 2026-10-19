using System.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[IntegrationTest]
[Component("Legacy")]
public sealed class LegacyConfigurationTests
{
    [Fact(DisplayName = "AddLegacy should register IDbConnection as a scoped SqlConnection")]
    public void AddLegacy_ShouldRegisterIDbConnection_AsScopedSqlConnection()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddLegacy();

        // Assert
        builder.Services.ShouldContain(sd =>
            sd.ServiceType == typeof(IDbConnection) &&
            sd.Lifetime == ServiceLifetime.Scoped);

        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDbConnection>().ShouldBeOfType<SqlConnection>();
    }

    [Fact(DisplayName = "AddLegacy should bind LegacySettings from the Legacy configuration section")]
    public void AddLegacy_ShouldBindLegacySettings_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Legacy:ApiKey"] = "config-api-key",
            ["Legacy:ConnectionString"] = "data source=test;initial catalog=neba"
        });

        // Act
        builder.AddLegacy();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<LegacySettings>>().Value;
        settings.ApiKey.ShouldBe("config-api-key");
        settings.ConnectionString.ShouldBe("data source=test;initial catalog=neba");
    }

    [Fact(DisplayName = "AddLegacy should use empty defaults when the Legacy section is absent")]
    public void AddLegacy_ShouldUseEmptyDefaults_WhenSectionIsAbsent()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddLegacy();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<LegacySettings>>().Value;
        settings.ApiKey.ShouldBe(string.Empty);
        settings.ConnectionString.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "UseLegacyWeeklySync should register the weekly job in Eastern time when a connection string is set")]
    public void UseLegacyWeeklySync_ShouldRegisterRecurringJob_WhenConnectionStringIsSet()
    {
        // Arrange
        Hangfire.Common.Job? registeredJob = null;
        string? registeredId = null;
        string? registeredCron = null;
        Hangfire.RecurringJobOptions? registeredOptions = null;

        var manager = new Mock<Hangfire.IRecurringJobManager>(MockBehavior.Strict);
        manager
            .Setup(m => m.AddOrUpdate(It.IsAny<string>(), It.IsAny<Hangfire.Common.Job>(), It.IsAny<string>(), It.IsAny<Hangfire.RecurringJobOptions>()))
            .Callback<string, Hangfire.Common.Job, string, Hangfire.RecurringJobOptions>((id, job, cron, options) =>
            {
                registeredId = id;
                registeredJob = job;
                registeredCron = cron;
                registeredOptions = options;
            });

        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Legacy:ConnectionString"] = "data source=test;initial catalog=neba"
        });
        builder.Services.AddSingleton(manager.Object);
        using var app = builder.Build();

        // Act
        app.UseLegacyWeeklySync();

        // Assert
        registeredId.ShouldBe(Neba.Api.Legacy.Sync.WeeklyLegacySync.RecurringJobId);
        registeredCron.ShouldBe(Neba.Api.Legacy.Sync.WeeklyLegacySync.Cron);
        registeredJob.ShouldNotBeNull();
        registeredJob.Type.ShouldBe(typeof(Neba.Api.Legacy.Sync.WeeklyLegacySyncJob));
        registeredOptions.ShouldNotBeNull();
        registeredOptions.TimeZone.ShouldBe(LegacyTimeZones.Eastern);
    }

    [Fact(DisplayName = "UseLegacyWeeklySync should remove the weekly job when the connection string is blank")]
    public void UseLegacyWeeklySync_ShouldRemoveRecurringJob_WhenConnectionStringIsBlank()
    {
        // Arrange
        var manager = new Mock<Hangfire.IRecurringJobManager>(MockBehavior.Strict);
        manager.Setup(m => m.RemoveIfExists(Neba.Api.Legacy.Sync.WeeklyLegacySync.RecurringJobId));
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(manager.Object);
        using var app = builder.Build();

        // Act
        app.UseLegacyWeeklySync();

        // Assert — the strict mock throws on AddOrUpdate, so reaching here means nothing registered
        manager.Invocations.ShouldHaveSingleItem().Method.Name.ShouldBe(nameof(Hangfire.IRecurringJobManager.RemoveIfExists));
    }
}