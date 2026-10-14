using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
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
        using var provider = builder.Services.BuildServiceProvider();
#pragma warning disable CS0618 // IResourceMonitor is obsolete; only its registration is checked
        var monitor = provider.GetService<IResourceMonitor>();
#pragma warning restore CS0618
        (monitor is not null).ShouldBe(supported);
    }
}
