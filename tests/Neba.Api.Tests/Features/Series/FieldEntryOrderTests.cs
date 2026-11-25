using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class FieldEntryOrderTests
{
    private sealed record Entry(int? Place, int? Points, string Last, string First, int Id);

    private static List<int> Order(params Entry[] entries)
        => [.. entries.InFieldOrder(e => e.Place, e => e.Points, e => e.Last, e => e.First, e => e.Id).Select(e => e.Id)];

    [Fact(DisplayName = "InFieldOrder should put placed bowlers first by place and unplaced bowlers last")]
    public void InFieldOrder_ShouldOrderByPlace_WithUnplacedLast()
    {
        // Arrange
        var entries = new[]
        {
            new Entry(null, 10, "Aaron", "Al", 1),
            new Entry(3, null, "Baker", "Bo", 2),
            new Entry(1, null, "Cole", "Cy", 3),
        };

        // Act
        var order = Order(entries);

        // Assert
        order.ShouldBe([3, 2, 1]);
    }

    [Fact(DisplayName = "InFieldOrder should order unplaced bowlers by Bowler of the Year points, high to low, with no points last")]
    public void InFieldOrder_ShouldOrderBackfillByPointsDescending()
    {
        // Arrange
        var entries = new[]
        {
            new Entry(null, null, "Aaron", "Al", 1),
            new Entry(null, 40, "Baker", "Bo", 2),
            new Entry(null, 90, "Cole", "Cy", 3),
        };

        // Act
        var order = Order(entries);

        // Assert
        order.ShouldBe([3, 2, 1]);
    }

    [Fact(DisplayName = "InFieldOrder should break ties by last name, then first name, ignoring case, then by id")]
    public void InFieldOrder_ShouldBreakTiesByNameThenId()
    {
        // Arrange
        var entries = new[]
        {
            new Entry(2, null, "smith", "zed", 4),
            new Entry(2, null, "Smith", "Amy", 3),
            new Entry(2, null, "Adams", "Zoe", 2),
            new Entry(2, null, "Smith", "amy", 1),
        };

        // Act
        var order = Order(entries);

        // Assert
        order.ShouldBe([2, 1, 3, 4]);
    }
}
