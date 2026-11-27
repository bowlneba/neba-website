using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

/// <summary>
/// The sample was made by wwwroot/js/field-encryption.js; Jest checks its layout.
/// </summary>
[IntegrationTest]
[Component("Encryption")]
public sealed class BrowserSampleTests
{
    private sealed record Sample(string KeyName, string KeyVersion, string Plaintext, string WireValue);

    [Fact(DisplayName = "A sample made by the browser module should decrypt")]
    public async Task DecryptFromClientAsync_ShouldReadTheBrowserSample()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sample = JsonSerializer.Deserialize<Sample>(
            await File.ReadAllTextAsync(RepoFiles.BrowserSample, ct), JsonSerializerOptions.Web)!;
        var settings = new ConfigurationBuilder().AddJsonFile(RepoFiles.DevelopmentSettings, optional: false).Build();
        var wrapper = new LocalKeyWrapper(settings["Encryption:Keys:BowlerPii:LocalPrivateKey"]!);
        var sut = new Encryptor<BowlerPii>(wrapper, NullLogger<Encryptor<BowlerPii>>.Instance);

        // Act
        var result = await sut.DecryptFromClientAsync(sample.WireValue, ct);

        // Assert
        sample.KeyName.ShouldBe(BowlerPii.Name);
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(sample.Plaintext);
        // Fake SSNs only: nine digits that start with 9.
        result.Value.ShouldMatch(@"\A9\d{8}\z");
    }
}