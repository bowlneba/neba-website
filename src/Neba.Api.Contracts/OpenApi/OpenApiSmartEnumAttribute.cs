namespace Neba.Api.Contracts.OpenApi;

/// <summary>
/// Associates a contract property with one or more SmartEnum type names so OpenAPI schema generation
/// can advertise the allowed values.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class OpenApiSmartEnumAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSmartEnumAttribute"/> class.
    /// </summary>
    /// <param name="smartEnumTypeName">The name of the backing SmartEnum type (for example, <c>SponsorTier</c>).</param>
    public OpenApiSmartEnumAttribute(string smartEnumTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(smartEnumTypeName);
        SmartEnumTypeName = smartEnumTypeName;
    }

    /// <summary>
    /// Gets the SmartEnum type name used to resolve the allowed values.
    /// </summary>
    public string SmartEnumTypeName { get; }
}