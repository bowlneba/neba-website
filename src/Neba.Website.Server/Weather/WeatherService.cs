using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather;
using Neba.Api.Contracts.Weather.GetWeatherForecast;

namespace Neba.Website.Server.Weather;

internal sealed class WeatherService(IWeatherApi weatherApi)
{
    public async Task<CollectionResponse<WeatherForecastResponse>> GetWeatherForecastAsync(CancellationToken cancellationToken = default)
    {
        var response = await weatherApi.GetWeatherForecastAsync(cancellationToken);

        return response.IsSuccessStatusCode && response.Content != null
            ? response.Content
            : throw new InvalidOperationException("Failed to retrieve weather forecast data.");
    }
}