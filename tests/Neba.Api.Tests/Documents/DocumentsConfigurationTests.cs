using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Documents;
using Neba.Api.Features.Documents.SyncDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Documents;

[IntegrationTest]
[Component("Documents")]
public sealed class DocumentsConfigurationTests
{
    private static GoogleSettings CreateSettings(params string[] documentNames)
    {
        return new GoogleSettings
        {
            ApplicationName = "Test Application",
            Credentials = new GoogleCredentials
            {
                ProjectId = "test-project",
                PrivateKey = "test-private-key",
                ClientEmail = "test@test-project.iam.gserviceaccount.com",
                PrivateKeyId = "test-key-id"
            },
            Documents = documentNames
                .Select(name => new GoogleDocument
                {
                    Name = name,
                    DocumentId = $"doc-{name}",
                    WebRoute = $"/{name}"
                })
                .ToArray()
        };
    }

    private static WebApplication BuildApp(GoogleSettings settings, IBackgroundJobScheduler scheduler)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(scheduler);

        return builder.Build();
    }

    [Fact(DisplayName = "UseDocumentSyncJobs should enqueue one startup sync per configured document")]
    public void UseDocumentSyncJobs_ShouldEnqueueStartupSyncOnce_PerConfiguredDocument()
    {
        // Arrange
        var settings = CreateSettings("bylaws", "tournament-rules");
        var enqueuedOnceCalls = new List<(SyncDocumentToStorageJob Job, string Key, TimeSpan Window)>();

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(s => s.AddOrUpdateRecurring(It.IsAny<string>(), It.IsAny<SyncDocumentToStorageJob>(), It.IsAny<string>()));
        scheduler
            .Setup(s => s.EnqueueOnce(It.IsAny<SyncDocumentToStorageJob>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<SyncDocumentToStorageJob, string, TimeSpan>((job, key, window) =>
                enqueuedOnceCalls.Add((job, key, window)));

        using var app = BuildApp(settings, scheduler.Object);

        // Act
        app.UseDocumentSyncJobs();

        // Assert
        enqueuedOnceCalls.Count.ShouldBe(2);
        enqueuedOnceCalls.ShouldContain(call =>
            call.Job.DocumentName == "bylaws" &&
            call.Job.TriggeredBy == "startup" &&
            call.Key == "startup-sync-document-bylaws" &&
            call.Window == TimeSpan.FromMinutes(15));
        enqueuedOnceCalls.ShouldContain(call =>
            call.Job.DocumentName == "tournament-rules" &&
            call.Job.TriggeredBy == "startup" &&
            call.Key == "startup-sync-document-tournament-rules" &&
            call.Window == TimeSpan.FromMinutes(15));
    }

    [Fact(DisplayName = "UseDocumentSyncJobs should register a recurring sync job per configured document")]
    public void UseDocumentSyncJobs_ShouldRegisterRecurringSync_PerConfiguredDocument()
    {
        // Arrange
        var settings = CreateSettings("bylaws", "tournament-rules");
        var recurringCalls = new List<(string RecurringJobId, SyncDocumentToStorageJob Job, string CronExpression)>();

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(s => s.AddOrUpdateRecurring(It.IsAny<string>(), It.IsAny<SyncDocumentToStorageJob>(), It.IsAny<string>()))
            .Callback<string, SyncDocumentToStorageJob, string>((recurringJobId, job, cronExpression) =>
                recurringCalls.Add((recurringJobId, job, cronExpression)));
        scheduler
            .Setup(s => s.EnqueueOnce(It.IsAny<SyncDocumentToStorageJob>(), It.IsAny<string>(), It.IsAny<TimeSpan>()));

        using var app = BuildApp(settings, scheduler.Object);

        // Act
        app.UseDocumentSyncJobs();

        // Assert
        recurringCalls.Count.ShouldBe(2);
        recurringCalls.ShouldContain(call =>
            call.RecurringJobId == "sync-document-bylaws" &&
            call.Job.DocumentName == "bylaws" &&
            call.Job.TriggeredBy == "scheduled" &&
            call.CronExpression == "0 5 1-7 * 1");
        recurringCalls.ShouldContain(call =>
            call.RecurringJobId == "sync-document-tournament-rules" &&
            call.Job.DocumentName == "tournament-rules" &&
            call.Job.TriggeredBy == "scheduled" &&
            call.CronExpression == "0 5 1-7 * 1");
    }

    [Fact(DisplayName = "UseDocumentSyncJobs should not schedule anything when no documents are configured")]
    public void UseDocumentSyncJobs_ShouldScheduleNothing_WhenNoDocumentsConfigured()
    {
        // Arrange
        var settings = CreateSettings();
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        using var app = BuildApp(settings, scheduler.Object);

        // Act & Assert - the Strict mock has no Setup for either scheduling method, so it
        // would throw if UseDocumentSyncJobs called AddOrUpdateRecurring/EnqueueOnce with no
        // documents configured.
        Should.NotThrow(() => app.UseDocumentSyncJobs());
    }
}
