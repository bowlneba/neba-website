using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Series;

internal static class SeriesFeatureConfiguration
{
    private const string MakeOfficialRecurringJobId = "series-make-official";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeries()
        {
            services.AddScoped<SeriesAdvancement>();
            services.AddScoped<ISeriesAdvancementFactsReader, SeriesAdvancementFactsReader>();

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Removes the recurring job that used to make Stages Official on a timer. Hangfire keeps a recurring job in its
        /// database after its registration leaves the code, and the job would fail every run. Stages now become Official
        /// when a person marks them. Delete this call once every environment has deployed it.
        /// </summary>
        public void UseSeriesRemoveMakeOfficialJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            scheduler.RemoveRecurring(MakeOfficialRecurringJobId);
        }
    }
}