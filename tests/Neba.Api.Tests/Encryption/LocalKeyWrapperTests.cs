using System.Security.Cryptography;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class LocalKeyWrapperTests
{
    private static LocalKeyWrapper CreateSut()
    {
        using var rsa = RSA.Create(2048);

        return new LocalKeyWrapper(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
    }

    [Fact(DisplayName = "A wrapped key should unwrap, under the version 'local'")]
    public async Task UnwrapAsync_ShouldReturnKey_WhenWrappedLocally()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var key = RandomNumberGenerator.GetBytes(32);
        var sut = CreateSut();
        var wrapped = await sut.WrapAsync(key, ct);

        // Act
        var result = await sut.UnwrapAsync(wrapped.KeyVersion, wrapped.Value, ct);

        // Assert
        wrapped.KeyVersion.ShouldBe("local");
        result.ShouldBe(key);
    }

    [Fact(DisplayName = "Any version other than 'local' should be rejected")]
    public async Task UnwrapAsync_ShouldThrow_WhenVersionIsUnknown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.UnwrapAsync("other", new byte[256], TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<CryptographicException>();
    }
}