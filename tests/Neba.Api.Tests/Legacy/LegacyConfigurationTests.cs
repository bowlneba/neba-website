using System.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Development });

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

    [Fact(DisplayName = "AddLegacy should register LegacyDecryptor as a singleton")]
    public void AddLegacy_ShouldRegisterLegacyDecryptor_AsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddLegacy();

        // Assert
        builder.Services.ShouldContain(sd =>
            sd.ServiceType == typeof(LegacyDecryptor) &&
            sd.Lifetime == ServiceLifetime.Singleton);

        using var serviceProvider = builder.Services.BuildServiceProvider();
        serviceProvider.GetRequiredService<LegacyDecryptor>().ShouldNotBeNull();
    }

    [Fact(DisplayName = "AddLegacy should bind LegacySettings from the Legacy configuration section")]
    public void AddLegacy_ShouldBindLegacySettings_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Legacy:ApiKey"] = "config-api-key",
            ["Legacy:ConnectionString"] = "data source=test;initial catalog=neba",
            ["Legacy:SsnPassphrase"] = "config-passphrase"
        });

        // Act
        builder.AddLegacy();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<LegacySettings>>().Value;
        settings.ApiKey.ShouldBe("config-api-key");
        settings.ConnectionString.ShouldBe("data source=test;initial catalog=neba");
        settings.SsnPassphrase.ShouldBe("config-passphrase");
    }

    [Fact(DisplayName = "AddLegacy should use empty defaults when the Legacy section is absent")]
    public void AddLegacy_ShouldUseEmptyDefaults_WhenSectionIsAbsent()
    {
        // Arrange: Development, where no Legacy setting is required.
        var builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.Sources.Clear();

        // Act
        builder.AddLegacy();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<LegacySettings>>().Value;
        settings.ApiKey.ShouldBe(string.Empty);
        settings.ConnectionString.ShouldBe(string.Empty);
        settings.SsnPassphrase.ShouldBe(string.Empty);
    }

    [Theory(DisplayName = "Startup should fail outside Development when the legacy passphrase is missing")]
    [InlineData("Production", "", true, TestDisplayName = "Production without it")]
    [InlineData("Staging", " ", true, TestDisplayName = "Staging with a blank one")]
    [InlineData("Production", "a-passphrase", false, TestDisplayName = "Production with it")]
    [InlineData("Development", "", false, TestDisplayName = "Development without it")]
    public void AddLegacy_ShouldRequireSsnPassphrase_OutsideDevelopment(string environment, string passphrase, bool fails)
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Legacy:SsnPassphrase"] = passphrase });
        builder.AddLegacy();
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Act: the check the host runs at startup.
        var exception = Record.Exception(() => serviceProvider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        if (fails)
        {
            exception.ShouldBeOfType<OptionsValidationException>().Message.ShouldContain("Legacy:SsnPassphrase");
        }
        else
        {
            exception.ShouldBeNull();
        }
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