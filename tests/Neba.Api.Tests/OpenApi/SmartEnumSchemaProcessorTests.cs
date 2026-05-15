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
        // Arrange
        var setEnumeration = typeof(SmartEnumSchemaProcessor)
            .GetMethod("SetEnumeration", BindingFlags.NonPublic | BindingFlags.Static);
        setEnumeration.ShouldNotBeNull();

        var schema = new JsonSchema();
        schema.Enumeration.Add("Stale");

        var expectedValues = new[] { "Premier", "Standard", "Title Sponsor" };

        // Act
        Should.NotThrow(() => setEnumeration!.Invoke(null, [schema, expectedValues]));

        // Assert — stale value was removed and exact set was applied
        schema.Enumeration.OfType<string>().ShouldBe(expectedValues);
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

    [Fact(DisplayName = "Process should populate enum values when OpenApiSmartEnum attribute is inherited from base property")]
    public void Process_ShouldPopulateEnumValues_WhenOpenApiSmartEnumAttributeIsInherited()
    {
        // Arrange
        var property = typeof(TypeWithInheritedTierValue).GetProperty(nameof(TypeWithInheritedTierValue.Tier));
        property.ShouldNotBeNull();

        // Act
        var withoutInheritance = property!.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: false).ToArray();
        var withInheritance = property.GetCustomAttributes<OpenApiSmartEnumAttribute>(inherit: true).ToArray();

        // Assert
        withoutInheritance.ShouldBeEmpty();
        withInheritance.Length.ShouldBe(1);
        withInheritance[0].SmartEnumTypeName.ShouldBe("SponsorTier");
    }


    [Fact(DisplayName = "GetJsonPropertyName should use camelCase when JsonPropertyName is whitespace")]
    public void GetJsonPropertyName_ShouldUseCamelCase_WhenJsonPropertyNameAttributeIsWhitespace()
    {
        // Arrange
        var property = typeof(TypeWithWhitespaceJsonPropertyName).GetProperty(nameof(TypeWithWhitespaceJsonPropertyName.Tiers));
        property.ShouldNotBeNull();

        var getJsonPropertyName = typeof(SmartEnumSchemaProcessor)
            .GetMethod("GetJsonPropertyName", BindingFlags.NonPublic | BindingFlags.Static);
        getJsonPropertyName.ShouldNotBeNull();

        // Act
        var jsonPropertyName = getJsonPropertyName!.Invoke(null, [property!]) as string;

        // Assert
        jsonPropertyName.ShouldBe("tiers");
    }

    [Fact(DisplayName = "Process should return when contextual type is null")]
    public void Process_ShouldReturn_WhenContextualTypeIsNull()
    {
        // Arrange
        var schema = new JsonSchema();
        schema.Properties["tier"] = new JsonSchemaProperty();

        var processor = new SmartEnumSchemaProcessor();
        var context = CreateSchemaProcessorContext(null, schema);

        // Act
        Should.NotThrow(() => processor.Process(context));

        // Assert
        schema.Properties["tier"].Enumeration.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Process should apply enum values to root schema when array schema item is null")]
    public void Process_ShouldApplyEnumValuesToRootSchema_WhenArraySchemaItemIsNull()
    {
        // Arrange
        var applyEnumValues = typeof(SmartEnumSchemaProcessor)
            .GetMethod("ApplyEnumValues", BindingFlags.NonPublic | BindingFlags.Static);
        applyEnumValues.ShouldNotBeNull();

        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            Item = null
        };
        var expectedEnumValues = new[] { "A", "B" };

        // Act
        Should.NotThrow(() => applyEnumValues!.Invoke(null, [schema, expectedEnumValues]));

        // Assert
        schema.Enumeration.OfType<string>().ShouldBe(expectedEnumValues);
        schema.Item.ShouldBeNull();
    }

    private static SchemaProcessorContext CreateSchemaProcessorContext(Type? modelType, JsonSchema schema)
    {
        var ctor = typeof(SchemaProcessorContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var args = ctor
            .GetParameters()
            .Select(parameter => CreateConstructorArgument(parameter.ParameterType, modelType, schema))
            .ToArray();

        return (SchemaProcessorContext)ctor.Invoke(args);
    }

    private static object? CreateConstructorArgument(Type parameterType, Type? modelType, JsonSchema schema)
    {
        if (parameterType == typeof(JsonSchema))
        {
            return schema;
        }

        if (parameterType == typeof(Type))
        {
            return modelType;
        }

        if (parameterType.FullName?.Contains("ContextualType", StringComparison.Ordinal) == true)
        {
            if (modelType is null)
            {
                return null;
            }

            var fromType = parameterType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name is "FromType" && method.GetParameters() is [{ ParameterType: var pt }] && pt == typeof(Type));

            if (fromType is not null)
            {
                return fromType.Invoke(null, [modelType]);
            }

            var contextualTypeCtor = parameterType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, [typeof(Type)], modifiers: null);

            return contextualTypeCtor?.Invoke([modelType]);
        }

        return parameterType.IsValueType
            ? Activator.CreateInstance(parameterType)
            : null;
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

    public abstract class TypeWithInheritedTierBase
    {
        [OpenApiSmartEnum("SponsorTier")]
        public virtual string? Tier { get; set; }
    }

    public sealed class TypeWithInheritedTierValue : TypeWithInheritedTierBase
    {
        public override string? Tier { get; set; }
    }

    private sealed record TypeWithWhitespaceJsonPropertyName
    {
        [JsonPropertyName("   ")]
        [OpenApiSmartEnum("SponsorTier")]
        public string? Tiers { get; init; }
    }

    private static string GetJsonPropertyName(PropertyInfo property)
        => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
           ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
}