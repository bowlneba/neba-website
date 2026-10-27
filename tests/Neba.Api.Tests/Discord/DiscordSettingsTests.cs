using System.ComponentModel.DataAnnotations;

using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class DiscordSettingsTests
{
    [Fact(DisplayName = "Valid settings pass validation")]
    public void Validate_WithValidWebhookUrl_Succeeds()
    {
        // Arrange
        var settings = new DiscordSettings
        {
            ApiLogsWebhookUrl = "https://discord.com/api/webhooks/1/token",
            ManualInterventionWebhookUrl = "https://discord.com/api/webhooks/2/token",
            NewsWebhookUrl = "https://discord.com/api/webhooks/3/token"
        };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Missing api-logs webhook URL fails validation")]
    public void Validate_WithEmptyApiLogsWebhookUrl_Fails()
    {
        // Arrange
        var settings = new DiscordSettings
        {
            ApiLogsWebhookUrl = string.Empty,
            ManualInterventionWebhookUrl = "https://discord.com/api/webhooks/2/token",
            NewsWebhookUrl = "https://discord.com/api/webhooks/3/token"
        };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(DiscordSettings.ApiLogsWebhookUrl)));
    }

    [Fact(DisplayName = "Missing manual-intervention webhook URL fails validation")]
    public void Validate_WithEmptyManualInterventionWebhookUrl_Fails()
    {
        // Arrange
        var settings = new DiscordSettings
        {
            ApiLogsWebhookUrl = "https://discord.com/api/webhooks/1/token",
            NewsWebhookUrl = "https://discord.com/api/webhooks/3/token"
        };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(DiscordSettings.ManualInterventionWebhookUrl)));
    }

    [Fact(DisplayName = "Missing news webhook URL fails validation")]
    public void Validate_WithEmptyNewsWebhookUrl_Fails()
    {
        // Arrange
        var settings = new DiscordSettings
        {
            ApiLogsWebhookUrl = "https://discord.com/api/webhooks/1/token",
            ManualInterventionWebhookUrl = "https://discord.com/api/webhooks/2/token"
        };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(DiscordSettings.NewsWebhookUrl)));
    }

    [Fact(DisplayName = "SectionName should be Discord")]
    public void SectionName_ShouldBeDiscord()
    {
        // Assert
        DiscordSettings.SectionName.ShouldBe("Discord");
    }

    private static bool TryValidate(DiscordSettings settings, out List<ValidationResult> results)
    {
        var context = new ValidationContext(settings);
        results = [];
        return Validator.TryValidateObject(settings, context, results, validateAllProperties: true);
    }
}