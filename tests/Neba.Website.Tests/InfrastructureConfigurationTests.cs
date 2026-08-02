using Microsoft.AspNetCore.Builder;

using Neba.TestFactory.Attributes;
using Neba.Website.Server;

namespace Neba.Website.Tests;

[UnitTest]
[Component("Website.InfrastructureConfiguration")]
public sealed class InfrastructureConfigurationTests
{
#nullable disable
    [Fact(DisplayName = "AddInfrastructure should throw when the builder is null")]
    public void AddInfrastructure_ShouldThrow_WhenBuilderIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.AddInfrastructure());
    }
#nullable enable

    [Fact(DisplayName = "AddInfrastructure should return the same builder when no Key Vault connection string is configured")]
    public void AddInfrastructure_ShouldReturnSameBuilder_WhenNoKeyVaultConnectionStringIsConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:keyvault"] = null;

        // Act
        var result = builder.AddInfrastructure();

        // Assert
        result.ShouldBeSameAs(builder);
    }
}