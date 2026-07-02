using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

using Refit;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services")]
public sealed class ApiServicesConfigurationTests
{
    private static IConfiguration BuildConfiguration(string? baseUrl)
    {
        var data = new Dictionary<string, string?>();
        if (baseUrl is not null)
            data["NebaApi:BaseUrl"] = baseUrl;

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact(DisplayName = "AddApiServices should register ApiExecutor as scoped and BearerTokenHandler as transient")]
    public void AddApiServices_ShouldRegisterApiExecutorAndBearerTokenHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("https://api.example.com");

        // Act
        services.AddApiServices(configuration);

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(ApiExecutor) && sd.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(sd => sd.ServiceType == typeof(BearerTokenHandler) && sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact(DisplayName = "AddApiServices should resolve each Refit endpoint with the configured BaseUrl applied")]
    public void AddApiServices_ShouldResolveRefitEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddLogging();
        var configuration = BuildConfiguration("https://api.example.com");
        services.AddApiServices(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var documentsApi = provider.GetRequiredService<IDocumentsApi>();
        var securityApi = provider.GetRequiredService<ISecurityApi>();

        // Assert
        documentsApi.ShouldNotBeNull();
        securityApi.ShouldNotBeNull();
    }

    [Fact(DisplayName = "AddApiServices should throw on start validation when BaseUrl is missing")]
    public void AddApiServices_ShouldThrowOnValidation_WhenBaseUrlIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(baseUrl: null);
        services.AddApiServices(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        NebaApiConfiguration Act() => provider.GetRequiredService<IOptions<NebaApiConfiguration>>().Value;

        // Assert
        Should.Throw<OptionsValidationException>((Func<NebaApiConfiguration>)Act)
            .Message.ShouldContain("BaseUrl must be a valid absolute URI");
    }

    [Fact(DisplayName = "AddApiServices should throw on start validation when BaseUrl is not absolute")]
    public void AddApiServices_ShouldThrowOnValidation_WhenBaseUrlIsRelative()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("/relative-path");
        services.AddApiServices(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        NebaApiConfiguration Act() => provider.GetRequiredService<IOptions<NebaApiConfiguration>>().Value;

        // Assert
        Should.Throw<OptionsValidationException>((Func<NebaApiConfiguration>)Act)
            .Message.ShouldContain("BaseUrl must be a valid absolute URI");
    }

    [Fact(DisplayName = "Should strip Authorization header when redacting an ApiException")]
    public async Task ExceptionRedactor_ShouldStripAuthorizationHeader_WhenApplied()
    {
        // Arrange
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret-token");

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Error")
        };

        var apiException = await ApiException.Create(
            requestMessage,
            HttpMethod.Get,
            responseMessage,
            ApiServicesConfiguration.RefitSettings
        );

        // Act
        ApiServicesConfiguration.RefitSettings.ExceptionRedactor!.Invoke(apiException);

        // Assert
        apiException.RequestMessage.Headers.Authorization.ShouldBeNull();
    }

    [Fact(DisplayName = "Should strip Set-Cookie response header when redacting an ApiException")]
    public async Task ExceptionRedactor_ShouldStripSetCookieHeader_WhenApplied()
    {
        // Arrange
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");

        using var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Error")
        };
        responseMessage.Headers.Add("Set-Cookie", "session=abc123; HttpOnly");

        var apiException = await ApiException.Create(
            requestMessage,
            HttpMethod.Get,
            responseMessage,
            ApiServicesConfiguration.RefitSettings
        );

        // Act
        ApiServicesConfiguration.RefitSettings.ExceptionRedactor!.Invoke(apiException);

        // Assert
        apiException.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should cap captured exception content length")]
    public void MaxExceptionContentLength_ShouldBeBounded()
    {
        // Assert
        ApiServicesConfiguration.RefitSettings.MaxExceptionContentLength.ShouldBe(2048);
    }
}