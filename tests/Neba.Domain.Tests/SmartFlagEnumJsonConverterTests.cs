using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Ardalis.SmartEnum;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests;

[UnitTest]
[Component("Domain")]
public sealed class SmartFlagEnumJsonConverterTests
{
    [JsonConverter(typeof(SmartFlagEnumJsonConverter<TestSmartFlagEnum>))]
    private sealed class TestSmartFlagEnum
        : SmartFlagEnum<TestSmartFlagEnum>
    {
        public static readonly TestSmartFlagEnum None = new(0, "None");
        public static readonly TestSmartFlagEnum First = new(1, "First");
        public static readonly TestSmartFlagEnum Second = new(2, "Second");
        public static readonly TestSmartFlagEnum Third = new(4, "Third");

        private TestSmartFlagEnum(int value, string name)
            : base(name, value) { }
    }

    [Fact(DisplayName = "Serializing a SmartFlagEnum writes its integer value")]
    public void Write_ShouldWriteIntegerValue_WhenSerializingSmartFlagEnum()
    {
        // Act
        var json = JsonSerializer.Serialize(TestSmartFlagEnum.Second);

        // Assert
        json.ShouldBe("2");
    }

    [Fact(DisplayName = "Serializing None value writes zero")]
    public void Write_ShouldWriteZero_WhenSerializingNoneValue()
    {
        // Act
        var json = JsonSerializer.Serialize(TestSmartFlagEnum.None);

        // Assert
        json.ShouldBe("0");
    }

    [Fact(DisplayName = "Deserializing an integer returns the correct SmartFlagEnum value")]
    public void Read_ShouldReturnCorrectEnumValue_WhenDeserializingInteger()
    {
        // Act
        var result = JsonSerializer.Deserialize<TestSmartFlagEnum>("4");

        // Assert
        result.ShouldBe(TestSmartFlagEnum.Third);
    }

    [Fact(DisplayName = "Deserializing zero returns None value")]
    public void Read_ShouldReturnNoneValue_WhenDeserializingZero()
    {
        // Act
        var result = JsonSerializer.Deserialize<TestSmartFlagEnum>("0");

        // Assert
        result.ShouldBe(TestSmartFlagEnum.None);
    }

    [Fact(DisplayName = "Deserializing legacy object format extracts Value property")]
    public void Read_ShouldExtractValueProperty_WhenDeserializingLegacyObjectFormat()
    {
        // Act
        var result = JsonSerializer.Deserialize<TestSmartFlagEnum>("""{"Value": 2, "Name": "Second"}""");

        // Assert
        result.ShouldBe(TestSmartFlagEnum.Second);
    }

    [Fact(DisplayName = "Deserializing legacy object format with only Value property succeeds")]
    public void Read_ShouldSucceed_WhenLegacyObjectHasOnlyValueProperty()
    {
        // Act
        var result = JsonSerializer.Deserialize<TestSmartFlagEnum>("""{"Value": 1}""");

        // Assert
        result.ShouldBe(TestSmartFlagEnum.First);
    }

    [Fact(DisplayName = "Deserializing object without Value property throws JsonException")]
    public void Read_ShouldThrowJsonException_WhenObjectMissingValueProperty()
    {
        // Act
        var act = () => JsonSerializer.Deserialize<TestSmartFlagEnum>("""{"Name": "First"}""");

        // Assert
        var exception = act.ShouldThrow<JsonException>();
        exception.Message.ShouldContain("Expected 'Value' property");
    }

    [Fact(DisplayName = "Deserializing invalid token type throws JsonException")]
    public void Read_ShouldThrowJsonException_WhenTokenTypeIsInvalid()
    {
        // Act
        var act = () => JsonSerializer.Deserialize<TestSmartFlagEnum>("\"First\""); // string instead of number

        // Assert
        var exception = act.ShouldThrow<JsonException>();
        exception.Message.ShouldContain("Expected number or object token");
    }

    [Fact(DisplayName = "Serializing null value writes null JSON")]
    public void Write_ShouldWriteNull_WhenValueIsNull()
    {
        // Arrange
        TestSmartFlagEnum? value = null;

        // Act
        var json = JsonSerializer.Serialize(value);

        // Assert
        // JsonSerializer writes "null" for null reference types without invoking the converter
        json.ShouldBe("null");
    }

    [Fact(DisplayName = "Round-trip serialization preserves value")]
    public void RoundTrip_ShouldPreserveValue_WhenSerializingAndDeserializing()
    {
        // Arrange
        var original = TestSmartFlagEnum.Third;

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TestSmartFlagEnum>(json);

        // Assert
        deserialized.ShouldBe(original);
    }

    [Theory(DisplayName = "All defined values serialize and deserialize correctly")]
    [InlineData(0, "None", TestDisplayName = "None value (0)")]
    [InlineData(1, "First", TestDisplayName = "First value (1)")]
    [InlineData(2, "Second", TestDisplayName = "Second value (2)")]
    [InlineData(4, "Third", TestDisplayName = "Third value (4)")]
    public void RoundTrip_ShouldWork_ForAllDefinedValues(int expectedValue, string expectedName)
    {
        // Arrange
        var original = SmartFlagEnum<TestSmartFlagEnum>.FromValue(expectedValue).First();

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TestSmartFlagEnum>(json);

        // Assert
        json.ShouldBe(expectedValue.ToString(CultureInfo.InvariantCulture));
        deserialized.ShouldNotBeNull();
        deserialized.Value.ShouldBe(expectedValue);
        deserialized.Name.ShouldBe(expectedName);
    }

    [Fact(DisplayName = "FromValue returns all constituent flags for combined values")]
    public void FromValue_ShouldReturnAllFlags_WhenValueIsCombined()
    {
        // Arrange
        const int combinedValue = 6; // Second (2) | Third (4)

        // Act
        var flags = SmartFlagEnum<TestSmartFlagEnum>.FromValue(combinedValue).ToList();

        // Assert
        flags.Count.ShouldBe(2);
        flags.ShouldContain(TestSmartFlagEnum.Second);
        flags.ShouldContain(TestSmartFlagEnum.Third);
    }
}