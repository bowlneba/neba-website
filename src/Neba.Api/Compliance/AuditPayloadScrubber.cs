using System.Collections.Concurrent;
using System.Reflection;

namespace Neba.Api.Compliance;

/// <summary>
/// Applies the [PublicData]/[PersonalData]/[PrivateData] property-level classification to an
/// arbitrary object graph before it is written to an audit store: Private properties are
/// omitted, Personal properties are star-masked, Public/unclassified properties pass through.
/// </summary>
internal static class AuditPayloadScrubber
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static IReadOnlyDictionary<string, object?> Scrub<T>(T source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var type = source.GetType();
        var properties = PropertyCache.GetOrAdd(type, static t 
            => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var result = new Dictionary<string, object?>(properties.Length);

        foreach (var property in properties)
        {
            var value = property.GetValue(source);

            if (property.GetCustomAttribute<PrivateDataAttribute>() is not null)
            {
                // Fully redacted - omit the key entirely
                continue;
            }

            if (property.GetCustomAttribute<PersonalDataAttribute>() is not null
                && value is string stringValue)
            {
                result[property.Name] = Mask(stringValue);

                continue;
            }

            result[property.Name] = value;
        }

        return result;
    }

    private static string Mask(string value)
        => string.IsNullOrEmpty(value)
            ? value
            : $"{value[0]}{new string('*', value.Length - 1)}";
}