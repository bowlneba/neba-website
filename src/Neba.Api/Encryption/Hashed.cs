using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neba.Api.Encryption;

/// <summary>
/// A keyed hash used to find duplicates. It must never leave the API.
/// </summary>
[JsonConverter(typeof(HashedJsonConverterFactory))]
// TKey is unused in the body on purpose: it keeps one key's hash from being compared with another's.
#pragma warning disable S2326 // Unused type parameters should be removed
internal sealed record Hashed<TKey> where TKey : IEncryptionKey
#pragma warning restore S2326
{
    public Hashed(string value) => Value = value;

    // Internal, not public, so serializers that ignore the converter above (Newtonsoft, used by
    // Hangfire) and logging that reads public properties do not see the hash.
    internal string Value { get; }

    public override string ToString() => "[REDACTED]";
}

internal sealed class HashedJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Hashed<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(ThrowingConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class ThrowingConverter<T> : JsonConverter<T>
    {
        private const string Message = "A hash must never be serialized.";

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException(Message);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => throw new NotSupportedException(Message);
    }
}
