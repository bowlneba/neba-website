using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Neba.TestFactory.Attributes;
using Xunit.v3;

namespace Neba.AppHost.Tests;

/// <summary>
/// Tests for the Aspire AppHost configuration to verify resource orchestration.
/// These tests validate the application model structure, ensuring all resources are properly
/// configured with correct dependencies, health checks, and startup ordering.
/// </summary>
[IntegrationTest]
[Component("AppHost")]
public sealed class AppModelTests
{
    [Fact(DisplayName = "Should configure all required resources in app model")]
    public async Task Should_Configure_All_Required_Resources()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert - Verify all expected resources exist
        var resources = appModel.Resources.Select(r => r.Name).ToList();

        resources.ShouldContain("postgres");
        resources.ShouldContain("neba-website");
        resources.ShouldContain("api");
        resources.ShouldContain("web");
    }

    [Fact(DisplayName = "Should configure PostgreSQL with data volume")]
    public async Task Should_Configure_PostgreSQL_With_DataVolume()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var postgres = appModel.Resources
            .OfType<PostgresServerResource>()
            .ShouldHaveSingleItem();

        postgres.Name.ShouldBe("postgres");

        // Verify data volume is configured
        postgres.Annotations
            .OfType<ContainerMountAnnotation>()
            .Where(m => m.Type == ContainerMountType.Volume && m.Source == "neba-website-data")
            .ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Should configure database resource with correct parent")]
    public async Task Should_Configure_Database_With_Correct_Parent()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var database = appModel.Resources
            .OfType<PostgresDatabaseResource>()
            .ShouldHaveSingleItem();

        database.Name.ShouldBe("neba-website");
        database.Parent.Name.ShouldBe("postgres");
    }

    [Fact(DisplayName = "Should configure API with health check and database dependency")]
    public async Task Should_Configure_API_With_HealthCheck_And_Database_Dependency()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var api = appModel.Resources
            .OfType<ProjectResource>()
            .Single(r => r.Name == "api");

        // Verify health check endpoint exists
        var healthCheck = api.Annotations
            .OfType<HealthCheckAnnotation>()
            .ShouldHaveSingleItem();

        healthCheck.ShouldNotBeNull();

        // Verify database reference
        var hasDbReference = api.Annotations
            .OfType<EnvironmentCallbackAnnotation>()
            .Any();

        hasDbReference.ShouldBeTrue();

        // Verify wait-for dependency on database
        var waitAnnotations = api.Annotations
            .OfType<WaitAnnotation>()
            .ToList();

        waitAnnotations.ShouldContain(w => w.Resource.Name == "neba-website");
    }

    [Fact(DisplayName = "Should configure web with external endpoints and correct dependencies")]
    public async Task Should_Configure_Web_With_External_Endpoints_And_Dependencies()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var web = appModel.Resources
            .OfType<ProjectResource>()
            .Single(r => r.Name == "web");

        // Verify endpoints are configured
        var endpoints = web.Annotations
            .OfType<EndpointAnnotation>()
            .ToList();

        endpoints.ShouldNotBeEmpty();

        // Verify health check
        var healthCheck = web.Annotations
            .OfType<HealthCheckAnnotation>()
            .ShouldHaveSingleItem();

        healthCheck.ShouldNotBeNull();

        // Verify wait-for dependencies on both database and API
        var waitAnnotations = web.Annotations
            .OfType<WaitAnnotation>()
            .ToList();

        waitAnnotations.ShouldContain(w => w.Resource.Name == "neba-website");
        waitAnnotations.ShouldContain(w => w.Resource.Name == "api");
    }

    [Fact(DisplayName = "Should ensure correct startup order through WaitFor dependencies")]
    public async Task Should_Ensure_Correct_Startup_Order()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var api = appModel.Resources.Single(r => r.Name == "api");
        var web = appModel.Resources.Single(r => r.Name == "web");

        // Assert - API waits for database
        var apiWaitsAnnotations = api.Annotations
            .OfType<WaitAnnotation>()
            .ToList();

        apiWaitsAnnotations.Select(w => w.Resource.Name)
            .ShouldContain("neba-website");

        // Assert - Web waits for both database and API
        var webWaitsAnnotations = web.Annotations
            .OfType<WaitAnnotation>()
            .ToList();

        var webWaitNames = webWaitsAnnotations.Select(w => w.Resource.Name);
        webWaitNames.ShouldContain("neba-website");
        webWaitNames.ShouldContain("api");

        // This ensures startup order: database → api → web
    }

    [Fact(DisplayName = "Should configure parameter for PostgreSQL username")]
    public async Task Should_Configure_Parameter_For_PostgreSQL_Username()
    {
        // Arrange & Act
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Neba_AppHost>(TestContext.Current.CancellationToken);
        await using var app = await appHost.BuildAsync(TestContext.Current.CancellationToken);
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var parameter = appModel.Resources
            .OfType<ParameterResource>()
            .Single(r => r.Name == "postgres-userName");

        parameter.ShouldNotBeNull();
        parameter.Secret.ShouldBeFalse();
    }
}
