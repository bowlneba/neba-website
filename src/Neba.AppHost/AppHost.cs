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

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithContainerName("bowlneba-storage")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("bowlneba-storage-data")
        .WithBlobPort(19632)
        .WithQueuePort(19633)
        .WithTablePort(19634));

var blobs = storage.AddBlobs("blob");
var tables = storage.AddTables("tables");

var api = builder.AddProject<Projects.Neba_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(tables)
    .WaitFor(tables)
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

#if DEBUG
        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = $"{endpoint.Url}/debug/cache",
            DisplayText = "Clear Cache"
        });
#endif
    });

#pragma warning disable ASPIREBROWSERLOGS001
var web = builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api)
    .WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

if (builder.ExecutionContext.IsPublishMode)
{
    var workspace = builder.AddAzureLogAnalyticsWorkspace("logs");
    var appInsights = builder.AddAzureApplicationInsights("appinsights")
        .WithLogAnalyticsWorkspace(workspace);

    var keyVault = builder.AddAzureKeyVault("keyvault");

    api
        .WithReference(appInsights)
        .WithReference(keyVault);

    // No Aspire.Hosting.Azure.Maps / Azure.Provisioning.Maps package exists, so the Maps
    // account is provisioned via a handwritten Bicep template. See maps.bicep and issue #28
    // for why this stays on subscription-key auth (via Key Vault) instead of managed identity.
    var maps = builder.AddBicepTemplate("maps", "maps.bicep")
        .WithParameter("keyVaultName", keyVault.Resource.NameOutputReference);

    web
        .WithReference(appInsights)
        .WithReference(keyVault)
        .WithEnvironment("AzureMaps__AccountId", maps.GetOutput("mapsAccountUniqueId"));
}
else
{
    var mailpit = builder.AddMailPit("mailpit");

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}

await builder.Build().RunAsync();