using Xunit.v3;

namespace Neba.TestFactory.Attributes;

/// <summary>
/// Identifies the component or feature being tested.
/// </summary>
/// <param name="name">The name of the component or feature being tested.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ComponentAttribute(string name) : Attribute, ITraitAttribute
{
    /// <summary>
    /// Gets the component or feature name.
    /// </summary>
    public string Name { get; } = name;

    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Component", Name)];
}
