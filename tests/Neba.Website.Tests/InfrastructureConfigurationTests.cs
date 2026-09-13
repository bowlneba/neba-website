using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Contracts.Compliance;
using Neba.TestFactory.Attributes;
using Neba.Website.Server;

namespace Neba.Website.Tests;

[UnitTest]
[Component("Website.InfrastructureConfiguration")]
public sealed class InfrastructureConfigurationTests
{
#nullable disable
    [Fact(DisplayName = "AddInfrastructure should throw when the builder is null")]
    public void AddInfrastructure_ShouldThrow_WhenBuilderIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddInfrastructure());
    }
#nullable enable

    [Fact(DisplayName = "AddInfrastructure should return the same builder when no Key Vault connection string is configured")]
    public void AddInfrastructure_ShouldReturnSameBuilder_WhenNoKeyVaultConnectionStringIsConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:keyvault"] = null;

        // Act
        var result = builder.AddInfrastructure();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact(DisplayName = "AddInfrastructure should wire up [LoggerMessage] redaction so a [PersonalData] parameter is masked")]
    public async Task AddInfrastructure_ShouldWireUpRedaction_SoPersonalDataParameterIsMasked()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:keyvault"] = null;
        builder.Configuration["ConnectionStrings:blob"] = null;
        builder.Logging.ClearProviders().AddFakeLogging();

        builder.AddInfrastructure();

        await using var provider = builder.Services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<InfrastructureConfigurationTests>>();
        var collector = provider.GetFakeLogCollector();

        // Act
        logger.LogSampleWithPersonalData("log-target@example.com");

        // Assert
        var logs = collector.GetSnapshot();
        logs.Count.ShouldBe(1);
        logs[0].Message.ShouldNotContain("log-target@example.com");
        logs[0].Message.ShouldMatch(@"^Sample: l\*+$");
    }
}

internal static partial class InfrastructureConfigurationTestsLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Sample: {Value}")]
    public static partial void LogSampleWithPersonalData(this ILogger logger, [PersonalData] string value);
}