using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DashboardApplicationName = "NEBA Website",
});

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(container => container
        .WithPgAdmin(pgAdmin => pgAdmin
            .WithLifetime(ContainerLifetime.Persistent)
            .WithHostPort(19631))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHostPort(19630)
        .WithEnvironment("POSTGRES_USER", builder.Configuration.GetValue<string>("Postgres:UserName") ?? throw new InvalidOperationException("Postgres username not configured."))
        .WithEnvironment("POSTGRES_PASSWORD", builder.Configuration.GetValue<string>("Postgres:Password") ?? throw new InvalidOperationException("Postgres password not configured."))
        .WithDataVolume("bowlneba-data"));

var database = postgres.AddDatabase("bowlneba");

var api = builder.AddProject<Projects.Neba_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
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

var web = builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);

if (builder.ExecutionContext.IsPublishMode)
{
    var workspace = builder.AddAzureLogAnalyticsWorkspace("logs");
    var appInsights = builder.AddAzureApplicationInsights("appinsights")
        .WithLogAnalyticsWorkspace(workspace);

    api.WithReference(appInsights);
    web.WithReference(appInsights);
}

await builder.Build().RunAsync();