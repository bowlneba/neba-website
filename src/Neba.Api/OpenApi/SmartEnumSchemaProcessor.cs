using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Neba.Api.Contracts.OpenApi;

using NJsonSchema;
using NJsonSchema.Generation;

namespace Neba.Api.OpenApi;

internal sealed class SmartEnumSchemaProcessor : ISchemaProcessor
{
    private static readonly Lazy<Dictionary<string, IReadOnlyCollection<string>>> EnumNamesByTypeName =
        new(BuildEnumNamesByTypeName, LazyThreadSafetyMode.ExecutionAndPublication);

    public void Process(SchemaProcessorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Type? modelType = context.ContextualType?.Type;
        if (modelType is null || context.Schema?.Properties is null || context.Schema.Properties.Count == 0)
        {
            return;
        }

        foreach (PropertyInfo property in modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true).Any()))
        {
            var attributes = property.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true).ToArray();

            string jsonPropertyName = GetJsonPropertyName(property);
            if (!context.Schema.Properties.TryGetValue(jsonPropertyName, out JsonSchemaProperty? propertySchema))
            {
                continue;
            }

            var enumNames = attributes
                .SelectMany(attribute => ResolveEnumNames(attribute.SmartEnumTypeName))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (enumNames.Length == 0)
            {
                continue;
            }

            ApplyEnumValues(propertySchema, enumNames);
        }
    }

    private static void ApplyEnumValues(JsonSchema schema, IReadOnlyCollection<string> enumNames)
    {
        if (schema.Type.HasFlag(JsonObjectType.Array) && schema.Item is not null)
        {
            SetEnumeration(schema.Item, enumNames);
            return;
        }

        SetEnumeration(schema, enumNames);
    }

    private static void SetEnumeration(JsonSchema schema, IReadOnlyCollection<string> enumNames)
    {
        schema.Enumeration.Clear();
        foreach (string enumName in enumNames)
        {
            schema.Enumeration.Add(enumName);
        }
    }

    private static IEnumerable<string> ResolveEnumNames(string smartEnumTypeName)
        => EnumNamesByTypeName.Value.TryGetValue(smartEnumTypeName, out IReadOnlyCollection<string>? names)
            ? names
            : [];

    // Stryker disable once Block : Static Lazy<> is initialized once per process; block removal mutations in init methods cannot be detected
    private static Dictionary<string, IReadOnlyCollection<string>> BuildEnumNamesByTypeName()
    {
        // Stryker disable all : Static Lazy<> is initialized once per process; mutations in these methods
        // are never triggered in subsequent test runs because the cached dictionary is already populated.
        var smartEnumNamesByTypeName = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        foreach ((Type type, IReadOnlyCollection<string>? names) in GetSmartEnumTypes()
                     .Select(type =>
                     {
                         _ = TryGetSmartEnumNames(type, out IReadOnlyCollection<string>? names);
                         return (type, names);
                     })
                     .Where(result => result.names is not null))
        {
            smartEnumNamesByTypeName[type.Name] = names!;
        }

        return smartEnumNamesByTypeName;
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static IEnumerable<Type> GetSmartEnumTypes()
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        return GetDomainAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsSmartEnum);
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static IEnumerable<Assembly> GetDomainAssemblies()
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        EnsureNebaAssembliesLoaded();
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Neba.Domain", StringComparison.Ordinal) == true);
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static void EnsureNebaAssembliesLoaded()
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(a => visited.Add(a.FullName!)));

        while (pending.TryDequeue(out Assembly? current))
        {
            EnqueueReferencedNebaAssemblies(current, visited, pending);
        }
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static void EnqueueReferencedNebaAssemblies(Assembly assembly, HashSet<string> visited, Queue<Assembly> pending)
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        foreach (Assembly loaded in assembly
                     .GetReferencedAssemblies()
                     .Where(reference => reference.Name?.StartsWith("Neba", StringComparison.Ordinal) == true && visited.Add(reference.FullName!))
                     .Select(reference => new { Success = TryLoadAssembly(reference, out Assembly? loaded), Loaded = loaded })
                     .Where(x => x.Success && x.Loaded is not null)
                     .Select(x => x.Loaded!))
        {
            pending.Enqueue(loaded);
        }
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static bool TryLoadAssembly(AssemblyName assemblyName, out Assembly? assembly)
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        try
        {
            assembly = Assembly.Load(assemblyName);
            return true;
        }
        catch (FileNotFoundException)
        {
            assembly = null;
            return false;
        }
        catch (FileLoadException)
        {
            assembly = null;
            return false;
        }
        catch (BadImageFormatException)
        {
            assembly = null;
            return false;
        }
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static bool TryGetSmartEnumNames(Type type, out IReadOnlyCollection<string>? names)
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        names = null;

        var listProperty = type.GetProperty("List", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (listProperty?.GetValue(null) is not System.Collections.IEnumerable values)
        {
            return false;
        }

        var valueNames = values
            .Cast<object>()
            .Select(value => value.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value) as string)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (valueNames.Length == 0)
        {
            return false;
        }

        names = valueNames;
        return true;
    }

    // Stryker disable once Block : see BuildEnumNamesByTypeName
    private static bool IsSmartEnum(Type type)
    {
        // Stryker disable all : see BuildEnumNamesByTypeName
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        for (Type? current = type; current is not null; current = current.BaseType)
        {
            if (!current.IsGenericType)
            {
                continue;
            }

            string? genericTypeName = current.GetGenericTypeDefinition().FullName;
            if (genericTypeName is "Ardalis.SmartEnum.SmartEnum`1"
                or "Ardalis.SmartEnum.SmartEnum`2"
                or "Ardalis.SmartEnum.SmartFlagEnum`1")
            {
                return true;
            }
        }

        return false;
    }

    private static string GetJsonPropertyName(PropertyInfo property)
    {
        var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
        return !string.IsNullOrWhiteSpace(jsonPropertyName)
            ? jsonPropertyName
            : JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}