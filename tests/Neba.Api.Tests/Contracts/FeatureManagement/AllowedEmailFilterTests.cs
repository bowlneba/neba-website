using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Neba.Api.Contracts.FeatureManagement;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.FeatureManagement;

[UnitTest]
[Component("Api.Contracts")]
public sealed class AllowedEmailFilterTests
{
    private readonly AllowedEmailFilter _filter = new();

    [Fact(DisplayName = "EvaluateAsync should return false when email is null")]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenEmailIsNull()
    {
#nullable disable
        // Arrange
        var featureFilterContext = CreateFeatureFilterContext(["allowed@bowlneba.com"]);
        var appContext = new AllowedEmailContext { Email = null };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeFalse();
#nullable enable
    }

    [Fact(DisplayName = "EvaluateAsync should return false when email is empty")]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenEmailIsEmpty()
    {
        // Arrange
        var featureFilterContext = CreateFeatureFilterContext(["allowed@bowlneba.com"]);
        var appContext = new AllowedEmailContext { Email = string.Empty };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "EvaluateAsync should return true when email is in the allowed list")]
    public async Task EvaluateAsync_ShouldReturnTrue_WhenEmailIsInAllowedList()
    {
        // Arrange
        const string email = "allowed@bowlneba.com";
        var featureFilterContext = CreateFeatureFilterContext([email, "other@bowlneba.com"]);
        var appContext = new AllowedEmailContext { Email = email };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "EvaluateAsync should return false when email is not in the allowed list")]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenEmailIsNotInAllowedList()
    {
        // Arrange
        var featureFilterContext = CreateFeatureFilterContext(["allowed@bowlneba.com"]);
        var appContext = new AllowedEmailContext { Email = "notallowed@bowlneba.com" };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "EvaluateAsync should return false when the allowed list is empty")]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenAllowedListIsEmpty()
    {
        // Arrange
        var featureFilterContext = CreateFeatureFilterContext([]);
        var appContext = new AllowedEmailContext { Email = "allowed@bowlneba.com" };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "EvaluateAsync should return false when no filter settings are configured")]
    public async Task EvaluateAsync_ShouldReturnFalse_WhenNoParametersAreConfigured()
    {
        // Arrange
        var featureFilterContext = new FeatureFilterEvaluationContext
        {
            Parameters = new ConfigurationBuilder().Build(),
        };
        var appContext = new AllowedEmailContext { Email = "allowed@bowlneba.com" };

        // Act
        var result = await _filter.EvaluateAsync(featureFilterContext, appContext);

        // Assert
        result.ShouldBeFalse();
    }

    private static FeatureFilterEvaluationContext CreateFeatureFilterContext(IReadOnlyList<string> allowedEmails)
    {
        var configurationData = allowedEmails
            .Select((email, index) => new KeyValuePair<string, string?>($"AllowedEmails:{index}", email));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        return new FeatureFilterEvaluationContext { Parameters = configuration };
    }
}
