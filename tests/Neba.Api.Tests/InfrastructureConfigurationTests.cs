using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests;

[UnitTest]
[Component("Api.InfrastructureConfiguration")]
public sealed class InfrastructureConfigurationTests
{
    [Fact(DisplayName = "AddInfrastructure should register the bowler PII encryptor and hasher")]
    public void AddInfrastructure_ShouldRegisterEncryption_WhenCalled()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear();
        // Program registers the handlers first; AddInfrastructure decorates them.
        builder.Services.AddDomain().AddApplication();

        // Act
        builder.AddInfrastructure();

        // Assert
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(IEncryptor<BowlerPii>));
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(IHasher<BowlerPii>));
    }

    [Theory(DisplayName = "Only the vaults with a connection string should load")]
    [InlineData("https://main.example/", "https://pii.example/", new[] { "keyvault", "piivault" }, TestDisplayName = "Both")]
    [InlineData("https://main.example/", " ", new[] { "keyvault" }, TestDisplayName = "Blank PII vault")]
    [InlineData(null, "https://pii.example/", new[] { "piivault" }, TestDisplayName = "PII vault only")]
    [InlineData(null, null, new string[0], TestDisplayName = "Neither")]
    public void AddKeyVault_ShouldLoadVaults_WhenTheyHaveAConnectionString(
        string? keyVault, string? piiVault, string[] expected)
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:keyvault"] = keyVault,
            ["ConnectionStrings:piivault"] = piiVault
        });
        List<string> loaded = [];

        // Act
        builder.AddKeyVault(loaded.Add);

        // Assert
        loaded.ShouldBe(expected);
    }
}