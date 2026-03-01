using System.ComponentModel;
using System.Text.Json;

using Neba.TestFactory.Attributes;

using StronglyTypedIds;

namespace Neba.Domain.Tests.Identities;

[UnitTest]
[Component("Identities")]
public sealed partial class UlidStrongIdTests
{
    [StronglyTypedId("ulid-full")]
    private readonly partial struct TestId;

    #region Constructor Tests

    [Fact(DisplayName = "Should parse correctly when valid ULID string")]
    public void Constructor_ShouldParseCorrectly_WhenValidUlidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();

        // Act
        var id = new TestId(ulid.ToString());

        // Assert
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should throw ArgumentNullException when null string")]
    public void Constructor_ShouldThrowArgumentNullException_WhenNullString()
    {
#nullable disable
        // Arrange
        const string value = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestId(value));
#nullable enable
    }

    [Fact(DisplayName = "Should return empty ULID when empty string")]
    public void Constructor_ShouldReturnEmptyUlid_WhenEmptyString()
    {
        // Act
        var id = new TestId(string.Empty);

        // Assert
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should throw ArgumentException when invalid string")]
    public void Constructor_ShouldThrowArgumentException_WhenInvalidString()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new TestId("not-a-valid-ulid"));
    }

    #endregion

    #region Static Factory Tests

    [Fact(DisplayName = "Should create unique IDs when called multiple times")]
    public void New_ShouldCreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Act
        var id1 = TestId.New();
        var id2 = TestId.New();

        // Assert
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should create ID from GUID")]
    public void FromGuid_ShouldCreateId_WhenGuidIsValid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = TestId.FromGuid(guid);

        // Assert
        id.Value.ToGuid().ShouldBe(guid);
    }

    [Fact(DisplayName = "Should return empty ULID when accessed")]
    public void Empty_ShouldReturnEmptyUlid_WhenAccessed()
    {
        // Act
        var result = TestId.Empty.Value;

        // Assert
        result.ShouldBe(Ulid.Empty);
    }

    #endregion

    #region Equality Tests

    [Fact(DisplayName = "Should return true when same value")]
    public void Equals_ShouldReturnTrue_WhenSameValue()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        // Act
        var result = id1.Equals(id2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return false when different value")]
    public void Equals_ShouldReturnFalse_WhenDifferentValue()
    {
        // Arrange
        var id1 = TestId.New();
        var id2 = TestId.New();

        // Act
        var result = id1.Equals(id2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should return true when boxed same value")]
    public void Equals_ShouldReturnTrue_WhenBoxedSameValue()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        object id2 = new TestId(ulid.ToString());

        // Act
        var result = id1.Equals(id2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return false when null")]
    public void Equals_ShouldReturnFalse_WhenNull()
    {
        // Arrange
        var id = TestId.New();

        // Act
        var result = id.Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should return false when different type")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        // Arrange
        var id = TestId.New();

        // Act
        var result = id.Equals("not an id");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should return same hash when same value")]
    public void GetHashCode_ShouldReturnSameHash_WhenSameValue()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        // Act & Assert
        id1.GetHashCode().ShouldBe(id2.GetHashCode());
    }

    [Fact(DisplayName = "Equality operator should return true when same value")]
    public void EqualityOperator_ShouldReturnTrue_WhenSameValue()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        // Act & Assert
        (id1 == id2).ShouldBeTrue();
    }

    [Fact(DisplayName = "Inequality operator should return true when different value")]
    public void InequalityOperator_ShouldReturnTrue_WhenDifferentValue()
    {
        // Arrange
        var id1 = TestId.New();
        var id2 = TestId.New();

        // Act & Assert
        (id1 != id2).ShouldBeTrue();
    }

    #endregion

    #region Comparison Tests

    [Fact(DisplayName = "Should return zero when same value")]
    public void CompareTo_ShouldReturnZero_WhenSameValue()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        // Act
        var result = id1.CompareTo(id2);

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "Should order correctly when different timestamps")]
    public void CompareTo_ShouldOrderCorrectly_WhenDifferentTimestamps()
    {
        // Arrange — explicit ULIDs with known ordering (earlier timestamp < later timestamp)
        var earlier = new TestId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var later = new TestId("01BX5ZZKBKACTAV9WEVGEMMVRY");

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0);
        later.CompareTo(earlier).ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Comparison operators should respect chronological order when ULIDs have different timestamps")]
    public void ComparisonOperators_ShouldRespectChronologicalOrder_WhenUlidsHaveDifferentTimestamps()
    {
        // Arrange — explicit ULIDs with known ordering
        var earlier = new TestId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var later = new TestId("01BX5ZZKBKACTAV9WEVGEMMVRY");

        // Act & Assert
        (earlier < later).ShouldBeTrue();
        (later > earlier).ShouldBeTrue();
        (earlier <= later).ShouldBeTrue();
        (later >= earlier).ShouldBeTrue();

        var ulid = Ulid.NewUlid();
        var same1 = new TestId(ulid.ToString());
        var same2 = new TestId(ulid.ToString());

        (same1 <= same2).ShouldBeTrue();
        (same1 >= same2).ShouldBeTrue();
    }

    #endregion

    #region Parsing Tests

    [Fact(DisplayName = "Should parse correctly when valid string")]
    public void Parse_ShouldParseCorrectly_WhenValidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();

        // Act
        var id = TestId.Parse(ulid.ToString(), provider: null);

        // Assert
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should return empty when empty string")]
    public void Parse_ShouldReturnEmpty_WhenEmptyString()
    {
        // Act
        var id = TestId.Parse(string.Empty, provider: null);

        // Assert
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should throw when string is invalid")]
    public void Parse_ShouldThrow_WhenStringIsInvalid()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => TestId.Parse("invalid", provider: null));
    }

    [Fact(DisplayName = "Should return true when valid string")]
    public void TryParse_ShouldReturnTrue_WhenValidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();

        // Act
        var result = TestId.TryParse(ulid.ToString(), provider: null, out var id);

        // Assert
        result.ShouldBeTrue();
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should return false when null")]
    public void TryParse_ShouldReturnFalse_WhenNull()
    {
        // Act
        var result = TestId.TryParse(null, provider: null, out var id);

        // Assert
        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    [Fact(DisplayName = "Should return true with empty value when empty string")]
    public void TryParse_ShouldReturnTrueWithEmptyValue_WhenEmptyString()
    {
        // Act
        var result = TestId.TryParse(string.Empty, provider: null, out var id);

        // Assert
        result.ShouldBeTrue();
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should return false when invalid string")]
    public void TryParse_ShouldReturnFalse_WhenInvalidString()
    {
        // Act
        var result = TestId.TryParse("invalid", provider: null, out var id);

        // Assert
        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    [Fact(DisplayName = "Should parse correctly when valid span")]
    public void Parse_ShouldParseCorrectly_WhenValidSpan()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var span = ulid.ToString().AsSpan();

        // Act
        var id = TestId.Parse(span, provider: null);

        // Assert
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should return empty when empty span")]
    public void Parse_ShouldReturnEmpty_WhenEmptySpan()
    {
        // Act
        var id = TestId.Parse([], provider: null);

        // Assert
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should throw when span is invalid")]
    public void Parse_ShouldThrow_WhenSpanIsInvalid()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => TestId.Parse("invalid".AsSpan(), provider: null));
    }

    [Fact(DisplayName = "Should return true when valid span")]
    public void TryParse_ShouldReturnTrue_WhenValidSpan()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var span = ulid.ToString().AsSpan();

        // Act
        var result = TestId.TryParse(span, provider: null, out var id);

        // Assert
        result.ShouldBeTrue();
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should return true with empty value when empty span")]
    public void TryParse_ShouldReturnTrueWithEmptyValue_WhenEmptySpan()
    {
        // Act
        var result = TestId.TryParse([], provider: null, out var id);

        // Assert
        result.ShouldBeTrue();
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact(DisplayName = "Should return false when invalid span")]
    public void TryParse_ShouldReturnFalse_WhenInvalidSpan()
    {
        // Act
        var result = TestId.TryParse("invalid".AsSpan(), provider: null, out var id);

        // Assert
        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    #endregion

    #region Formatting Tests

    [Fact(DisplayName = "Should return ULID string when id is valid")]
    public void ToString_ShouldReturnUlidString_WhenIdIsValid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        // Act
        var result = id.ToString();

        // Assert
        result.ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Should return ULID string when format provider is null")]
    public void ToString_ShouldReturnUlidString_WhenFormatProviderIsNull()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        // Act
        var result = id.ToString(format: null, formatProvider: null);

        // Assert
        result.ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Should succeed when buffer is sufficient")]
    public void TryFormat_ShouldSucceed_WhenBufferIsSufficient()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());
        Span<char> buffer = stackalloc char[26]; // ULID is 26 characters

        // Act
        var result = id.TryFormat(buffer, out var charsWritten, format: default, provider: null);

        // Assert
        result.ShouldBeTrue();
        charsWritten.ShouldBe(26);
        buffer.ToString().ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Should fail when buffer is insufficient")]
    public void TryFormat_ShouldFail_WhenBufferIsInsufficient()
    {
        // Arrange
        var id = TestId.New();
        Span<char> buffer = stackalloc char[10]; // Too small

        // Act
        var result = id.TryFormat(buffer, out _, format: default, provider: null);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TypeConverter Tests

    [Fact(DisplayName = "Type converter should be registered for TestId")]
    public void TypeConverter_ShouldBeRegistered_ForTestId()
    {
        // Act
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Assert
        converter.ShouldBeOfType<TestId.TestIdTypeConverter>();
    }

    [Fact(DisplayName = "Type converter can convert from should return true when string")]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertFrom(typeof(string));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter can convert from should return true when ULID")]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenUlid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertFrom(typeof(Ulid));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter can convert from should return true when GUID")]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenGuid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertFrom(typeof(Guid));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter convert from should convert when string")]
    public void TypeConverterConvertFrom_ShouldConvert_WhenString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();

        // Act
        var result = (TestId)converter.ConvertFrom(ulid.ToString())!;

        // Assert
        result.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Type converter convert from should convert when ULID")]
    public void TypeConverterConvertFrom_ShouldConvert_WhenUlid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();

        // Act
        var result = (TestId)converter.ConvertFrom(ulid)!;

        // Assert
        result.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Type converter convert from should convert when GUID")]
    public void TypeConverterConvertFrom_ShouldConvert_WhenGuid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var guid = Guid.NewGuid();

        // Act
        var result = (TestId)converter.ConvertFrom(guid)!;

        // Assert
        result.Value.ToGuid().ShouldBe(guid);
    }

    [Fact(DisplayName = "Type converter can convert to should return true when string")]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertTo(typeof(string));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter can convert to should return true when ULID")]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenUlid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertTo(typeof(Ulid));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter can convert to should return true when GUID")]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenGuid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        // Act
        var result = converter.CanConvertTo(typeof(Guid));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Type converter convert to should convert when string")]
    public void TypeConverterConvertTo_ShouldConvert_WhenString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        // Act
        var result = converter.ConvertTo(id, typeof(string));

        // Assert
        result.ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Type converter convert to should convert when ULID")]
    public void TypeConverterConvertTo_ShouldConvert_WhenUlid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        // Act
        var result = converter.ConvertTo(id, typeof(Ulid));

        // Assert
        result.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Type converter convert to should convert when GUID")]
    public void TypeConverterConvertTo_ShouldConvert_WhenGuid()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var guid = Guid.NewGuid();
        var id = TestId.FromGuid(guid);

        // Act
        var result = converter.ConvertTo(id, typeof(Guid));

        // Assert
        result.ShouldBe(guid);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact(DisplayName = "Should serialize as string when id is valid")]
    public void JsonSerialize_ShouldSerializeAsString_WhenIdIsValid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        // Act
        var json = JsonSerializer.Serialize(id);

        // Assert
        json.ShouldBe($"\"{ulid}\"");
    }

    [Fact(DisplayName = "Should deserialize from string when string is valid")]
    public void JsonDeserialize_ShouldRestoreValue_WhenStringIsValid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var json = $"\"{ulid}\"";

        // Act
        var id = JsonSerializer.Deserialize<TestId>(json);

        // Assert
        id.Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should round trip when dictionary key")]
    public void JsonSerialize_ShouldRoundTrip_WhenDictionaryKey()
    {
        // Arrange
        var id = TestId.New();
        var dict = new Dictionary<TestId, string> { [id] = "test" };

        // Act
        var json = JsonSerializer.Serialize(dict);
        var deserialized = JsonSerializer.Deserialize<Dictionary<TestId, string>>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.ShouldContainKey(id);
        deserialized[id].ShouldBe("test");
    }

    [Fact(DisplayName = "Should round trip when in object")]
    public void JsonSerialize_ShouldRoundTrip_WhenInObject()
    {
        // Arrange
        var id = TestId.New();
        var obj = new TestContainer { Id = id, Name = "Test" };

        // Act
        var json = JsonSerializer.Serialize(obj);
        var deserialized = JsonSerializer.Deserialize<TestContainer>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(id);
        deserialized.Name.ShouldBe("Test");
    }

    private sealed class TestContainer
    {
        public TestId Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
