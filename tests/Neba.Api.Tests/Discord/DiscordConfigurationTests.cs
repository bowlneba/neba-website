using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

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
            ["Discord:WebhookUrl"] = "https://discord.com/api/webhooks/1/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<DiscordSettings>>().Value;
        settings.WebhookUrl.ShouldBe("https://discord.com/api/webhooks/1/token");
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
            ["Discord:WebhookUrl"] = "https://discord.com/api/webhooks/1/token"
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
            ["Discord:WebhookUrl"] = "https://discord.com/api/webhooks/1/token"
        });

        // Act
        builder.AddDiscord();

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient(nameof(IDiscordNotifier));
        client.BaseAddress.ShouldBe(new Uri("https://discord.com/api/webhooks/1/token"));
    }
}