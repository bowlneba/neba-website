using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Neba.ServiceDefaults.Telemetry;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Telemetry;

[UnitTest]
[Component("ServiceDefaults.Telemetry")]
public sealed class LogEnrichmentConfigurationTests
{
    [Fact(DisplayName = "ConfigureLogEnrichment should set application metadata from the host environment")]
    public async Task ConfigureLogEnrichment_ShouldSetApplicationMetadata()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "Neba.Test",
            EnvironmentName = "Staging"
        });

        // Act
        builder.ConfigureLogEnrichment();

        // Assert
        await using var provider = builder.Services.BuildServiceProvider();
        var metadata = provider.GetRequiredService<IOptions<ApplicationMetadata>>().Value;
        metadata.ApplicationName.ShouldBe("Neba.Test");
        metadata.EnvironmentName.ShouldBe("Staging");
        metadata.BuildVersion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "ConfigureLogEnrichment should add application and process properties to log records")]
    public async Task ConfigureLogEnrichment_ShouldEnrichLogRecords()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "Neba.Test",
            EnvironmentName = "Staging"
        });
        builder.Logging.ClearProviders().AddFakeLogging();
        builder.ConfigureLogEnrichment();

        await using var provider = builder.Services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<LogEnrichmentConfigurationTests>>();
        var collector = provider.GetFakeLogCollector();

        // Act
        logger.LogHello();

        // Assert
        var record = collector.GetSnapshot().ShouldHaveSingleItem();
        record.GetStructuredStateValue(ApplicationEnricherTags.ApplicationName).ShouldBe("Neba.Test");
        record.GetStructuredStateValue(ApplicationEnricherTags.EnvironmentName).ShouldBe("Staging");
        record.GetStructuredStateValue(ApplicationEnricherTags.BuildVersion).ShouldNotBeNullOrWhiteSpace();
        record.GetStructuredStateValue(ProcessEnricherTagNames.ProcessId).ShouldBe(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}

internal static partial class LogEnrichmentConfigurationTestsLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "hello")]
    public static partial void LogHello(this ILogger<LogEnrichmentConfigurationTests> logger);
}
