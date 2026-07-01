using System.Net;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

using Refit;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services")]
public sealed class ApiServicesConfigurationTests
{
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
