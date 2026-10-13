using System.Net;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class ManualInterventionNotifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

#pragma warning disable CA2000 // the client owns and disposes the handler
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, List<HttpRequestMessage>? requests = null) =>
        new(new StubHandler(statusCode, requests), disposeHandler: true) { BaseAddress = new Uri("https://discord.example.com/manual") };
#pragma warning restore CA2000

    [Fact(DisplayName = "NotifyAsync should post a warning to the manual-intervention webhook only")]
    public async Task NotifyAsync_ShouldPostOnlyToManualWebhook_WhenAlertIsWarning()
    {
        // Arrange
        var requests = new List<HttpRequestMessage>();
        using var httpClient = CreateHttpClient(HttpStatusCode.NoContent, requests);
        var apiLog = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var notifier = new ManualInterventionNotifier(httpClient, new FakeTimeProvider(Now), apiLog.Object);
        var alert = new DiscordAlert(DiscordAlertSeverity.Warning, "Unsynced", "Link the bowler.");

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        requests.ShouldHaveSingleItem();
        apiLog.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "NotifyAsync should also post a critical alert to the api-logs notifier")]
    public async Task NotifyAsync_ShouldMirrorToApiLogs_WhenAlertIsCritical()
    {
        // Arrange
        using var httpClient = CreateHttpClient(HttpStatusCode.NoContent);
        var apiLog = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var alert = new DiscordAlert(DiscordAlertSeverity.Critical, "Season unmatched", "No match.");
        apiLog.Setup(n => n.NotifyAsync(alert, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var notifier = new ManualInterventionNotifier(httpClient, new FakeTimeProvider(Now), apiLog.Object);

        // Act
        await notifier.NotifyAsync(alert, TestContext.Current.CancellationToken);

        // Assert
        apiLog.Verify(n => n.NotifyAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "NotifyAsync should throw when the webhook rejects the alert")]
    public async Task NotifyAsync_ShouldThrow_WhenWebhookRejectsAlert()
    {
        // Arrange
        using var httpClient = CreateHttpClient(HttpStatusCode.NotFound);
        var apiLog = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        var notifier = new ManualInterventionNotifier(httpClient, new FakeTimeProvider(Now), apiLog.Object);
        var alert = new DiscordAlert(DiscordAlertSeverity.Critical, "Season unmatched", "No match.");

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => notifier.NotifyAsync(alert, TestContext.Current.CancellationToken));
        apiLog.VerifyNoOtherCalls();
    }

    private sealed class StubHandler(HttpStatusCode statusCode, List<HttpRequestMessage>? requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests?.Add(request);
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}