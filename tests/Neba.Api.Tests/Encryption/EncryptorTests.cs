using System.Security.Cryptography;

using ErrorOr;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class EncryptorTests : IDisposable
{
    private readonly VersionedKeyWrapper _wrapper = new();

    public void Dispose() => _wrapper.Dispose();

    private Encryptor<BowlerPii> CreateSut() => new(_wrapper, NullLogger<Encryptor<BowlerPii>>.Instance);

    // Fake SSNs start with 9, which is never issued as one.
    private const string FakeSsn = "900123456";

    [Fact(DisplayName = "A value should survive encrypt then decrypt")]
    public async Task DecryptAsync_ShouldReturnPlaintext_WhenValueWasEncrypted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var encrypted = await sut.EncryptAsync(FakeSsn, ct);

        // Act
        var result = await sut.DecryptAsync(encrypted, ct);

        // Assert
        result.ShouldBe(FakeSsn);
        encrypted.Value.ShouldNotContain(FakeSsn);
    }

    [Fact(DisplayName = "The same input should give different output each time")]
    public async Task EncryptAsync_ShouldReturnDifferentValues_WhenInputIsTheSame()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        // Act
        var first = await sut.EncryptAsync(FakeSsn, ct);
        var second = await sut.EncryptAsync(FakeSsn, ct);

        // Assert
        first.Value.ShouldNotBe(second.Value);
    }

    // Offsets for a two-byte key version ("v1") and an RSA 2048 wrapped key.
    [Theory(DisplayName = "A change to any field should make decryption fail")]
    [InlineData(0, TestDisplayName = "Format version")]
    [InlineData(1, TestDisplayName = "Key version length")]
    [InlineData(2, TestDisplayName = "Key version")]
    [InlineData(4, TestDisplayName = "Wrapped key length")]
    [InlineData(6, TestDisplayName = "Wrapped key")]
    [InlineData(262, TestDisplayName = "Nonce")]
    [InlineData(274, TestDisplayName = "Tag")]
    [InlineData(290, TestDisplayName = "Ciphertext")]
    public async Task DecryptAsync_ShouldThrow_WhenAFieldIsChanged(int offset)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var bytes = Convert.FromBase64String((await sut.EncryptAsync(FakeSsn, ct)).Value);
        bytes.Length.ShouldBe(299); // 290 bytes of header, then 9 digits
        bytes[offset] ^= 0x01;
        var tampered = new Encrypted<BowlerPii>(Convert.ToBase64String(bytes));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.DecryptAsync(tampered, ct));

        // Assert
        exception.ShouldNotBeNull();
        (exception is FormatException or CryptographicException).ShouldBeTrue(exception.ToString());
    }

    [Theory(DisplayName = "A stored value that is not in the format should throw")]
    [InlineData("not base64!", TestDisplayName = "Not Base64")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("AQ==", TestDisplayName = "Version byte only")]
    [InlineData("AQL//wAA", TestDisplayName = "Key version is not UTF-8")]
    public async Task DecryptAsync_ShouldThrowFormatException_WhenValueIsMalformed(string value)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.DecryptAsync(new Encrypted<BowlerPii>(value), TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<FormatException>();
    }

    [Fact(DisplayName = "Another key should not decrypt the value")]
    public async Task DecryptAsync_ShouldThrow_WhenAnotherKeyPairIsUsed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var encrypted = await CreateSut().EncryptAsync(FakeSsn, ct);
        using var otherWrapper = new VersionedKeyWrapper();
        var other = new Encryptor<BowlerPii>(otherWrapper, NullLogger<Encryptor<BowlerPii>>.Instance);

        // Act
        var act = () => other.DecryptAsync(encrypted, ct);

        // Assert
        await act.ShouldThrowAsync<CryptographicException>();
    }

    [Fact(DisplayName = "A value should not decrypt under another key name, even with the same RSA key")]
    public async Task DecryptAsync_ShouldThrow_WhenKeyNameDiffers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var encrypted = await CreateSut().EncryptAsync(FakeSsn, ct);
        var other = new Encryptor<OtherPii>(_wrapper, NullLogger<Encryptor<OtherPii>>.Instance);

        // Act
        var act = () => other.DecryptAsync(new Encrypted<OtherPii>(encrypted.Value), ct);

        // Assert
        await act.ShouldThrowAsync<AuthenticationTagMismatchException>();
    }

    [Fact(DisplayName = "A wire value should decrypt as client input")]
    public async Task DecryptFromClientAsync_ShouldReturnPlaintext_WhenValueIsAWireValue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var wire = await sut.SealAsync(FakeSsn, EncryptionPurpose.Wire, ct);

        // Act
        var result = await sut.DecryptFromClientAsync(wire, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(FakeSsn);
    }

    [Fact(DisplayName = "A wire value should fail as a stored value")]
    public async Task DecryptAsync_ShouldThrow_WhenValueIsAWireValue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var wire = await sut.SealAsync(FakeSsn, EncryptionPurpose.Wire, ct);

        // Act
        var act = () => sut.DecryptAsync(new Encrypted<BowlerPii>(wire), ct);

        // Assert
        await act.ShouldThrowAsync<AuthenticationTagMismatchException>();
    }

    [Fact(DisplayName = "A stored value should fail as client input, with the general error and a warning")]
    public async Task DecryptFromClientAsync_ShouldReturnGeneralError_WhenValueIsAStoredValue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logger = new FakeLogger<Encryptor<BowlerPii>>();
        var sut = new Encryptor<BowlerPii>(_wrapper, logger);
        var stored = await sut.EncryptAsync(FakeSsn, ct);

        // Act
        var result = await sut.DecryptFromClientAsync(stored.Value, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(EncryptionErrors.InvalidClientValue);
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Exception.ShouldBeOfType<AuthenticationTagMismatchException>();
        record.Message.ShouldNotContain(FakeSsn);
        record.Message.ShouldNotContain(stored.Value);
    }

    [Theory(DisplayName = "Bad client input should give the general error")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("900123456", TestDisplayName = "Plaintext digits")]
    [InlineData("not base64!", TestDisplayName = "Not Base64")]
    [InlineData("AgJ2MQ==", TestDisplayName = "Unknown format version")]
    public async Task DecryptFromClientAsync_ShouldReturnGeneralError_WhenValueIsMalformed(string value)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DecryptFromClientAsync(value, TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.ShouldBe(EncryptionErrors.InvalidClientValue);
    }

    [Fact(DisplayName = "A wrapped key of the wrong size should be rejected")]
    public async Task DecryptFromClientAsync_ShouldReturnGeneralError_WhenAesKeyIsNot256Bits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var shortKey = RandomNumberGenerator.GetBytes(16);
        var wrapped = await _wrapper.WrapAsync(shortKey, ct);
        var nonce = RandomNumberGenerator.GetBytes(Envelope.NonceSize);
        var tag = new byte[Envelope.TagSize];
        var ciphertext = new byte[9];
        using var aes = new AesGcm(shortKey, Envelope.TagSize);
        aes.Encrypt(nonce, "900123456"u8, ciphertext, tag,
            Envelope.AdditionalData(BowlerPii.Name, EncryptionPurpose.Wire, wrapped.KeyVersion));
        var wire = new Envelope(wrapped.KeyVersion, wrapped.Value, nonce, tag, ciphertext).Encode();

        // Act
        var result = await CreateSut().DecryptFromClientAsync(wire, ct);

        // Assert
        result.FirstError.ShouldBe(EncryptionErrors.InvalidClientValue);
    }

    [Fact(DisplayName = "A value from an older key version should still decrypt")]
    public async Task DecryptAsync_ShouldReturnPlaintext_WhenKeyHasRotated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var old = await sut.EncryptAsync(FakeSsn, ct);
        _wrapper.Rotate("v2");
        var current = await sut.EncryptAsync(FakeSsn, ct);

        // Act
        var fromOld = await sut.DecryptAsync(old, ct);
        var fromCurrent = await sut.DecryptAsync(current, ct);

        // Assert
        fromOld.ShouldBe(FakeSsn);
        fromCurrent.ShouldBe(FakeSsn);
        Envelope.Decode(old.Value).KeyVersion.ShouldBe("v1");
        Envelope.Decode(current.Value).KeyVersion.ShouldBe("v2");
    }
}