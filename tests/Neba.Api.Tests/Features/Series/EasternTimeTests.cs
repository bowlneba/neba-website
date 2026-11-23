using System.Globalization;

using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class EasternTimeTests
{
    [Theory(DisplayName = "ToLocal should convert a UTC instant to Eastern wall-clock date and time")]
    [InlineData("2025-07-12T13:00:00Z", "2025-07-12", "09:00")] // EDT, UTC-4
    [InlineData("2026-01-15T14:00:00Z", "2026-01-15", "09:00")] // EST, UTC-5
    [InlineData("2026-07-15T01:30:00Z", "2026-07-14", "21:30")] // crosses the date line, EDT
    [InlineData("2026-01-16T02:30:00Z", "2026-01-15", "21:30")] // crosses the date line, EST
    [InlineData("2026-03-08T06:30:00Z", "2026-03-08", "01:30")] // 1:30 AM EST, before the spring-forward gap
    [InlineData("2026-03-08T07:00:00Z", "2026-03-08", "03:00")] // the gap: 2:00 AM EST becomes 3:00 AM EDT
    [InlineData("2026-03-08T07:30:00Z", "2026-03-08", "03:30")]
    [InlineData("2026-11-01T05:30:00Z", "2026-11-01", "01:30")] // 1:30 AM EDT, first pass of the repeated hour
    [InlineData("2026-11-01T06:30:00Z", "2026-11-01", "01:30")] // 1:30 AM EST, second pass
    [InlineData("2026-11-01T07:00:00Z", "2026-11-01", "02:00")] // after the repeat, EST
    public void ToLocal_ShouldReturnEasternDateAndTime(string utc, string expectedDate, string expectedTime)
    {
        // Arrange
        var instant = DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture);

        // Act
        var (date, time) = EasternTime.ToLocal(instant);

        // Assert
        date.ShouldBe(DateOnly.Parse(expectedDate, CultureInfo.InvariantCulture));
        time.ShouldBe(TimeOnly.Parse(expectedTime, CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "ToLocal should give the same result for an instant given in a non-UTC offset")]
    public void ToLocal_ShouldIgnoreTheInputOffset()
    {
        // Arrange
        var utc = new DateTimeOffset(2025, 7, 12, 13, 0, 0, TimeSpan.Zero);
        var sameInstant = utc.ToOffset(TimeSpan.FromHours(9));

        // Act
        var fromUtc = EasternTime.ToLocal(utc);
        var fromOffset = EasternTime.ToLocal(sameInstant);

        // Assert
        fromOffset.ShouldBe(fromUtc);
    }
}