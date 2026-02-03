using Hangfire;
using Hangfire.PostgreSql;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.BackgroundJobs;

namespace Neba.Website.Tests.BackgroundJobs;

[UnitTest]
[Component("Website.BackgroundJobs")]
public sealed class BackgroundJobsExtensionsTests
{
    [Fact(DisplayName = "Should not add Hangfire when connection string is missing")]
    public void AddBackgroundJobs_ShouldNotAddHangfire_WhenConnectionStringIsNull()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        var result = services.AddBackgroundJobs(config);

        // Assert
        result.ShouldBe(services);
        services.FirstOrDefault(sd => sd.ServiceType == typeof(JobStorage)).ShouldBeNull();
    }

    [Fact(DisplayName = "Should add Hangfire when connection string is provided")]
    public void AddBackgroundJobs_ShouldAddHangfire_WhenConnectionStringIsProvided()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:neba-website", "Host=localhost;Username=postgres;Password=password;Database=test" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var result = services.AddBackgroundJobs(config);

        // Assert
        result.ShouldBe(services);
        services.FirstOrDefault(sd => sd.ServiceType == typeof(JobStorage)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should not add Hangfire when connection string is empty")]
    public void AddBackgroundJobs_ShouldNotAddHangfire_WhenConnectionStringIsEmpty()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:neba-website", "" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var result = services.AddBackgroundJobs(config);

        // Assert
        result.ShouldBe(services);
        services.FirstOrDefault(sd => sd.ServiceType == typeof(JobStorage)).ShouldBeNull();
    }

    [Fact(DisplayName = "Should use Hangfire dashboard when JobStorage is available")]
    public void UseBackgroundJobsDashboard_ShouldConfigureDashboard_WhenJobStorageExists()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var mockJobStorage = new Mock<JobStorage>(MockBehavior.Strict);

        builder.Services.AddSingleton(mockJobStorage.Object);
        builder.Services.AddHangfire((config) => config.UseStorage(mockJobStorage.Object));

        var app = builder.Build();

        // Act
        var result = app.UseBackgroundJobsDashboard();

        // Assert
        result.ShouldBe(app);
    }

    [Fact(DisplayName = "Should not configure dashboard when JobStorage is not available")]
    public void UseBackgroundJobsDashboard_ShouldNotConfigureDashboard_WhenJobStorageIsNull()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();

        // Act
        var result = app.UseBackgroundJobsDashboard();

        // Assert
        result.ShouldBe(app);
    }
}