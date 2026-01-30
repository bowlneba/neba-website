using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Weather;

internal sealed class WeatherGroup
    : SubGroup<BaseGroup>
{
    public WeatherGroup()
    {
        VersionSets.CreateApi("Weather", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("weatherforecast", ep =>
        {
            ep.Description(d => d.WithTags("Weather"));
        });
    }
}