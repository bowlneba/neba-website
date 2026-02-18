using Neba.Infrastructure.Clock;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Clock;

[UnitTest]
[Component("Clock")]
public sealed class DateTimeProviderTests
{
    [Fact(DisplayName = "UtcNow should return the current UTC time")]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var provider = new DateTimeProvider();

        // Act
        var result = provider.UtcNow;

        // Assert
        var after = DateTimeOffset.UtcNow;
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact(DisplayName = "Today should return the current UTC date")]
    public void Today_ShouldReturnCurrentUtcDate()
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        var result = provider.Today;

        // Assert
        result.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}