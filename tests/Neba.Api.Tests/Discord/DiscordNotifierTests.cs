using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class DiscordNotifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    private static DiscordNotifier CreateNotifier(HttpClient httpClient, FakeLogger<DiscordNotifier>? logger = null, FakeTimeProvider? timeProvider = null) =>
        new(httpClient, timeProvider ?? new FakeTimeProvider(Now), logger ?? new FakeLogger<DiscordNotifier>());

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://discord.example.com/webhook") };

    [Fact(DisplayName = "NotifyAsync should post the alert as a Discord embed payload")]
    public async Task NotifyAsync_ShouldPostAlertAsEmbedPayload_WhenCalled()
    {
        // Arrange
        using var handler = new CapturingHttpMessageHandler(HttpStatusCode.NoContent);
        var timeProvider = new FakeTimeProvider(Now);
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient, timeProvider: timeProvider);
        var alert = new DiscordAlert("Deploy Failed", DiscordAlertSeverity.Critical, "The deploy pipeline failed.",
            new Dictionary<string, object> { ["Environment"] = "Production" });

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        handler.RequestBody.ShouldNotBeNull();
        using var document = JsonDocument.Parse(handler.RequestBody);
        var embed = document.RootElement.GetProperty("embeds")[0];
        embed.GetProperty("title").GetString().ShouldBe("Deploy Failed");
        embed.GetProperty("description").GetString().ShouldBe("The deploy pipeline failed.");
        embed.GetProperty("color").GetInt32().ShouldBe(DiscordAlertSeverity.Critical.NotificationColor.RawValue);
        embed.GetProperty("timestamp").GetDateTimeOffset().ShouldBe(Now);
        var field = embed.GetProperty("fields")[0];
        field.GetProperty("name").GetString().ShouldBe("Environment");
        field.GetProperty("value").GetString().ShouldBe("Production");
        field.GetProperty("inline").GetBoolean().ShouldBeTrue();
    }

    [Fact(DisplayName = "NotifyAsync should omit fields when no metadata is provided")]
    public async Task NotifyAsync_ShouldOmitFields_WhenNoMetadataIsProvided()
    {
        // Arrange
        using var handler = new CapturingHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient);
        var alert = new DiscordAlert("Info Alert", DiscordAlertSeverity.Info, "Just letting you know.");

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        handler.RequestBody.ShouldNotBeNull();
        using var document = JsonDocument.Parse(handler.RequestBody);
        var embed = document.RootElement.GetProperty("embeds")[0];
        embed.GetProperty("fields").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact(DisplayName = "NotifyAsync should not log when the webhook accepts the alert")]
    public async Task NotifyAsync_ShouldNotLog_WhenWebhookAcceptsAlert()
    {
        // Arrange
        var fakeLogger = new FakeLogger<DiscordNotifier>();
        using var handler = new CapturingHttpMessageHandler(HttpStatusCode.NoContent);
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient, fakeLogger);
        var alert = new DiscordAlert("Info Alert", DiscordAlertSeverity.Info, "Just letting you know.");

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        fakeLogger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "NotifyAsync should log a warning and not throw when the webhook rejects the alert")]
    public async Task NotifyAsync_ShouldLogWarningAndNotThrow_WhenWebhookRejectsAlert()
    {
        // Arrange
        var fakeLogger = new FakeLogger<DiscordNotifier>();
        using var handler = new CapturingHttpMessageHandler(HttpStatusCode.TooManyRequests);
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient, fakeLogger);
        var alert = new DiscordAlert("Deploy Failed", DiscordAlertSeverity.Critical, "The deploy pipeline failed.");

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("Deploy Failed");
        record.Message.ShouldContain("429");
    }

    [Fact(DisplayName = "NotifyAsync should log a warning and not throw when posting the alert throws")]
    public async Task NotifyAsync_ShouldLogWarningAndNotThrow_WhenPostingThrows()
    {
        // Arrange
        var fakeLogger = new FakeLogger<DiscordNotifier>();
        using var handler = new ThrowingHttpMessageHandler(new HttpRequestException("connection refused"));
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient, fakeLogger);
        var alert = new DiscordAlert("Deploy Failed", DiscordAlertSeverity.Critical, "The deploy pipeline failed.");

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("Deploy Failed");
    }

    [Fact(DisplayName = "NotifyAsync should not swallow OperationCanceledException")]
    public async Task NotifyAsync_ShouldNotSwallowOperationCanceledException_WhenPostingIsCanceled()
    {
        // Arrange
        var fakeLogger = new FakeLogger<DiscordNotifier>();
        using var handler = new ThrowingHttpMessageHandler(new OperationCanceledException());
        using var httpClient = CreateHttpClient(handler);
        var notifier = CreateNotifier(httpClient, fakeLogger);
        var alert = new DiscordAlert("Deploy Failed", DiscordAlertSeverity.Critical, "The deploy pipeline failed.");

        // Act
        await Should.ThrowAsync<OperationCanceledException>(() => notifier.NotifyAsync(alert, TestContext.Current.CancellationToken));

        // Assert
        fakeLogger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    private sealed class CapturingHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw exception;
    }
}
