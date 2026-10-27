using System.Net;
using System.Text.Json;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Discord;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class NewsAnnouncementNotifierTests
{
#pragma warning disable CA2000 // the client owns and disposes the handler
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, List<string> bodies) =>
        new(new StubHandler(statusCode, bodies), disposeHandler: true) { BaseAddress = new Uri("https://discord.example.com/news") };
#pragma warning restore CA2000

    [Fact(DisplayName = "NotifyAsync should post one embed with the title, article link and image")]
    public async Task NotifyAsync_ShouldPostEmbedWithTitleLinkAndImage_WhenArticleHasImage()
    {
        // Arrange
        var bodies = new List<string>();
        using var httpClient = CreateHttpClient(HttpStatusCode.NoContent, bodies);
        var notifier = new NewsAnnouncementNotifier(httpClient);
        var announcement = NewsAnnouncementFactory.Create(
            title: "Season Champions Crowned",
            articleUrl: new Uri("https://bowlneba.com/news/season-champions-crowned"),
            imageUrl: new Uri("https://files.bowlneba.com/news/header.jpg"));

        // Act
        await notifier.NotifyAsync(announcement, TestContext.Current.CancellationToken);

        // Assert
        bodies.ShouldHaveSingleItem();
        using var body = JsonDocument.Parse(bodies[0]);
        var embed = body.RootElement.GetProperty("embeds").EnumerateArray().Single();
        embed.GetProperty("title").GetString().ShouldBe("Season Champions Crowned");
        embed.GetProperty("url").GetString().ShouldBe("https://bowlneba.com/news/season-champions-crowned");
        embed.GetProperty("image").GetProperty("url").GetString().ShouldBe("https://files.bowlneba.com/news/header.jpg");
    }

    [Fact(DisplayName = "NotifyAsync should omit the image when the article has no header image")]
    public async Task NotifyAsync_ShouldOmitImage_WhenArticleHasNoImage()
    {
        // Arrange
        var bodies = new List<string>();
        using var httpClient = CreateHttpClient(HttpStatusCode.NoContent, bodies);
        var notifier = new NewsAnnouncementNotifier(httpClient);
        var announcement = NewsAnnouncementFactory.Create(imageUrl: null);

        // Act
        await notifier.NotifyAsync(announcement, TestContext.Current.CancellationToken);

        // Assert
        using var body = JsonDocument.Parse(bodies.Single());
        var embed = body.RootElement.GetProperty("embeds").EnumerateArray().Single();
        embed.TryGetProperty("image", out _).ShouldBeFalse();
    }

    [Fact(DisplayName = "NotifyAsync should throw when the webhook rejects the post")]
    public async Task NotifyAsync_ShouldThrow_WhenWebhookRejectsPost()
    {
        // Arrange
        var bodies = new List<string>();
        using var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, bodies);
        var notifier = new NewsAnnouncementNotifier(httpClient);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => notifier.NotifyAsync(NewsAnnouncementFactory.Create(), TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "NotifyAsync should send exactly one request when the webhook fails")]
    public async Task NotifyAsync_ShouldNotRetry_WhenWebhookFails()
    {
        // Arrange
        var bodies = new List<string>();
        using var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, bodies);
        var notifier = new NewsAnnouncementNotifier(httpClient);

        // Act
        await Should.ThrowAsync<HttpRequestException>(
            () => notifier.NotifyAsync(NewsAnnouncementFactory.Create(), TestContext.Current.CancellationToken));

        // Assert
        bodies.Count.ShouldBe(1);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, List<string> bodies) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(statusCode);
        }
    }
}
