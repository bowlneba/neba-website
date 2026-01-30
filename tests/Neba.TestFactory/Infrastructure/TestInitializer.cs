using System.Runtime.CompilerServices;

namespace Neba.TestFactory.Infrastructure;

/// <summary>
/// Module initializer to configure the test environment.
/// Prevents file descriptor exhaustion on Linux by disabling config reload.
/// </summary>
internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            "DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE",
            "false");
    }
}