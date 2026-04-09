using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Neba.Api.Contracts.OpenApi;
using Neba.Api.OpenApi;
using Neba.TestFactory.Attributes;

using NJsonSchema;
using NJsonSchema.Generation;

namespace Neba.Api.Tests.OpenApi;

[UnitTest]
[Component("Api.OpenApi")]
public sealed class SmartEnumSchemaProcessorTests
{
    [Fact(DisplayName = "Process should populate enum values for all OpenApiSmartEnum properties")]
    public void Process_ShouldPopulateEnumValues_WhenPropertyHasOpenApiSmartEnumAttribute()
    {
        // Arrange
        var contractAssembly = typeof(OpenApiSmartEnumAttribute).Assembly;
        var propertiesWithAttributes = contractAssembly
            .GetTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true).Any())
            .ToArray();

        propertiesWithAttributes.ShouldNotBeEmpty();

        // Act + Assert
        foreach (var property in propertiesWithAttributes)
        {
            var schema = GenerateSchemaFor(property.DeclaringType!);

            string jsonPropertyName = GetJsonPropertyName(property);
            var hasProperty = schema.Properties.TryGetValue(jsonPropertyName, out JsonSchemaProperty? schemaProperty);

            hasProperty.ShouldBeTrue($"Schema for {property.DeclaringType!.Name} should contain '{jsonPropertyName}'.");
            schemaProperty.ShouldNotBeNull();

            var expectedValues = property
                .GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true)
                .SelectMany(attribute => ResolveSmartEnumNames(attribute.SmartEnumTypeName))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            expectedValues.ShouldNotBeEmpty($"Expected SmartEnum values for {property.DeclaringType!.Name}.{property.Name}.");

            var actualValues = GetSchemaEnumValues(property, schemaProperty)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            actualValues.ShouldBe(expectedValues, $"Schema enum values should match SmartEnum '{property.Name}'.");
        }
    }

    [Fact(DisplayName = "Process should leave non-annotated string properties unconstrained")]
    public void Process_ShouldLeavePropertyUnconstrained_WhenPropertyHasNoOpenApiSmartEnumAttribute()
    {
        // Arrange
        var schema = GenerateSchemaFor(typeof(Neba.Api.Contracts.Sponsors.SponsorDetailResponse));

        // Act
        var hasProperty = schema.Properties.TryGetValue("name", out JsonSchemaProperty? schemaProperty);

        // Assert
        hasProperty.ShouldBeTrue();
        schemaProperty.ShouldNotBeNull();
        schemaProperty.Enumeration.ShouldBeEmpty();
    }

    private static JsonSchema GenerateSchemaFor(Type type)
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        settings.SchemaProcessors.Add(new SmartEnumSchemaProcessor());

        return JsonSchema.FromType(type, settings);
    }

    private static IEnumerable<string> GetSchemaEnumValues(PropertyInfo property, JsonSchemaProperty schemaProperty)
    {
        if (IsStringCollection(property.PropertyType))
        {
            schemaProperty.Item.ShouldNotBeNull($"Array schema item should be present for {property.Name}.");
            return schemaProperty.Item!.Enumeration.OfType<string>();
        }

        return schemaProperty.Enumeration.OfType<string>();
    }

    private static bool IsStringCollection(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            return type.GetElementType() == typeof(string);
        }

        var enumerableArgument = type
            .GetInterfaces()
            .Append(type)
            .Where(interfaceType => interfaceType.IsGenericType)
            .Where(interfaceType => interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(interfaceType => interfaceType.GetGenericArguments()[0])
            .FirstOrDefault();

        return enumerableArgument == typeof(string);
    }

    private static IEnumerable<string> ResolveSmartEnumNames(string smartEnumTypeName)
    {
        var enumType = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Neba.Domain", StringComparison.Ordinal) == true)
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == smartEnumTypeName);

        enumType.ShouldNotBeNull($"Could not resolve SmartEnum type '{smartEnumTypeName}'.");

        var listProperty = enumType!.GetProperty("List", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        listProperty.ShouldNotBeNull($"SmartEnum type '{smartEnumTypeName}' should expose a static List property.");

        var values = listProperty!.GetValue(null) as System.Collections.IEnumerable;
        values.ShouldNotBeNull($"SmartEnum type '{smartEnumTypeName}' returned null List value.");

        return [.. values!
            .Cast<object>()
            .Select(value => value.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) as string)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)];
    }

    private static string GetJsonPropertyName(PropertyInfo property)
        => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
           ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
}