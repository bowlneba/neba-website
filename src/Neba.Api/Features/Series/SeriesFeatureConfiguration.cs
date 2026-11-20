namespace Neba.Api.Features.Series;

internal static class SeriesFeatureConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeries()
        {
            services.AddScoped<SeriesAdvancement>();
            services.AddScoped<ISeriesAdvancementFactsReader, SeriesAdvancementFactsReader>();

            return services;
        }
    }
}