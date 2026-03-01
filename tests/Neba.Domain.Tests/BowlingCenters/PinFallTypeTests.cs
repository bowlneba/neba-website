using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters.PinFallType")]
public sealed class PinFallTypeTests
{
    [Fact(DisplayName = "Should have 2 pin fall types")]
    public void PinFallType_ShouldHave2Types()
    {
        // Act
        var count = PinFallType.List.Count;

        // Assert
        count.ShouldBe(2);
    }

    [Theory(DisplayName = "Pin fall type values should be correct")]
    [InlineData("Free Fall", "FF", TestDisplayName = "Free Fall value should be FF")]
    [InlineData("String Pin", "SP", TestDisplayName = "String Pin value should be SP")]
    public void PinFallType_ShouldHaveCorrectProperties(string expectedName, string value)
    {
        // Act
        var type = PinFallType.FromValue(value);

        // Assert
        type.Name.ShouldBe(expectedName);
        type.Value.ShouldBe(value);
    }
}
