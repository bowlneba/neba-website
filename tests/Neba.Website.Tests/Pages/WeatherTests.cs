using Bunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather;
using Neba.Api.Contracts.Weather.GetWeatherForecast;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Weather;

using Refit;

namespace Neba.Website.Tests.Pages;

/// <summary>
/// Minimal tests for Weather page - this page is a demo/example and will be removed.
/// Tests are included for SonarQube code coverage requirements.
/// </summary>
[UnitTest]
[Component("Website.Pages.Weather")]
public sealed class WeatherTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IWeatherApi> _mockWeatherApi;

    public WeatherTests()
    {
        _ctx = new BunitContext();

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

        _mockWeatherApi = new Mock<IWeatherApi>(MockBehavior.Loose);

        var mockStopwatchProvider = new Mock<IStopwatchProvider>(MockBehavior.Loose);
        mockStopwatchProvider.Setup(x => x.GetTimestamp()).Returns(0);
        mockStopwatchProvider.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var apiExecutor = new ApiExecutor(
            mockStopwatchProvider.Object,
            NullLogger<ApiExecutor>.Instance
        );

        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
        _ctx.Services.AddSingleton(_mockWeatherApi.Object);
        _ctx.Services.AddSingleton(apiExecutor);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldContainPageTitle_WhenRendered()
    {
        // Arrange - use a TaskCompletionSource that never completes to keep page in loading state
        var tcs = new TaskCompletionSource<IApiResponse<CollectionResponse<WeatherForecastResponse>>>();
        _mockWeatherApi.Setup(x => x.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = _ctx.Render<Weather>();

        // Assert
        cut.Markup.ShouldContain("Weather");
    }

    [Fact(DisplayName = "Should render loading indicator container initially")]
    public void Render_ShouldShowLoadingContainer_WhenDataIsLoading()
    {
        // Arrange - use a TaskCompletionSource that never completes
        var tcs = new TaskCompletionSource<IApiResponse<CollectionResponse<WeatherForecastResponse>>>();
        _mockWeatherApi.Setup(x => x.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = _ctx.Render<Weather>();

        // Assert - The page should contain the loading indicator container
        // (The actual indicator has a delay, but its container is rendered)
        cut.Markup.ShouldContain("min-h-[200px]");
    }

    [Fact(DisplayName = "Should render page description")]
    public void Render_ShouldContainDescription_WhenRendered()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IApiResponse<CollectionResponse<WeatherForecastResponse>>>();
        _mockWeatherApi.Setup(x => x.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var cut = _ctx.Render<Weather>();

        // Assert
        cut.Markup.ShouldContain("backend API");
    }

    [Fact(DisplayName = "Should render table with weather data when loaded successfully")]
    public async Task Render_ShouldDisplayWeatherTable_WhenDataLoadsSuccessfully()
    {
        // Arrange
        var weatherData = new List<WeatherForecastResponse>
        {
            new()
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                TemperatureC = 25,
                Summary = "Sunny"
            },
            new()
            {
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TemperatureC = 20,
                Summary = "Cloudy"
            }
        };

        var response = new CollectionResponse<WeatherForecastResponse> { Items = weatherData };

        var mockApiResponse = new Mock<IApiResponse<CollectionResponse<WeatherForecastResponse>>>(MockBehavior.Loose);
        mockApiResponse.Setup(r => r.Content).Returns(response);
        mockApiResponse.Setup(r => r.IsSuccessStatusCode).Returns(true);
        mockApiResponse.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _mockWeatherApi.Setup(x => x.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockApiResponse.Object);

        // Act
        var cut = _ctx.Render<Weather>();

        // Wait for component to load data and complete delay
        await Task.Delay(3000, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert - verify table exists
        var table = cut.Find("table.neba-table");
        table.ShouldNotBeNull();

        // Verify table headers
        cut.Markup.ShouldContain("<th>Date</th>");
        cut.Markup.ShouldContain("<th aria-label=\"Temperature in Celsius\">Temp. (C)</th>");
        cut.Markup.ShouldContain("<th aria-label=\"Temperature in Fahrenheit\">Temp. (F)</th>");
        cut.Markup.ShouldContain("<th>Summary</th>");

        // Verify weather data in table rows
        cut.Markup.ShouldContain("Sunny");
        cut.Markup.ShouldContain("Cloudy");
        cut.Markup.ShouldContain("25");
        cut.Markup.ShouldContain("20");
    }

    [Fact(DisplayName = "Should display error message when API call fails")]
    public async Task Render_ShouldDisplayError_WhenApiCallFails()
    {
        // Arrange
        _mockWeatherApi.Setup(x => x.GetWeatherForecastAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act
        var cut = _ctx.Render<Weather>();
        await Task.Delay(500, Xunit.TestContext.Current.CancellationToken);
        cut.Render();

        // Assert - verify error alert is shown
        cut.Markup.ShouldContain("Failed to load weather data");
        var errorAlerts = cut.FindAll(".neba-alert");
        errorAlerts.Count.ShouldBeGreaterThan(0);
    }
}