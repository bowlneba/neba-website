using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Caching;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Caching;

[UnitTest]
[Component("Infrastructure.Caching")]
public sealed class CachingConfigurationTests
{
    [Fact(DisplayName = "AddCaching registers IFusionCache and IDistributedCache")]
    public void AddCaching_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCaching();

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(IFusionCache));
        services.ShouldContain(d => d.ServiceType == typeof(IDistributedCache));
    }
}