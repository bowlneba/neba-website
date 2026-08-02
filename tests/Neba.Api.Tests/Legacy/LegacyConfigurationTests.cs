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
}
