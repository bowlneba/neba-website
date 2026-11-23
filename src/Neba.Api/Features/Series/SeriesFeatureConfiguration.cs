using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series.MakeDueStagesOfficial;

namespace Neba.Api.Features.Series;

internal static class SeriesFeatureConfiguration
{
    private const string MakeOfficialRecurringJobId = "series-make-official";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeries(IConfiguration configuration)
        {
            services.AddOptions<SeriesAdvancementSettings>()
                .Bind(configuration.GetSection(SeriesAdvancementSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddScoped<SeriesAdvancement>();
            services.AddScoped<ISeriesAdvancementFactsReader, SeriesAdvancementFactsReader>();

            return services;
        }
    }

    extension(WebApplication app)
    {
        public void UseSeriesMakeOfficialJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            // Mondays at 23:00 UTC (Hangfire default). One sweep makes every due Stage Official, so a weekend with two tournaments needs one run. A Stage waits for the next Monday if its grace period ends after the run.
            scheduler.AddOrUpdateRecurring(MakeOfficialRecurringJobId, new MakeDueStagesOfficialJob(), "0 23 * * 1");
        }
    }
}