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
        var settings = new DiscordSettings { WebhookUrl = "https://discord.com/api/webhooks/1/token" };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Missing webhook URL fails validation")]
    public void Validate_WithEmptyWebhookUrl_Fails()
    {
        // Arrange
        var settings = new DiscordSettings { WebhookUrl = string.Empty };

        // Act
        var isValid = TryValidate(settings, out var results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(DiscordSettings.WebhookUrl)));
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