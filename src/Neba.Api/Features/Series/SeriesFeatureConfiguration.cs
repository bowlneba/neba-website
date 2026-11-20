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

            // Every 15 minutes, so a Stage becomes Official up to 15 minutes after its grace period ends.
            scheduler.AddOrUpdateRecurring(MakeOfficialRecurringJobId, new MakeDueStagesOfficialJob(), "*/15 * * * *");
        }
    }
}