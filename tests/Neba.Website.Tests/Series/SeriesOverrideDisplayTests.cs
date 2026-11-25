using Neba.TestFactory.Attributes;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesOverrideDisplayTests
{
    [Theory(DisplayName = "AddBowlerLabel should name the Stage order")]
    [InlineData(1, "Add bowler to the 1st tournament", TestDisplayName = "1st tournament")]
    [InlineData(12, "Add bowler to the 12th tournament", TestDisplayName = "12th tournament")]
    public void AddBowlerLabel_ShouldNameStage(int order, string expected)
    {
        // Arrange

        // Act
        var label = SeriesDisplay.AddBowlerLabel(order);

        // Assert
        label.ShouldBe(expected);
    }

    [Fact(DisplayName = "ClearSlotLabel should name the bowler")]
    public void ClearSlotLabel_ShouldNameBowler()
    {
        // Arrange & Act
        var label = SeriesDisplay.ClearSlotLabel("Marcus Alvarez");

        // Assert
        label.ShouldBe("Clear slot for Marcus Alvarez");
    }

    [Fact(DisplayName = "RemoveOverrideLabel should name the bowler")]
    public void RemoveOverrideLabel_ShouldNameBowler()
    {
        // Arrange & Act
        var label = SeriesDisplay.RemoveOverrideLabel("Marcus Alvarez");

        // Assert
        label.ShouldBe("Remove override for Marcus Alvarez");
    }

    [Fact(DisplayName = "OverrideRemovedMessage should say the override is removed")]
    public void OverrideRemovedMessage_ShouldSayRemoved()
    {
        // Arrange & Act
        const string message = SeriesDisplay.OverrideRemovedMessage;

        // Assert
        message.ShouldBe("Override removed.");
    }
}