using Xunit.v3;

namespace Neba.TestFactory.Attributes;

/// <summary>
/// Marks a test class or method as an integration test.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute
{
    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Category", "Integration")];
}
