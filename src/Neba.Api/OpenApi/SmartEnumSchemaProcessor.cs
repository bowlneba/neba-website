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

        foreach (PropertyInfo property in modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attributes = property.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true).ToArray();
            if (attributes.Length == 0)
            {
                continue;
            }

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

    private static Dictionary<string, IReadOnlyCollection<string>> BuildEnumNamesByTypeName()
    {
        var smartEnumNamesByTypeName = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        foreach (Type type in GetSmartEnumTypes())
        {
            if (TryGetSmartEnumNames(type, out IReadOnlyCollection<string>? names) && names is not null)
            {
                smartEnumNamesByTypeName[type.Name] = names;
            }
        }

        return smartEnumNamesByTypeName;
    }

    private static IEnumerable<Type> GetSmartEnumTypes()
        => GetDomainAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsSmartEnum);

    private static IEnumerable<Assembly> GetDomainAssemblies()
    {
        EnsureNebaAssembliesLoaded();
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Neba.Domain", StringComparison.Ordinal) == true);
    }

    private static void EnsureNebaAssembliesLoaded()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(a => visited.Add(a.FullName!)));

        while (pending.TryDequeue(out Assembly? current))
        {
            EnqueueReferencedNebaAssemblies(current, visited, pending);
        }
    }

    private static void EnqueueReferencedNebaAssemblies(Assembly assembly, HashSet<string> visited, Queue<Assembly> pending)
    {
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            if (!visited.Add(reference.FullName!)) continue;
            if (reference.Name?.StartsWith("Neba", StringComparison.Ordinal) != true) continue;
            if (!TryLoadAssembly(reference, out Assembly? loaded) || loaded is null) continue;

            pending.Enqueue(loaded);
        }
    }

    private static bool TryLoadAssembly(AssemblyName assemblyName, out Assembly? assembly)
    {
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

    private static bool TryGetSmartEnumNames(Type type, out IReadOnlyCollection<string>? names)
    {
        names = null;

        var listProperty = type.GetProperty("List", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (listProperty?.GetValue(null) is not System.Collections.IEnumerable values)
        {
            return false;
        }

        var valueNames = new List<string>();
        foreach (object value in values)
        {
            string? name = value.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value) as string;
            if (!string.IsNullOrWhiteSpace(name))
            {
                valueNames.Add(name);
            }
        }

        if (valueNames.Count == 0)
        {
            return false;
        }

        names = [.. valueNames.Distinct(StringComparer.Ordinal)];
        return true;
    }

    private static bool IsSmartEnum(Type type)
    {
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
        if (!string.IsNullOrWhiteSpace(jsonPropertyName))
        {
            return jsonPropertyName;
        }

        return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}