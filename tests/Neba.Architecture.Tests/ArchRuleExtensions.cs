using ArchUnitNET.Domain;

using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Neba.Architecture.Tests;

internal static class ArchRuleExtensions
{
    /// <summary>
    /// Checks the rule against the architecture, failing the test with a descriptive message
    /// if any violations are found. Uses the core ArchUnitNET API to avoid a dependency on
    /// TngTech.ArchUnitNET.xUnit (which targets xUnit v2 and conflicts with our xUnit v3).
    /// </summary>
    internal static void Check(this IArchRule rule, ArchitectureModel architecture)
    {
        rule.HasNoViolations(architecture).ShouldBeTrue(rule.Description);
    }
}