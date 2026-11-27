using System.Security.Cryptography;

using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class KeyVaultKeyWrapperTests
{
    private const string CurrentVersion = "11111111111111111111111111111111";
    private const string OldVersion = "22222222222222222222222222222222";

    private static readonly byte[] PlainKey = [1, 2, 3];
    private static readonly byte[] WrappedBytes = [4, 5, 6];

    private readonly List<Uri> _requested = [];
    private readonly Mock<CryptographyClient> _client = new(MockBehavior.Strict);

    private KeyVaultKeyWrapper CreateSut()
    {
        KeyVaultKeyId.TryParse($"https://vault.example/keys/bowlerpii-key/{CurrentVersion}", out var keyId)
            .ShouldBeTrue();

        return new KeyVaultKeyWrapper(keyId, uri =>
        {
            _requested.Add(uri);

            return _client.Object;
        });
    }

    private void SetupUnwrap()
        => _client
            .Setup(c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, WrappedBytes, TestContext.Current.CancellationToken))
            .ReturnsAsync(CryptographyModelFactory.UnwrapResult("key-id", PlainKey, KeyWrapAlgorithm.RsaOaep256));

    private void SetupUnwrapFailure(int status)
        => _client
            .Setup(c => c.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, WrappedBytes, TestContext.Current.CancellationToken))
            .ThrowsAsync(new RequestFailedException(status, "failed"));

    [Fact(DisplayName = "Wrap should use the configured version and report it")]
    public async Task WrapAsync_ShouldUseConfiguredVersion()
    {
        // Arrange
        _client
            .Setup(c => c.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, PlainKey, TestContext.Current.CancellationToken))
            .ReturnsAsync(CryptographyModelFactory.WrapResult("key-id", WrappedBytes, KeyWrapAlgorithm.RsaOaep256));
        var sut = CreateSut();

        // Act
        var result = await sut.WrapAsync(PlainKey, TestContext.Current.CancellationToken);

        // Assert
        result.KeyVersion.ShouldBe(CurrentVersion);
        result.Value.ShouldBe(WrappedBytes);
        _requested.ShouldHaveSingleItem()
            .ShouldBe(new Uri($"https://vault.example/keys/bowlerpii-key/{CurrentVersion}"));
    }

    [Fact(DisplayName = "Unwrap should use the version in the value and reuse its client")]
    public async Task UnwrapAsync_ShouldUseStoredVersion_AndCacheTheClient()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        SetupUnwrap();
        var sut = CreateSut();

        // Act
        var first = await sut.UnwrapAsync(OldVersion, WrappedBytes, ct);
        await sut.UnwrapAsync(OldVersion, WrappedBytes, ct);

        // Assert
        first.ShouldBe(PlainKey);
        _requested.ShouldHaveSingleItem()
            .ShouldBe(new Uri($"https://vault.example/keys/bowlerpii-key/{OldVersion}"));
    }

    [Fact(DisplayName = "A version that is not a Key Vault version should never reach Key Vault")]
    public async Task UnwrapAsync_ShouldThrow_WhenVersionIsNotValid()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.UnwrapAsync("../secrets", WrappedBytes, TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<CryptographicException>();
        _requested.ShouldBeEmpty();
    }

    [Theory(DisplayName = "A bad wrapped key or missing version should read as bad input")]
    [InlineData(400, TestDisplayName = "Bad parameter")]
    [InlineData(404, TestDisplayName = "Missing version")]
    public async Task UnwrapAsync_ShouldThrowCryptographicException_WhenKeyVaultRejectsTheInput(int status)
    {
        // Arrange
        SetupUnwrapFailure(status);
        var sut = CreateSut();

        // Act
        var act = () => sut.UnwrapAsync(OldVersion, WrappedBytes, TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<CryptographicException>();
    }

    [Fact(DisplayName = "A version that failed to unwrap should not keep its client")]
    public async Task UnwrapAsync_ShouldNotCacheTheClient_WhenUnwrapFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        SetupUnwrapFailure(404);
        var sut = CreateSut();
        await Should.ThrowAsync<CryptographicException>(() => sut.UnwrapAsync(OldVersion, WrappedBytes, ct));

        // Act
        var act = () => sut.UnwrapAsync(OldVersion, WrappedBytes, ct);

        // Assert
        await act.ShouldThrowAsync<CryptographicException>();
        _requested.Count.ShouldBe(2);
    }

    [Theory(DisplayName = "An outage or access failure should not read as bad input")]
    [InlineData(403, TestDisplayName = "Forbidden")]
    [InlineData(503, TestDisplayName = "Unavailable")]
    public async Task UnwrapAsync_ShouldRethrow_WhenKeyVaultFails(int status)
    {
        // Arrange
        SetupUnwrapFailure(status);
        var sut = CreateSut();

        // Act
        var act = () => sut.UnwrapAsync(OldVersion, WrappedBytes, TestContext.Current.CancellationToken);

        // Assert
        (await act.ShouldThrowAsync<RequestFailedException>()).Status.ShouldBe(status);
    }
}