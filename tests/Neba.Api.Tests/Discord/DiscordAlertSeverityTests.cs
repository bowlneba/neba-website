using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class DiscordAlertSeverityTests
{
    [Fact(DisplayName = "Should have 3 severities")]
    public void DiscordAlertSeverity_ShouldHave3Severities()
    {
        // Act
        var count = DiscordAlertSeverity.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Severity values and colors should be correct")]
    [InlineData("Info", 0, 0x3498DB, TestDisplayName = "Info should be value 0 with blue color")]
    [InlineData("Warning", 1, 0xF1C40F, TestDisplayName = "Warning should be value 1 with yellow color")]
    [InlineData("Critical", 2, 0xE74C3C, TestDisplayName = "Critical should be value 2 with red color")]
    public void DiscordAlertSeverity_ShouldHaveCorrectProperties(string expectedName, int value, int expectedColor)
    {
        // Act
        var severity = DiscordAlertSeverity.FromValue(value);

        // Assert
        severity.Name.ShouldBe(expectedName);
        severity.Value.ShouldBe(value);
        severity.NotificationColor.RawValue.ShouldBe(expectedColor);
    }

    [Fact(DisplayName = "Every severity should have a distinct, non-zero notification color")]
    public void DiscordAlertSeverity_ShouldHaveDistinctNonZeroColors_ForEverySeverity()
    {
        // Act
        var colors = DiscordAlertSeverity.List.Select(severity => severity.NotificationColor.RawValue).ToList();

        // Assert
        colors.ShouldAllBe(color => color != 0);
        colors.Distinct().Count().ShouldBe(colors.Count);
    }
}