using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Contracts.Compliance;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Storage;

[UnitTest]
[Component("Storage")]
public sealed class LogPropertiesRedactionTests
{
    [Fact(DisplayName = "LogProperties should redact classified properties through the redaction pipeline")]
    public async Task LogProperties_ShouldRedactClassifiedProperties()
    {
        // Arrange
        await using var provider = BuildProvider(out var collector);
        var logger = provider.GetRequiredService<ILogger<LogPropertiesRedactionTests>>();

        // Act
        var recipient = new Recipient("log-target@example.com", "Vendor Ltd", "secret-token");
        logger.LogRecipient(recipient);

        // Assert
        var record = collector.GetSnapshot().ShouldHaveSingleItem();

        record.GetStructuredStateValue("recipient.Company").ShouldBe("Vendor Ltd");

        var email = record.GetStructuredStateValue("recipient.Email");
        email.ShouldNotBeNull();
        email.ShouldMatch(@"^l\*+$");
        email.ShouldNotContain("@example.com");

        record.GetStructuredStateValue("recipient.Token").ShouldBeNullOrEmpty();
        record.Message.ShouldNotContain("secret-token");
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

internal sealed record Recipient(
    [property: PersonalData] string Email,
    string Company,
    [property: PrivateData] string Token);

internal static partial class LogPropertiesRedactionTestsLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recipient logged")]
    public static partial void LogRecipient(
        this ILogger<LogPropertiesRedactionTests> logger,
        [LogProperties] Recipient recipient);
}