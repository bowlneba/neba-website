using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests;

[UnitTest]
[Component("Time")]
public sealed class EasternTimeZoneTests
{
    [Fact(DisplayName = "Instance should be an Eastern zone that observes daylight saving")]
    public void Instance_ShouldBeEasternZone()
    {
        // Arrange
        var winter = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var summer = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var winterOffset = EasternTimeZone.Instance.GetUtcOffset(winter);
        var summerOffset = EasternTimeZone.Instance.GetUtcOffset(summer);

        // Assert
        winterOffset.ShouldBe(TimeSpan.FromHours(-5));
        summerOffset.ShouldBe(TimeSpan.FromHours(-4));
    }
}
