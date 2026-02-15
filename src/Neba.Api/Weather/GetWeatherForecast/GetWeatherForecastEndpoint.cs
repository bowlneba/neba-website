using System.Security.Cryptography;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Weather.GetWeatherForecast;

namespace Neba.Api.Weather.GetWeatherForecast;

internal sealed class GetWeatherForecastEndpoint
    : EndpointWithoutRequest<CollectionResponse<WeatherForecastResponse>>
{
    public override void Configure()
    {
        Get(string.Empty);
        Group<WeatherGroup>();

        AllowAnonymous();

        Description(description => description
            .WithName("GetWeatherForecast")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        var forecasts = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = RandomNumberGenerator.GetInt32(-20, 55),
                Summary = summaries[RandomNumberGenerator.GetInt32(summaries.Length)]
            })
            .ToArray();

        var response = new CollectionResponse<WeatherForecastResponse>
        {
            Items = [.. forecasts],
        };

        await Send.OkAsync(response, ct);
    }
}