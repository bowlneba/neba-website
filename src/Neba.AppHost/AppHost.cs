var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(container => container
        .WithPgAdmin());

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
    .WithReference(database)
    .WaitFor(database)
    .WithReference(api)
    .WaitFor(api)
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

if (builder.ExecutionContext.IsPublishMode)
{
    var appInsights = builder.AddAzureApplicationInsights("appinsights");
    api.WithReference(appInsights);
    web.WithReference(appInsights);
}

await builder.Build().RunAsync();