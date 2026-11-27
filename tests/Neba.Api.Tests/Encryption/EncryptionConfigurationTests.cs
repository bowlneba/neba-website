using System.Security.Cryptography;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

namespace Neba.Api.Tests.Encryption;

[IntegrationTest]
[Component("Encryption")]
public sealed class EncryptionConfigurationTests
{
    private const string Section = "Encryption:Keys:BowlerPii";
    private const string KeyUri = "https://vault.example/keys/bowlerpii-key/0123456789abcdef0123456789abcdef";

    private static readonly string HashKey = Convert.ToBase64String(new byte[32]);

    private static string PrivateKey()
    {
        using var rsa = RSA.Create(2048);

        return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    private static WebApplication Build(string environment, Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseTestServer();
        builder.AddEncryption();

        return builder.Build();
    }

    // The app is stopped, never disposed (docs/learnings/testing.md).
    private static async Task<OptionsValidationException?> StartAsync(string environment, Dictionary<string, string?> settings)
    {
        var app = Build(environment, settings);
        var exception = await Record.ExceptionAsync(() => app.StartAsync(TestContext.Current.CancellationToken));
        await app.StopAsync(TestContext.Current.CancellationToken);

        return exception is null ? null : exception.ShouldBeOfType<OptionsValidationException>();
    }

    private static Dictionary<string, string?> ProductionSettings() => new()
    {
        [$"{Section}:KeyVaultKeyUri"] = KeyUri,
        [$"{Section}:HashKey"] = HashKey
    };

    [Theory(DisplayName = "Startup should succeed outside Development with a Key Vault URI and hash key")]
    [InlineData("Production", TestDisplayName = "Production")]
    [InlineData("Staging", TestDisplayName = "Staging")]
    public async Task Start_ShouldSucceed_WhenSettingsAreComplete(string environment)
    {
        // Arrange
        var settings = ProductionSettings();

        // Act
        var failure = await StartAsync(environment, settings);

        // Assert
        failure.ShouldBeNull();
    }

    [Theory(DisplayName = "Startup should fail outside Development when a setting is missing")]
    [InlineData("KeyVaultKeyUri", TestDisplayName = "Key Vault key URI")]
    [InlineData("HashKey", TestDisplayName = "Hash key")]
    public async Task Start_ShouldFail_WhenASettingIsMissing(string setting)
    {
        // Arrange
        var settings = ProductionSettings();
        settings.Remove($"{Section}:{setting}");

        // Act
        var failure = await StartAsync("Production", settings);

        // Assert
        failure.ShouldNotBeNull();
        failure.Message.ShouldContain($"{Section}:{setting}");
    }

    [Fact(DisplayName = "Startup should fail outside Development when a local private key is set")]
    public async Task Start_ShouldFail_WhenALocalKeyIsSetOutsideDevelopment()
    {
        // Arrange
        var privateKey = PrivateKey();
        var settings = ProductionSettings();
        settings[$"{Section}:LocalPrivateKey"] = privateKey;

        // Act
        var failure = await StartAsync("Production", settings);

        // Assert
        failure.ShouldNotBeNull();
        failure.Message.ShouldContain($"{Section}:LocalPrivateKey");
        failure.Message.ShouldNotContain(privateKey);
    }

    [Theory(DisplayName = "Startup should fail when a setting cannot be used")]
    [InlineData("Production", "HashKey", "c2hvcnQ=", TestDisplayName = "Short hash key")]
    [InlineData("Production", "HashKey", "not base64!", TestDisplayName = "Hash key is not Base64")]
    [InlineData("Production", "KeyVaultKeyUri", "https://vault.example/keys/k", TestDisplayName = "Key URI without a version")]
    [InlineData("Development", "KeyVaultKeyUri", "https://vault.example/keys/k", TestDisplayName = "Bad key URI in Development")]
    public async Task Start_ShouldFail_WhenASettingIsInvalid(string environment, string setting, string value)
    {
        // Arrange
        var settings = ProductionSettings();
        settings[$"{Section}:{setting}"] = value;

        // Act
        var failure = await StartAsync(environment, settings);

        // Assert
        failure.ShouldNotBeNull();
        failure.Message.ShouldContain($"{Section}:{setting}");
        failure.Message.ShouldNotContain(value);
    }

    [Fact(DisplayName = "Startup should fail in Development with neither a local key nor a Key Vault URI")]
    public async Task Start_ShouldFail_WhenDevelopmentHasNoKey()
    {
        // Arrange
        var settings = new Dictionary<string, string?> { [$"{Section}:HashKey"] = HashKey };

        // Act
        var failure = await StartAsync("Development", settings);

        // Assert
        failure.ShouldNotBeNull();
        failure.Message.ShouldContain($"{Section}:LocalPrivateKey");
    }

    [Fact(DisplayName = "A Key Vault URI should build the Key Vault encryptor without calling Azure")]
    public async Task AddEncryption_ShouldResolveServices_WhenKeyVaultUriIsSet()
    {
        // Arrange
        var app = Build("Production", ProductionSettings());

        // A "local" version is refused by the Key Vault wrapper before any call to Azure; the local
        // wrapper would try to decrypt it instead.
        var value = new Envelope(LocalKeyWrapper.KeyVersion, new byte[256], new byte[12], new byte[16], new byte[9])
            .Encode();

        // Act
        var encryptor = app.Services.GetRequiredService<IEncryptor<BowlerPii>>();
        var hasher = app.Services.GetRequiredService<IHasher<BowlerPii>>();
        var exception = await Record.ExceptionAsync(
            () => encryptor.DecryptAsync(new Encrypted<BowlerPii>(value), TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<CryptographicException>().Message.ShouldBe("Unknown key version.");
        hasher.ShouldNotBeNull();
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "The local settings committed in the repo should load and work")]
    public async Task AddEncryption_ShouldRoundTrip_WithTheRepoDevelopmentSettings()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(RepoFiles.DevelopmentSettings, optional: false);
        builder.WebHost.UseTestServer();
        builder.AddEncryption();
        var app = builder.Build();
        await app.StartAsync(ct);
        var encryptor = app.Services.GetRequiredService<IEncryptor<BowlerPii>>();

        var ssn = SsnFactory.Create();

        // Act
        var encrypted = await encryptor.EncryptAsync(ssn, ct);
        var decrypted = await encryptor.DecryptAsync(encrypted, ct);
        var hash = app.Services.GetRequiredService<IHasher<BowlerPii>>().Hash(ssn);

        // Assert
        decrypted.ShouldBe(ssn);
        Envelope.Decode(encrypted.Value).KeyVersion.ShouldBe(LocalKeyWrapper.KeyVersion);
        hash.Value.ShouldNotBeNullOrWhiteSpace();
        await app.StopAsync(ct);
    }
}