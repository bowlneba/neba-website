var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DashboardApplicationName = "NEBA Website",
});

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(container => container
        .WithContainerName("bowlneba-postgres")
        .WithPgAdmin(pgAdmin => pgAdmin
            .WithContainerName("bowlneba-pgadmin")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithHostPort(19631))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHostPort(19630)
        .WithDataVolume("bowlneba-pgdata"));

var database = postgres.AddDatabase("bowlneba");
var cache = postgres.AddDatabase("bowlneba-cache");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithContainerName("bowlneba-storage")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("bowlneba-storage-data")
        .WithBlobPort(19632));

var blobs = storage.AddBlobs("blob");

var api = builder.AddProject<Projects.Neba_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithUrlForEndpoint("http", callback =>
    {
        callback.DisplayText = "Scalar API";
        callback.Url = "/scalar";
    })
    .WithUrls(context =>
    {
        var endpoint = context.GetEndpoint("http")
            ?? throw new InvalidOperationException("HTTP endpoint not found.");

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

    var keyVault = builder.AddAzureKeyVault("keyvault");

    api
        .WithReference(appInsights)
        .WithReference(keyVault);

    web
        .WithReference(appInsights);
}

await builder.Build().RunAsync();