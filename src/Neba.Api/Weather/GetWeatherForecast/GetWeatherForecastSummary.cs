using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather.GetWeatherForecast;

namespace Neba.Api.Weather.GetWeatherForecast;

internal sealed class GetWeatherForecastSummary
    : Summary<GetWeatherForecastEndpoint>
{
    public GetWeatherForecastSummary()
    {
        Summary = "Gets a 5-day weather forecast.";
        Description = "Retrieves a list of weather forecasts for the next 5 days.";

        Response(200, "A list of weather forecasts.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<WeatherForecastResponse>
            {
                Items = [
                    new WeatherForecastResponse
                    {
                        Date = new DateOnly(2026, 1, 1),
                        TemperatureC = 25,
                        Summary = "Warm"
                    },
                    new WeatherForecastResponse
                    {
                        Date = new DateOnly(2026, 1, 2),
                        TemperatureC = 30,
                        Summary = "Hot"
                    }
                ]
            });
    }
}