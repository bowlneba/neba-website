using Xunit.v3;

namespace Neba.TestFactory.Attributes;

/// <summary>
/// Marks a test class or method as a unit test.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class UnitTestAttribute : Attribute, ITraitAttribute
{
    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Category", "Unit")];
}
