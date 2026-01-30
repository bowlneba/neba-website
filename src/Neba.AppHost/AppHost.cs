var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-userName", "neba-db-user");

var postgres = builder.AddPostgres("postgres")
    .WithUserName(postgresUser)
    .WithPgAdmin()
    .WithDataVolume("neba-website-data");

var database = postgres.AddDatabase("neba-website");

var apiService = builder.AddProject<Projects.Neba_Api>("api")
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

builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(apiService)
    .WaitFor(apiService)
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