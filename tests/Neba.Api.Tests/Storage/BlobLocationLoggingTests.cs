using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Contracts.Compliance;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Storage;

[UnitTest]
[Component("Storage")]
public sealed class BlobLocationLoggingTests
{
    [Fact(DisplayName = "Blob log message should emit container and path as structured tags")]
    public async Task LogFileNotFound_ShouldEmitLocationTags()
    {
        // Arrange
        await using var provider = BuildProvider(out var collector);
        var logger = provider.GetRequiredService<ILogger<AzureBlobStorageService>>();

        // Act
        var location = new BlobLocation("documents", "rules/2026.html");
        logger.LogFileNotFound(location);

        // Assert
        var record = collector.GetSnapshot().ShouldHaveSingleItem();
        record.Message.ShouldBe("File not found: documents/rules/2026.html");
        record.GetStructuredStateValue("location.Container").ShouldBe("documents");
        record.GetStructuredStateValue("location.Path").ShouldBe("rules/2026.html");
    }

    private static ServiceProvider BuildProvider(out FakeLogCollector collector)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders().AddFakeLogging();
        builder.AddRedaction();

        var provider = builder.Services.BuildServiceProvider();
        collector = provider.GetFakeLogCollector();
        return provider;
    }
}