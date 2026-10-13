using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class DiscordFieldFormatterTests
{
    [Fact(DisplayName = "JoinIds should return None when there are no ids")]
    public void JoinIds_ShouldReturnNone_WhenEmpty()
    {
        // Arrange
        int[] ids = [];

        // Act
        var result = DiscordFieldFormatter.JoinIds(ids);

        // Assert
        result.ShouldBe("None");
    }

    [Fact(DisplayName = "JoinIds should sort and de-duplicate ids")]
    public void JoinIds_ShouldSortAndDeduplicate_WhenUnderCap()
    {
        // Arrange
        int[] ids = [30, 10, 20, 10];

        // Act
        var result = DiscordFieldFormatter.JoinIds(ids);

        // Assert
        result.ShouldBe("10, 20, 30");
    }

    [Fact(DisplayName = "JoinIds should list every id when the count equals the cap")]
    public void JoinIds_ShouldListEveryId_WhenAtCap()
    {
        // Arrange
        var ids = Enumerable.Range(1, 5);

        // Act
        var result = DiscordFieldFormatter.JoinIds(ids, max: 5);

        // Assert
        result.ShouldBe("1, 2, 3, 4, 5");
    }

    [Fact(DisplayName = "JoinIds should truncate and count the remainder when over the cap")]
    public void JoinIds_ShouldTruncate_WhenOverCap()
    {
        // Arrange
        var ids = Enumerable.Range(1, 8);

        // Act
        var result = DiscordFieldFormatter.JoinIds(ids, max: 5);

        // Assert
        result.ShouldBe("1, 2, 3, 4, 5 (+3 more)");
    }

    [Fact(DisplayName = "JoinIds should stay under the 1024-character field limit at the default cap")]
    public void JoinIds_ShouldStayUnderFieldLimit_ForLargestIds()
    {
        // Arrange
        var ids = Enumerable.Range(int.MaxValue - 1000, 1000);

        // Act
        var result = DiscordFieldFormatter.JoinIds(ids);

        // Assert
        result.Length.ShouldBeLessThan(1024);
    }
}
