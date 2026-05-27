using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Caching;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Caching;

[UnitTest]
[Component("Infrastructure.Caching")]
public sealed class CachingConfigurationTests
{
    [Fact(DisplayName = "AddCaching throws InvalidOperationException when connection string is missing")]
    public void AddCaching_Throws_WhenConnectionStringMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => services.AddCaching(config));
    }

    [Fact(DisplayName = "AddCaching registers IFusionCache and IDistributedCache when connection string is provided")]
    public void AddCaching_RegistersExpectedServices_WhenConnectionStringProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bowlneba"] = "Host=fake;Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        services.AddCaching(config);

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(IFusionCache));
        services.ShouldContain(d => d.ServiceType == typeof(IDistributedCache));
    }
}