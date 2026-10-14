using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.ServiceDefaults.Telemetry;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Telemetry;

[UnitTest]
[Component("ServiceDefaults.Telemetry")]
public sealed class ResourceMonitoringConfigurationTests
{
    [Fact(DisplayName = "AddResourceMonitoring should register the resource monitor on Linux and Windows only")]
    public void AddResourceMonitoring_ShouldRegisterResourceMonitor_OnSupportedPlatforms()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var supported = OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        // Act
        builder.AddResourceMonitoring();

        // Assert
        builder.Services.Any(descriptor =>
                descriptor.ServiceType.Assembly.GetName().Name == "Microsoft.Extensions.Diagnostics.ResourceMonitoring"
                || descriptor.ImplementationType?.Assembly.GetName().Name == "Microsoft.Extensions.Diagnostics.ResourceMonitoring")
            .ShouldBe(supported);
    }
}
