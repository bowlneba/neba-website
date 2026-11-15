using Neba.TestFactory.Attributes;
using Neba.Website.Server.Time;

namespace Neba.Website.Tests.Time;

[UnitTest]
[Component("Website.Time")]
public sealed class ClientTimeZoneServiceExtensionsTests
{
    private readonly Mock<IClientTimeZoneService> _serviceMock = new(MockBehavior.Strict);

    [Fact(DisplayName = "ToLocalOrNullAsync returns null and does not convert when the value is null")]
    public async Task ToLocalOrNullAsync_ShouldReturnNull_WhenValueIsNull()
    {
        // Arrange
        // Strict mock: any call to ToLocalAsync without a Setup would throw.

        // Act
        var local = await _serviceMock.Object.ToLocalOrNullAsync(null);

        // Assert
        local.ShouldBeNull();
    }

    [Fact(DisplayName = "ToLocalOrNullAsync returns the converted value when the value is set")]
    public async Task ToLocalOrNullAsync_ShouldReturnConvertedValue_WhenValueIsSet()
    {
        // Arrange
        var utc = new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 7, 25, 8, 0, 0, TimeSpan.FromHours(-4));
        _serviceMock
            .Setup(s => s.ToLocalAsync(utc))
            .ReturnsAsync(expected);

        // Act
        var local = await _serviceMock.Object.ToLocalOrNullAsync(utc);

        // Assert
        local.ShouldBe(expected);
    }
}