using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Seasons.CreateNextSeason;

namespace Neba.Api.Features.Seasons;

internal static class SeasonsConfiguration
{
    private const string CreateNextSeasonRecurringJobId = "create-next-season";

    extension(WebApplication app)
    {
        public void UseSeasonRolloverJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            scheduler.AddOrUpdateRecurring(
                CreateNextSeasonRecurringJobId,
                new CreateNextSeasonJob(),
                "0 5 1 1,4,7,10 *"); // Quarterly on the 1st at 5:00 AM
        }
    }
}