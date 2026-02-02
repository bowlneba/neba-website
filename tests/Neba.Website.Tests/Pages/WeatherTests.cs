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

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Loose);
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
}