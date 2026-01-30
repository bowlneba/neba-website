namespace Neba.Api.Contracts.Weather.GetWeatherForecast;

/// <summary>
/// Represents a weather forecast response.
/// </summary>
public sealed record WeatherForecastResponse
{
    /// <summary>
    /// The date of the weather forecast.
    /// </summary>
    /// <example>2024-06-15</example>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The temperature in Celsius.
    /// </summary>
    /// <example>25</example>
    public required int TemperatureC { get; init; }

    /// <summary>
    /// The temperature in Fahrenheit.
    /// </summary>
    /// <example>77</example>
    public int TemperatureF
        => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// A summary description of the weather.
    /// </summary>
    /// <example>Warm</example>
    public required string Summary { get; init; }
}