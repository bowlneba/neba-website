using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.CircuitServicesAccessor")]
public sealed class CircuitServicesAccessorTests
{
    [Fact(DisplayName = "Services should be null until set")]
    public void Services_ShouldBeNull_ByDefault()
    {
        // Arrange & Act
        var sut = new CircuitServicesAccessor();

        // Assert
        sut.Services.ShouldBeNull();
    }

    [Fact(DisplayName = "Services set in one async flow should not be visible in a parallel flow")]
    public async Task Services_ShouldNotLeakAcrossAsyncFlows()
    {
        // Arrange
        await using var circuitServices = new ServiceCollection().BuildServiceProvider();
        var sut = new CircuitServicesAccessor();

        // Act
        var seenInOtherFlow = await Task.Run(() =>
        {
            sut.Services = circuitServices;
            return Task.Run(() => sut.Services);
        }, TestContext.Current.CancellationToken);
        var seenHere = sut.Services;

        // Assert
        seenInOtherFlow.ShouldBeSameAs(circuitServices);
        seenHere.ShouldBeNull();
    }
}