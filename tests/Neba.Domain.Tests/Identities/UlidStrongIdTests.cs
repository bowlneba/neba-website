using System.ComponentModel;
using System.Text.Json;

using StronglyTypedIds;

namespace Neba.Domain.Tests.Identities;

public sealed partial class UlidStrongIdTests
{
    [StronglyTypedId("ulid-full")]
    private readonly partial struct TestId;

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldParseCorrectly_WhenValidUlidString()
    {
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenNullString()
    {
#nullable disable
        const string value = null;

        Should.Throw<ArgumentNullException>(() => new TestId(value));
#nullable enable
    }

    [Fact]
    public void Constructor_ShouldReturnEmptyUlid_WhenEmptyString()
    {
        var id = new TestId(string.Empty);

        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenInvalidString()
    {
        Should.Throw<ArgumentException>(() => new TestId("not-a-valid-ulid"));
    }

    #endregion

    #region Static Factory Tests

    [Fact]
    public void New_ShouldCreateUniqueIds()
    {
        var id1 = TestId.New();
        var id2 = TestId.New();

        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Ulid.Empty);
    }

    [Fact]
    public void FromGuid_ShouldCreateIdFromGuid()
    {
        var guid = Guid.NewGuid();
        var id = TestId.FromGuid(guid);

        id.Value.ToGuid().ShouldBe(guid);
    }

    [Fact]
    public void Empty_ShouldReturnEmptyUlid()
    {
        TestId.Empty.Value.ShouldBe(Ulid.Empty);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameValue()
    {
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentValue()
    {
        var id1 = TestId.New();
        var id2 = TestId.New();

        id1.Equals(id2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBoxedSameValue()
    {
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        object id2 = new TestId(ulid.ToString());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenNull()
    {
        var id = TestId.New();

        id.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        var id = TestId.New();

        id.Equals("not an id").ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHash_WhenSameValue()
    {
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        id1.GetHashCode().ShouldBe(id2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenSameValue()
    {
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        (id1 == id2).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrue_WhenDifferentValue()
    {
        var id1 = TestId.New();
        var id2 = TestId.New();

        (id1 != id2).ShouldBeTrue();
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenSameValue()
    {
        var ulid = Ulid.NewUlid();
        var id1 = new TestId(ulid.ToString());
        var id2 = new TestId(ulid.ToString());

        id1.CompareTo(id2).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_ShouldOrderCorrectly_WhenDifferentTimestamps()
    {
        // Use explicit ULIDs with known ordering (earlier timestamp < later timestamp)
        var earlier = new TestId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var later = new TestId("01BX5ZZKBKACTAV9WEVGEMMVRY");

        earlier.CompareTo(later).ShouldBeLessThan(0);
        later.CompareTo(earlier).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        // Use explicit ULIDs with known ordering
        var earlier = new TestId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var later = new TestId("01BX5ZZKBKACTAV9WEVGEMMVRY");

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

    [Fact]
    public void Parse_ShouldParseCorrectly_WhenValidString()
    {
        var ulid = Ulid.NewUlid();
        var id = TestId.Parse(ulid.ToString(), provider: null);

        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void Parse_ShouldReturnEmpty_WhenEmptyString()
    {
        var id = TestId.Parse(string.Empty, provider: null);

        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenStringIsInvalid()
    {
        Should.Throw<ArgumentException>(() => TestId.Parse("invalid", provider: null));
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_WhenValidString()
    {
        var ulid = Ulid.NewUlid();

        var result = TestId.TryParse(ulid.ToString(), provider: null, out var id);

        result.ShouldBeTrue();
        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenNull()
    {
        var result = TestId.TryParse(null, provider: null, out var id);

        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    [Fact]
    public void TryParse_ShouldReturnTrueWithEmptyValue_WhenEmptyString()
    {
        var result = TestId.TryParse(string.Empty, provider: null, out var id);

        result.ShouldBeTrue();
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenInvalidString()
    {
        var result = TestId.TryParse("invalid", provider: null, out var id);

        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    [Fact]
    public void Parse_ShouldParseCorrectly_WhenValidSpan()
    {
        var ulid = Ulid.NewUlid();
        var span = ulid.ToString().AsSpan();

        var id = TestId.Parse(span, provider: null);

        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void Parse_ShouldReturnEmpty_WhenEmptySpan()
    {
        var id = TestId.Parse([], provider: null);

        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenSpanIsInvalid()
    {
        Should.Throw<ArgumentException>(() => TestId.Parse("invalid".AsSpan(), provider: null));
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_WhenValidSpan()
    {
        var ulid = Ulid.NewUlid();
        var span = ulid.ToString().AsSpan();

        var result = TestId.TryParse(span, provider: null, out var id);

        result.ShouldBeTrue();
        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void TryParse_ShouldReturnTrueWithEmptyValue_WhenEmptySpan()
    {
        var result = TestId.TryParse(ReadOnlySpan<char>.Empty, provider: null, out var id);

        result.ShouldBeTrue();
        id.Value.ShouldBe(Ulid.Empty);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenInvalidSpan()
    {
        var result = TestId.TryParse("invalid".AsSpan(), provider: null, out var id);

        result.ShouldBeFalse();
        id.ShouldBe(default);
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void ToString_ShouldReturnUlidString()
    {
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        id.ToString().ShouldBe(ulid.ToString());
    }

    [Fact]
    public void ToString_ShouldReturnUlidString_WhenFormatProviderIsNull()
    {
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        id.ToString(format: null, formatProvider: null).ShouldBe(ulid.ToString());
    }

    [Fact]
    public void TryFormat_ShouldSucceed_WhenBufferIsSufficient()
    {
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());
        Span<char> buffer = stackalloc char[26]; // ULID is 26 characters

        var result = id.TryFormat(buffer, out var charsWritten, format: default, provider: null);

        result.ShouldBeTrue();
        charsWritten.ShouldBe(26);
        buffer.ToString().ShouldBe(ulid.ToString());
    }

    [Fact]
    public void TryFormat_ShouldFail_WhenBufferIsInsufficient()
    {
        var id = TestId.New();
        Span<char> buffer = stackalloc char[10]; // Too small

        var result = id.TryFormat(buffer, out _, format: default, provider: null);

        result.ShouldBeFalse();
    }

    #endregion

    #region TypeConverter Tests

    [Fact]
    public void TypeConverter_ShouldBeRegistered()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.ShouldBeOfType<TestId.TestIdTypeConverter>();
    }

    [Fact]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenUlid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertFrom(typeof(Ulid)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterCanConvertFrom_ShouldReturnTrue_WhenGuid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertFrom(typeof(Guid)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterConvertFrom_ShouldConvert_WhenString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();

        var result = (TestId)converter.ConvertFrom(ulid.ToString())!;

        result.Value.ShouldBe(ulid);
    }

    [Fact]
    public void TypeConverterConvertFrom_ShouldConvert_WhenUlid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();

        var result = (TestId)converter.ConvertFrom(ulid)!;

        result.Value.ShouldBe(ulid);
    }

    [Fact]
    public void TypeConverterConvertFrom_ShouldConvert_WhenGuid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var guid = Guid.NewGuid();

        var result = (TestId)converter.ConvertFrom(guid)!;

        result.Value.ToGuid().ShouldBe(guid);
    }

    [Fact]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertTo(typeof(string)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenUlid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertTo(typeof(Ulid)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterCanConvertTo_ShouldReturnTrue_WhenGuid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));

        converter.CanConvertTo(typeof(Guid)).ShouldBeTrue();
    }

    [Fact]
    public void TypeConverterConvertTo_ShouldConvert_WhenString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        var result = converter.ConvertTo(id, typeof(string));

        result.ShouldBe(ulid.ToString());
    }

    [Fact]
    public void TypeConverterConvertTo_ShouldConvert_WhenUlid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        var result = converter.ConvertTo(id, typeof(Ulid));

        result.ShouldBe(ulid);
    }

    [Fact]
    public void TypeConverterConvertTo_ShouldConvert_WhenGuid()
    {
        var converter = TypeDescriptor.GetConverter(typeof(TestId));
        var guid = Guid.NewGuid();
        var id = TestId.FromGuid(guid);

        var result = converter.ConvertTo(id, typeof(Guid));

        result.ShouldBe(guid);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void JsonSerialize_ShouldSerializeAsString()
    {
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        var json = JsonSerializer.Serialize(id);

        json.ShouldBe($"\"{ulid}\"");
    }

    [Fact]
    public void JsonDeserialize_ShouldDeserializeFromString()
    {
        var ulid = Ulid.NewUlid();
        var json = $"\"{ulid}\"";

        var id = JsonSerializer.Deserialize<TestId>(json);

        id.Value.ShouldBe(ulid);
    }

    [Fact]
    public void JsonSerialize_ShouldRoundTrip_WhenDictionaryKey()
    {
        var id = TestId.New();
        var dict = new Dictionary<TestId, string> { [id] = "test" };

        var json = JsonSerializer.Serialize(dict);
        var deserialized = JsonSerializer.Deserialize<Dictionary<TestId, string>>(json);

        deserialized.ShouldNotBeNull();
        deserialized.ShouldContainKey(id);
        deserialized[id].ShouldBe("test");
    }

    [Fact]
    public void JsonSerialize_ShouldRoundTrip_WhenInObject()
    {
        var id = TestId.New();
        var obj = new TestContainer { Id = id, Name = "Test" };

        var json = JsonSerializer.Serialize(obj);
        var deserialized = JsonSerializer.Deserialize<TestContainer>(json);

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

    #region EF Core ValueConverter Tests

    [Fact]
    public void EfCoreValueConverter_ShouldConvertToString()
    {
        var converter = new TestId.EfCoreValueConverter();
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());

        var result = converter.ConvertToProvider(id);

        result.ShouldBe(ulid.ToString());
    }

    [Fact]
    public void EfCoreValueConverter_ShouldConvertFromString()
    {
        var converter = new TestId.EfCoreValueConverter();
        var ulid = Ulid.NewUlid();

        var result = converter.ConvertFromProvider(ulid.ToString());

        result.ShouldBeOfType<TestId>();
        ((TestId)result!).Value.ShouldBe(ulid);
    }

    #endregion
}
