using System.Net;
using System.Net.Http.Json;
using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather.GetWeatherForecast;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Weather;

[IntegrationTest]
[Component("Api.Weather")]
public sealed class GetWeatherForecastsEndpointTests(AspireFixture fixture)
    : IClassFixture<AspireFixture>
{
    [Fact(DisplayName = "Should return weather forecasts with 200 OK")]
    public async Task GetWeatherForecast_ShouldReturnForecasts_WhenCalled()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await fixture.ApiClient.GetAsync(
            new Uri("/weatherforecast", UriKind.Relative),
            cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<CollectionResponse<WeatherForecastResponse>>(cancellationToken);

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeEmpty();
        result.Items.Count.ShouldBe(5);
        result.TotalItems.ShouldBe(5);

        foreach (var forecast in result.Items)
        {
            forecast.Date.ShouldBeGreaterThan(DateOnly.FromDateTime(DateTime.Now));
            forecast.Summary.ShouldNotBeNullOrWhiteSpace();
            forecast.TemperatureC.ShouldBeInRange(-20, 55);
        }
    }
}
