using Neba.Api.BackgroundJobs;
using Neba.Api.Uploads;

namespace Neba.Api.Storage;

internal static class StorageConfiguration
{
    private const string CleanupOrphanedUploadsRecurringJobId = "cleanup-orphaned-uploads";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddStorage()
        {
            builder.AddAzureBlobServiceClient("blob");

            builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
            builder.Services.AddSingleton<IUploadStagingService, UploadStagingService>();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public void UseUploadCleanupJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            scheduler.AddOrUpdateRecurring(
                CleanupOrphanedUploadsRecurringJobId,
                new CleanupOrphanedUploadsJob(),
                "0 5 */2 * *"); // Every other day at 5:00 AM
        }
    }
}