using Bogus;

using Neba.Infrastructure.BackgroundJobs;

namespace Neba.TestFactory.Infrastructure.BackgroundJobs;

public static class HangfireSettingsFactory
{
    public const int ValidWorkerCount = 20;
    public const int ValidSucceededJobsRetentionDays = 30;
    public const int ValidDeletedJobsRetentionDays = 30;
    public const int ValidFailedJobsRetentionDays = 90;
    public const int ValidAutomaticRetryAttempts = 3;

    public static HangfireSettings Create(
        int? workerCount = null,
        int? succeededJobsRetentionDays = null,
        int? deletedJobsRetentionDays = null,
        int? failedJobsRetentionDays = null,
        int? automaticRetryAttempts = null)
            => new()
            {
                WorkerCount = workerCount ?? ValidWorkerCount,
                SucceededJobsRetentionDays = succeededJobsRetentionDays ?? ValidSucceededJobsRetentionDays,
                DeletedJobsRetentionDays = deletedJobsRetentionDays ?? ValidDeletedJobsRetentionDays,
                FailedJobsRetentionDays = failedJobsRetentionDays ?? ValidFailedJobsRetentionDays,
                AutomaticRetryAttempts = automaticRetryAttempts ?? ValidAutomaticRetryAttempts
            };

    public static HangfireSettings Bogus(int? seed = null)
    {
        var faker = new Faker<HangfireSettings>()
            .RuleFor(h => h.WorkerCount, f => f.Random.Int(1, 100))
            .RuleFor(h => h.SucceededJobsRetentionDays, f => f.Random.Int(1, 365))
            .RuleFor(h => h.DeletedJobsRetentionDays, f => f.Random.Int(1, 365))
            .RuleFor(h => h.FailedJobsRetentionDays, f => f.Random.Int(1, 365))
            .RuleFor(h => h.AutomaticRetryAttempts, f => f.Random.Int(1, 10));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate();
    }
}