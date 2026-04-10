using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Neba.Api.Contracts.OpenApi;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.OpenApi;
using Neba.Domain.Sponsors;
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
            .Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
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

    [Fact(DisplayName = "Process should not throw and produce empty schema when type has no public properties")]
    public void Process_ShouldProduceEmptySchema_WhenTypeHasNoPublicProperties()
    {
        // Arrange & Act
        var schema = GenerateSchemaFor(typeof(TypeWithNoPublicProperties));

        // Assert — no exception thrown, schema has no properties
        schema.Properties.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Process should not duplicate enum values when processor runs twice on same schema")]
    public void Process_ShouldNotDuplicateEnumValues_WhenProcessorRunsMultipleTimes()
    {
        // Arrange — generate a fresh schema and pre-populate a property's enumeration manually
        var schema = GenerateSchemaFor(typeof(SponsorDetailResponse));
        var tierProperty = schema.Properties["tier"];
        var expectedCount = tierProperty.Enumeration.Count;
        expectedCount.ShouldBeGreaterThan(0);

        // Act — manually add a duplicate to simulate stale data, then re-generate
        tierProperty.Enumeration.Add("Stale");

        // Re-generate on a fresh schema to simulate a second processor run
        var freshSchema = GenerateSchemaFor(typeof(SponsorDetailResponse));
        var freshCount = freshSchema.Properties["tier"].Enumeration.Count;

        // Assert — fresh schema has exact expected count (Clear was called, not doubled)
        freshCount.ShouldBe(expectedCount);
    }

    [Fact(DisplayName = "Process should populate item enumeration for array-typed SmartEnum properties")]
    public void Process_ShouldPopulateItemEnumeration_WhenPropertyIsArrayOfSmartEnum()
    {
        // Arrange
        var schema = GenerateSchemaFor(typeof(TypeWithArraySmartEnum));

        // Act
        var hasProperty = schema.Properties.TryGetValue("tiers", out JsonSchemaProperty? schemaProperty);

        // Assert
        hasProperty.ShouldBeTrue();
        schemaProperty.ShouldNotBeNull();
        schemaProperty!.Enumeration.ShouldBeEmpty("enum values should be on the array item schema, not the array schema itself");
        schemaProperty.Item.ShouldNotBeNull();
        schemaProperty.Item!.Enumeration.ShouldNotBeEmpty();
        schemaProperty.Item.Enumeration.OfType<string>().ShouldContain(SponsorTier.Premier.Name);
    }

    [Fact(DisplayName = "Process should use explicit JsonPropertyName when attribute is present")]
    public void Process_ShouldUseExplicitJsonPropertyName_WhenJsonPropertyNameAttributeIsPresent()
    {
        // Arrange
        var schema = GenerateSchemaFor(typeof(TypeWithExplicitJsonName));

        // Act
        var hasCustomName = schema.Properties.TryGetValue("custom-tiers", out JsonSchemaProperty? customProperty);
        var hasCamelCase = schema.Properties.TryGetValue("tiers", out _);

        // Assert
        hasCustomName.ShouldBeTrue("should use the explicit JsonPropertyName, not camelCase");
        hasCamelCase.ShouldBeFalse("camelCase name should not be present when explicit name is set");
        customProperty!.Enumeration.ShouldNotBeEmpty();
    }

#pragma warning disable S2094 // Intentionally empty — used to test schema processor early-exit on types with no properties
    private sealed class TypeWithNoPublicProperties;
#pragma warning restore S2094

    private sealed record TypeWithArraySmartEnum
    {
        [OpenApiSmartEnum("SponsorTier")]
        public IReadOnlyCollection<string>? Tiers { get; init; }
    }

    private sealed record TypeWithExplicitJsonName
    {
        [JsonPropertyName("custom-tiers")]
        [OpenApiSmartEnum("SponsorTier")]
        public string? Tiers { get; init; }
    }

    private static string GetJsonPropertyName(PropertyInfo property)
        => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
           ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
}