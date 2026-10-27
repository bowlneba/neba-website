using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Discord;

namespace Neba.Api.Tests.Discord;

[IntegrationTest]
[Component("Discord")]
public sealed class DiscordConfigurationTests
{
    [Fact(DisplayName = "AddDiscord should bind DiscordSettings from the Discord configuration section")]
    public void AddDiscord_ShouldBindDiscordSettings_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<DiscordSettings>>().Value;
        settings.ApiLogsWebhookUrl.ShouldBe("https://discord.com/api/webhooks/1/token");
        settings.ManualInterventionWebhookUrl.ShouldBe("https://discord.com/api/webhooks/2/token");
        settings.NewsWebhookUrl.ShouldBe("https://discord.com/api/webhooks/3/token");
    }

    [Fact(DisplayName = "AddDiscord should throw on start when the Discord section is absent")]
    public void AddDiscord_ShouldThrowOnStart_WhenSectionIsAbsent()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddDiscord();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<DiscordSettings>>().Value);
    }

    [Fact(DisplayName = "AddDiscord should register IDiscordNotifier as DiscordNotifier")]
    public void AddDiscord_ShouldRegisterIDiscordNotifier_AsDiscordNotifier()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });
        builder.Services.AddSingleton(TimeProvider.System);

        // Act
        builder.AddDiscord();

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDiscordNotifier>().ShouldBeOfType<DiscordNotifier>();
    }

    [Fact(DisplayName = "AddDiscord should configure the HttpClient BaseAddress from the webhook URL")]
    public void AddDiscord_ShouldConfigureHttpClientBaseAddress_FromWebhookUrl()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient(nameof(IDiscordNotifier));
        client.BaseAddress.ShouldBe(new Uri("https://discord.com/api/webhooks/1/token"));

        using var manualInterventionClient = httpClientFactory.CreateClient(nameof(IManualInterventionNotifier));
        manualInterventionClient.BaseAddress.ShouldBe(new Uri("https://discord.com/api/webhooks/2/token"));
    }

    [Fact(DisplayName = "AddDiscord should register INewsAnnouncementNotifier with the news webhook as its base address")]
    public void AddDiscord_ShouldRegisterNewsAnnouncementNotifier_WithNewsWebhookBaseAddress()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<INewsAnnouncementNotifier>().ShouldBeOfType<NewsAnnouncementNotifier>();

        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient(nameof(INewsAnnouncementNotifier));
        client.BaseAddress.ShouldBe(new Uri("https://discord.com/api/webhooks/3/token"));
    }

    [Fact(DisplayName = "AddDiscord should not retry a failed news announcement post")]
    public async Task AddDiscord_ShouldNotRetryNewsAnnouncementPost_WhenWebhookFails()
    {
        // Arrange
        var requestCount = 0;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });
        builder.AddDiscord();
        builder.Services.AddHttpClient(nameof(INewsAnnouncementNotifier))
            .ConfigurePrimaryHttpMessageHandler(() => new CountingFailureHandler(() => requestCount++));

        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<INewsAnnouncementNotifier>();

        // Act
        await Should.ThrowAsync<HttpRequestException>(
            () => notifier.NotifyAsync(NewsAnnouncementFactory.Create(), TestContext.Current.CancellationToken));

        // Assert
        requestCount.ShouldBe(1);
    }

    [Fact(DisplayName = "AddDiscord should throw on start when the manual-intervention webhook URL is blank")]
    public void AddDiscord_ShouldThrowOnStart_WhenManualInterventionWebhookUrlIsBlank()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<DiscordSettings>>().Value);
    }

    [Fact(DisplayName = "AddDiscord should throw on start when the news webhook URL is blank")]
    public void AddDiscord_ShouldThrowOnStart_WhenNewsWebhookUrlIsBlank()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = ""
        });

        // Act
        builder.AddDiscord();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<DiscordSettings>>().Value);
    }

    [Fact(DisplayName = "AddDiscord should register IManualInterventionNotifier as ManualInterventionNotifier")]
    public void AddDiscord_ShouldRegisterIManualInterventionNotifier_AsManualInterventionNotifier()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Discord:ApiLogsWebhookUrl"] = "https://discord.com/api/webhooks/1/token",
            ["Discord:ManualInterventionWebhookUrl"] = "https://discord.com/api/webhooks/2/token",
            ["Discord:NewsWebhookUrl"] = "https://discord.com/api/webhooks/3/token"
        });
        builder.Services.AddSingleton(TimeProvider.System);

        // Act
        builder.AddDiscord();

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IManualInterventionNotifier>().ShouldBeOfType<ManualInterventionNotifier>();
    }

    private sealed class CountingFailureHandler(Action onRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onRequest();

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        }
    }
}