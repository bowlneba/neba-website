using System.ComponentModel.DataAnnotations;

using Neba.Infrastructure.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireSettingsValidationTests
{
    [Fact(DisplayName = "Valid settings pass validation")]
    public void Validate_WithValidValues_Succeeds()
    {
        // Arrange
        var settings = new HangfireSettings
        {
            WorkerCount = 10,
            SucceededJobsRetentionDays = 30,
            DeletedJobsRetentionDays = 30,
            FailedJobsRetentionDays = 30,
        };

        // Act
        bool isValid = TryValidate(settings, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Invalid worker count fails validation")]
    [InlineData(0, "WorkerCount must be between 1 and 100.", TestDisplayName = "WorkerCount below minimum")]
    [InlineData(101, "WorkerCount must be between 1 and 100.", TestDisplayName = "WorkerCount above maximum")]
    public void Validate_WithInvalidWorkerCount_Fails(int workerCount, string expectedMessage)
    {
        // Arrange
        var settings = new HangfireSettings
        {
            WorkerCount = workerCount,
            SucceededJobsRetentionDays = 30,
            DeletedJobsRetentionDays = 30,
            FailedJobsRetentionDays = 30,
        };

        // Act
        bool isValid = TryValidate(settings, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.ErrorMessage == expectedMessage);
    }

    [Theory(DisplayName = "Invalid retention days fails validation")]
    [InlineData(0, 30, 30, "SucceededJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Succeeded retention below minimum")]
    [InlineData(400, 30, 30, "SucceededJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Succeeded retention above maximum")]
    [InlineData(30, 0, 30, "DeletedJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Deleted retention below minimum")]
    [InlineData(30, 400, 30, "DeletedJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Deleted retention above maximum")]
    [InlineData(30, 30, 0, "FailedJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Failed retention below minimum")]
    [InlineData(30, 30, 400, "FailedJobsRetentionDays must be between 1 and 365.", TestDisplayName = "Failed retention above maximum")]
    public void Validate_WithInvalidRetentionDays_Fails(
        int succeededDays,
        int deletedDays,
        int failedDays,
        string expectedMessage)
    {
        // Arrange
        var settings = new HangfireSettings
        {
            WorkerCount = 10,
            SucceededJobsRetentionDays = succeededDays,
            DeletedJobsRetentionDays = deletedDays,
            FailedJobsRetentionDays = failedDays,
        };

        // Act
        bool isValid = TryValidate(settings, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.ErrorMessage == expectedMessage);
    }

    private static bool TryValidate(HangfireSettings settings, out List<ValidationResult> results)
    {
        var context = new ValidationContext(settings);
        results = [];
        return Validator.TryValidateObject(settings, context, results, validateAllProperties: true);
    }
}
