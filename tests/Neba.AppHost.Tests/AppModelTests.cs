using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;

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
    [Fact(DisplayName = "All required resources should be configured in app model")]
    public async Task AppModel_ShouldIncludeAllRequiredResources_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "PostgreSQL should be configured with data volume")]
    public async Task PostgreSQL_ShouldHaveDataVolume_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "Database resource should be linked to PostgreSQL parent")]
    public async Task Database_ShouldHavePostgresAsParent_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "API should have health check and wait for database")]
    public async Task API_ShouldHaveHealthCheckAndDatabaseDependency_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "Web should have endpoints and wait for database and API")]
    public async Task Web_ShouldHaveEndpointsAndCorrectDependencies_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "Startup order should be database, then API, then web")]
    public async Task StartupOrder_ShouldBeDatabase_ThenAPI_ThenWeb_WhenDistributedApplicationIsBuilt()
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

    [Fact(DisplayName = "PostgreSQL username should be configured as a parameter")]
    public async Task PostgreSQLUsername_ShouldBeConfiguredAsParameter_WhenDistributedApplicationIsBuilt()
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
