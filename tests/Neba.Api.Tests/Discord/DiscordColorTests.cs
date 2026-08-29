using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class DiscordColorTests
{
    [Theory(DisplayName = "RawValue should pack RGB components into a single hex integer")]
    [InlineData(0x34, 0x98, 0xDB, 0x3498DB, TestDisplayName = "Blue-ish RGB packs to 0x3498DB")]
    [InlineData(0x00, 0x00, 0x00, 0x000000, TestDisplayName = "Black RGB packs to 0x000000")]
    [InlineData(0xFF, 0xFF, 0xFF, 0xFFFFFF, TestDisplayName = "White RGB packs to 0xFFFFFF")]
    public void RawValue_ShouldPackRgbComponents_IntoHexInteger(byte r, byte g, byte b, int expected)
    {
        // Arrange
        var color = new DiscordColor(r, g, b);

        // Act
        var rawValue = color.RawValue;

        // Assert
        rawValue.ShouldBe(expected);
    }

    [Theory(DisplayName = "Named presets should match their documented hex values")]
    [InlineData("Blue", 0x3498DB)]
    [InlineData("Yellow", 0xF1C40F)]
    [InlineData("Red", 0xE74C3C)]
    public void NamedPresets_ShouldMatchDocumentedHexValues(string presetName, int expectedRawValue)
    {
        // Arrange
        var preset = presetName switch
        {
            "Blue" => DiscordColor.Blue,
            "Yellow" => DiscordColor.Yellow,
            "Red" => DiscordColor.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(presetName))
        };

        // Act
        var rawValue = preset.RawValue;

        // Assert
        rawValue.ShouldBe(expectedRawValue);
    }

    [Fact(DisplayName = "Colors with the same RGB components should be equal")]
    public void Equality_ShouldBeStructural_ForSameRgbComponents()
    {
        // Arrange
        var first = new DiscordColor(0x12, 0x34, 0x56);
        var second = new DiscordColor(0x12, 0x34, 0x56);

        // Act
        var areEqual = first == second;

        // Assert
        areEqual.ShouldBeTrue();
    }
}