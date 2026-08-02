using Audit.AzureStorageTables.ConfigurationApi;

using Azure.Core;

using Neba.Api.Auditing;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class AuditingConfigurationTests
{
    [Fact(DisplayName = "ConfigureConnection should use ConnectionString when the value is not an absolute URI")]
    public void ConfigureConnection_ShouldUseConnectionString_WhenValueIsNotAbsoluteUri()
    {
        // Arrange
        var entityConfigurator = new Mock<IAzureTablesEntityConfigurator>(MockBehavior.Strict);
        var connectionConfigurator = new Mock<IAzureTableConnectionConfigurator>(MockBehavior.Strict);
        connectionConfigurator
            .Setup(c => c.ConnectionString("UseDevelopmentStorage=true"))
            .Returns(entityConfigurator.Object)
            .Verifiable();

        // Act
        var result = connectionConfigurator.Object.ConfigureConnection("UseDevelopmentStorage=true");

        // Assert
        result.ShouldBeSameAs(entityConfigurator.Object);
        connectionConfigurator.VerifyAll();
    }

    [Fact(DisplayName = "ConfigureConnection should use a token-credentialed endpoint when the value is a bare service endpoint URI")]
    public void ConfigureConnection_ShouldUseTokenCredentialedEndpoint_WhenValueIsAbsoluteUri()
    {
        // Arrange
        var endpoint = new Uri("https://neba.table.core.windows.net/");
        var entityConfigurator = new Mock<IAzureTablesEntityConfigurator>(MockBehavior.Strict);
        var connectionConfigurator = new Mock<IAzureTableConnectionConfigurator>(MockBehavior.Strict);
        connectionConfigurator
            .Setup(c => c.Endpoint(endpoint, It.IsAny<TokenCredential>()))
            .Returns(entityConfigurator.Object)
            .Verifiable();

        // Act
        var result = connectionConfigurator.Object.ConfigureConnection(endpoint.ToString());

        // Assert
        result.ShouldBeSameAs(entityConfigurator.Object);
        connectionConfigurator.VerifyAll();
    }
}