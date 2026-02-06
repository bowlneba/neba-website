using Azure.Provisioning.AppService;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("psql-bowlneba")
    .RunAsContainer(container => container
        .WithPgAdmin()
        .WithDataVolume("bowlneba-website-data"));

var database = postgres.AddDatabase("bowlneba-db");

builder.AddAzureAppServiceEnvironment("bowlneba")
    .ConfigureInfrastructure(infrastructure =>
    {
        var plan = infrastructure.GetProvisionableResources()
            .OfType<AppServicePlan>()
            .Single();

        plan.Sku = new AppServiceSkuDescription
        {
            Name = Environment.GetEnvironmentVariable("AZURE_ASP_SKU_NAME") ?? "B1",
            Tier = Environment.GetEnvironmentVariable("AZURE_ASP_SKU_TIER") ?? "Basic"
        };
    });

var apiService = builder.AddProject<Projects.Neba_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .PublishAsAzureAppServiceWebsite((_, site) =>
    {
        site.SiteConfig.IsAlwaysOn = true;
        site.SiteConfig.IsHttp20Enabled = true;
    })
    .WithUrls(context =>
    {
        var endpoint = context.GetEndpoint("http")
            ?? throw new InvalidOperationException("HTTP endpoint not found.");

        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/scalar",
            DisplayText = "Scalar API Docs"
        });

        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/background-jobs",
            DisplayText = "Hangfire Dashboard"
        });
    });

builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(apiService)
    .WaitFor(apiService)
    .PublishAsAzureAppServiceWebsite((_, site) =>
    {
        site.SiteConfig.IsAlwaysOn = true;
        site.SiteConfig.IsHttp20Enabled = true;
    })
    .WithUrls(context =>
    {
        var endpoint = context.GetEndpoint("http")
            ?? throw new InvalidOperationException("HTTP endpoint not found.");

        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/admin/background-jobs",
            DisplayText = "Hangfire Dashboard"
        });
    });

await builder.Build().RunAsync();