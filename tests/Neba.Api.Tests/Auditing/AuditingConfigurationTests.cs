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

    [Theory(DisplayName = "ToPartitionKey should replace characters Azure Table Storage rejects in PartitionKey")]
    [InlineData("Api:POST:/legacy/ping", "Api:POST:_legacy_ping")]
    [InlineData(@"Api:POST:\legacy\ping", "Api:POST:_legacy_ping")]
    [InlineData("Api:POST:/legacy#ping?x", "Api:POST:_legacy_ping_x")]
    public void ToPartitionKey_ShouldReplaceInvalidCharacters_WhenEventTypeContainsThem(string eventType, string expected)
    {
        // Act
        var result = AuditingConfiguration.ToPartitionKey(eventType);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "ToPartitionKey should return the event type unchanged when it contains no invalid characters")]
    public void ToPartitionKey_ShouldReturnUnchanged_WhenEventTypeContainsNoInvalidCharacters()
    {
        // Arrange
        const string eventType = "EF:AppDbContext";

        // Act
        var result = AuditingConfiguration.ToPartitionKey(eventType);

        // Assert
        result.ShouldBe(eventType);
    }

    [Theory(DisplayName = "ToPartitionKey should return \"unknown\" when the event type is null or empty")]
    [InlineData(null)]
    [InlineData("")]
    public void ToPartitionKey_ShouldReturnUnknown_WhenEventTypeIsNullOrEmpty(string? eventType)
    {
        // Act
        var result = AuditingConfiguration.ToPartitionKey(eventType);

        // Assert
        result.ShouldBe("unknown");
    }
}