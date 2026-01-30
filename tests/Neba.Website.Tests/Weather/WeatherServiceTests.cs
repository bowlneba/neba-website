using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather;
using Neba.Api.Contracts.Weather.GetWeatherForecast;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Weather;

using Refit;

namespace Neba.Website.Tests.Weather;

[UnitTest]
[Component("Website.Weather")]
public sealed class WeatherServiceTests
{
    [Fact(DisplayName = "Should return weather forecast when API call succeeds")]
    public async Task GetWeatherForecastAsync_ShouldReturnForecast_WhenApiCallSucceeds()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expectedForecasts = new[]
        {
            new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                TemperatureC = 20,
                Summary = "Sunny"
            },
            new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                TemperatureC = 15,
                Summary = "Cloudy"
            }
        };

        var expectedResponse = new CollectionResponse<WeatherForecastResponse>
        {
            Items = expectedForecasts,
        };

        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedResponse);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(cancellationToken))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act
        var result = await service.GetWeatherForecastAsync(cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldBe(expectedForecasts);
        result.TotalItems.ShouldBe(expectedForecasts.Length);
        weatherApiMock.Verify(
            api => api.GetWeatherForecastAsync(cancellationToken),
            Times.Once);
    }

    [Fact(DisplayName = "Should throw when API response is not success status code")]
    public async Task GetWeatherForecastAsync_ShouldThrow_WhenNotSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(false);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(cancellationToken))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetWeatherForecastAsync(cancellationToken));

        exception.Message.ShouldBe("Failed to retrieve weather forecast data.");
    }

    [Fact(DisplayName = "Should throw when API response content is null")]
    public async Task GetWeatherForecastAsync_ShouldThrow_WhenContentIsNull()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns((CollectionResponse<WeatherForecastResponse>?)null);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(cancellationToken))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetWeatherForecastAsync(cancellationToken));

        exception.Message.ShouldBe("Failed to retrieve weather forecast data.");
    }

    [Fact(DisplayName = "Should propagate cancellation token from test context to API")]
    public async Task GetWeatherForecastAsync_ShouldPropagateTestContextCancellationToken_ToApi()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expectedResponse = new CollectionResponse<WeatherForecastResponse>
        {
            Items = []
        };

        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedResponse);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(cancellationToken))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act
        await service.GetWeatherForecastAsync(cancellationToken);

        // Assert
        weatherApiMock.Verify(
            api => api.GetWeatherForecastAsync(cancellationToken),
            Times.Once);
    }

    [Fact(DisplayName = "Should use default cancellation token when none provided")]
    public async Task GetWeatherForecastAsync_ShouldUseDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        var expectedResponse = new CollectionResponse<WeatherForecastResponse>
        {
            Items = []
        };

        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedResponse);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act
        await service.GetWeatherForecastAsync(TestContext.Current.CancellationToken);

        // Assert
        weatherApiMock.Verify(
            api => api.GetWeatherForecastAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should return empty collection when API returns no items")]
    public async Task GetWeatherForecastAsync_ShouldReturnEmptyCollection_WhenApiReturnsNoItems()
    {
        // Arrange
        var expectedResponse = new CollectionResponse<WeatherForecastResponse>
        {
            Items = []
        };

        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedResponse);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act
        var result = await service.GetWeatherForecastAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
    }

    [Theory(DisplayName = "Should throw with correct message for various failure scenarios")]
    [InlineData(true, null, TestDisplayName = "Success status but null content")]
    [InlineData(false, null, TestDisplayName = "Failed status with null content")]
    public async Task GetWeatherForecastAsync_ShouldThrowWithCorrectMessage_ForFailureScenarios(
        bool isSuccess,
        CollectionResponse<WeatherForecastResponse>? content)
    {
        // Arrange
        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(isSuccess);
        if (isSuccess)
        {
            apiResponseMock.Setup(r => r.Content).Returns(content);
        }

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetWeatherForecastAsync());

        exception.Message.ShouldBe("Failed to retrieve weather forecast data.");
    }

    [Fact(DisplayName = "Should return large collection of forecasts")]
    public async Task GetWeatherForecastAsync_ShouldReturnLargeCollection_Successfully()
    {
        // Arrange
        var forecasts = Enumerable.Range(1, 100)
            .Select(i => new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                TemperatureC = 15 + (i % 10),
                Summary = $"Summary {i}"
            })
            .ToArray();

        var expectedResponse = new CollectionResponse<WeatherForecastResponse>
        {
            Items = forecasts
        };

        var apiResponseMock = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedResponse);

        var weatherApiMock = new Mock<IWeatherApi>(MockBehavior.Strict);
        weatherApiMock
            .Setup(api => api.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponseMock.Object);

        var service = new WeatherService(weatherApiMock.Object);

        // Act
        var result = await service.GetWeatherForecastAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(100);
        result.TotalItems.ShouldBe(100);
    }
}
