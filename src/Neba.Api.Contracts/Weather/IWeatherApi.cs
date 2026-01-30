using Neba.Api.Contracts.Weather.GetWeatherForecast;

using Refit;

namespace Neba.Api.Contracts.Weather;

/// <summary>
/// Defines the weather API contract.
/// </summary>
public interface IWeatherApi
{
    /// <summary>
    /// Gets the weather forecast.
    /// </summary>
    /// <returns>A collection response containing weather forecast data.</returns>
    [Get("/weatherforecast")]
    Task<ApiResponse<CollectionResponse<WeatherForecastResponse>>> GetWeatherForecastAsync(CancellationToken cancellationToken = default);
}