using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Neba.Api.Encryption;

/// <summary>
/// The stored and wire format (see docs/adr/0011-envelope-encryption-for-sensitive-values.md).
/// Base64 of: format version (1), key version length (1), key version, wrapped key length
/// (2, big-endian), wrapped key, nonce (12), tag (16), ciphertext. Keep field-encryption.js in step.
/// </summary>
internal sealed record Envelope(string KeyVersion, byte[] WrappedKey, byte[] Nonce, byte[] Tag, byte[] Ciphertext)
{
    private const byte FormatVersion = 1;

    public const int AesKeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static byte[] AdditionalData(string keyName, EncryptionPurpose purpose, string keyVersion)
    {
        var purposeText = purpose == EncryptionPurpose.Storage ? "storage" : "wire";

        return Encoding.UTF8.GetBytes($"v1|{keyName}|{purposeText}|{keyVersion}");
    }

    public string Encode()
    {
        var version = Encoding.UTF8.GetBytes(KeyVersion);
        var bytes = new byte[2 + version.Length + 2 + WrappedKey.Length + NonceSize + TagSize + Ciphertext.Length];
        var span = bytes.AsSpan();

        span[0] = FormatVersion;
        span[1] = checked((byte)version.Length);
        version.CopyTo(span[2..]);
        span = span[(2 + version.Length)..];

        BinaryPrimitives.WriteUInt16BigEndian(span, checked((ushort)WrappedKey.Length));
        WrappedKey.CopyTo(span[2..]);
        span = span[(2 + WrappedKey.Length)..];

        Nonce.CopyTo(span);
        Tag.CopyTo(span[NonceSize..]);
        Ciphertext.CopyTo(span[(NonceSize + TagSize)..]);

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Reads a value in this format.
    /// </summary>
    /// <exception cref="FormatException">The value is not in this format.</exception>
    public static Envelope Decode(string value)
    {
        ReadOnlySpan<byte> span = Convert.FromBase64String(value);

        if (Take(ref span, 1)[0] != FormatVersion)
        {
            throw new FormatException("Unknown encrypted value format version.");
        }

        string keyVersion;
        try
        {
            keyVersion = StrictUtf8.GetString(Take(ref span, Take(ref span, 1)[0]));
        }
        catch (DecoderFallbackException ex)
        {
            throw new FormatException("The key version is not valid UTF-8.", ex);
        }

        var wrappedKey = Take(ref span, BinaryPrimitives.ReadUInt16BigEndian(Take(ref span, 2))).ToArray();
        var nonce = Take(ref span, NonceSize).ToArray();
        var tag = Take(ref span, TagSize).ToArray();

        return new Envelope(keyVersion, wrappedKey, nonce, tag, span.ToArray());
    }

    private static ReadOnlySpan<byte> Take(ref ReadOnlySpan<byte> source, int count)
    {
        if (source.Length < count)
        {
            throw new FormatException("The encrypted value is too short.");
        }

        var taken = source[..count];
        source = source[count..];

        return taken;
    }
}

/// <summary>
/// Where an encrypted value is used. A value made for one purpose does not decrypt as the other.
/// </summary>
internal enum EncryptionPurpose
{
    /// <summary>Kept in the database.</summary>
    Storage,

    /// <summary>Sent by a browser.</summary>
    Wire
}
