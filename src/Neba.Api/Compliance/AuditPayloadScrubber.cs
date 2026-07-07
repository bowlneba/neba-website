using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Neba.Api.Compliance;

/// <summary>
/// Applies the [PublicData]/[PersonalData]/[PrivateData] property-level classification to an
/// arbitrary object graph before it is written to an audit store: Private properties are
/// omitted, Personal properties are star-masked, Public/unclassified properties pass through.
/// Nested reference-typed properties (e.g. a command's nested Input DTO) and collections are
/// scrubbed recursively so their own classified properties aren't leaked unredacted.
/// </summary>
internal static class AuditPayloadScrubber
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static IReadOnlyDictionary<string, object?> Scrub<T>(T source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (IReadOnlyDictionary<string, object?>)ScrubObject(source, new HashSet<object>(ReferenceEqualityComparer.Instance))!;
    }

    private static Dictionary<string, object?> ScrubObject(object source, HashSet<object> visiting)
    {
        var type = source.GetType();
        var properties = PropertyCache.GetOrAdd(type, static t
            => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var result = new Dictionary<string, object?>(properties.Length);

        // Guards against infinite recursion on circular object graphs - a repeated reference is
        // omitted rather than re-scrubbed, since it would already appear elsewhere in the payload.
        if (!visiting.Add(source))
        {
            return result;
        }

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

            result[property.Name] = ScrubValue(value, visiting);
        }

        visiting.Remove(source);

        return result;
    }

    private static object? ScrubValue(object? value, HashSet<object> visiting)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();

        if (IsSimpleType(type))
        {
            return value;
        }

        if (value is IEnumerable enumerable)
        {
            var scrubbedItems = new List<object?>();

            foreach (var item in enumerable)
            {
                scrubbedItems.Add(ScrubValue(item, visiting));
            }

            return scrubbedItems;
        }

        // A nested complex object (e.g. a command's Input DTO) - scrub it using its own
        // property classifications rather than passing the raw instance through unredacted.
        return ScrubObject(value, visiting);
    }

    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive
            || underlyingType.IsEnum
            || underlyingType == typeof(string)
            || underlyingType == typeof(decimal)
            || underlyingType == typeof(DateTime)
            || underlyingType == typeof(DateTimeOffset)
            || underlyingType == typeof(Guid)
            || underlyingType == typeof(TimeSpan);
    }

    private static string Mask(string value)
        => string.IsNullOrEmpty(value)
            ? value
            : $"{value[0]}{new string('*', value.Length - 1)}";
}