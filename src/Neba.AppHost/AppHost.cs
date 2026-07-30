using System.Globalization;

using Aspire.Hosting.Azure;

using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.PostgreSql;

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
    builder.AddAzureContainerAppEnvironment("env");

    var workspace = builder.AddAzureLogAnalyticsWorkspace("logs");
    var appInsights = builder.AddAzureApplicationInsights("appinsights")
        .WithLogAnalyticsWorkspace(workspace);

    // Postgres compute comes from the AZURE_POSTGRES_SKU_* repo vars (currently
    // Standard_B2s/Burstable) so switching tiers - e.g. downgrading to Standard_B1ms during a
    // off-season "dark" period - is a config change, not code change. See
    // .github/workflows/infrastructure_preview.yml and cd.yml for where these are supplied.
    var postgresSkuName = Environment.GetEnvironmentVariable("AZURE_POSTGRES_SKU_NAME")
        ?? throw new InvalidOperationException("AZURE_POSTGRES_SKU_NAME environment variable is not set.");
    var postgresSkuTier = Enum.Parse<PostgreSqlFlexibleServerSkuTier>(
        Environment.GetEnvironmentVariable("AZURE_POSTGRES_SKU_TIER")
            ?? throw new InvalidOperationException("AZURE_POSTGRES_SKU_TIER environment variable is not set."));

    postgres.ConfigureInfrastructure(infra =>
    {
        var server = infra.GetProvisionableResources().OfType<PostgreSqlFlexibleServer>().Single();
        server.Sku = new PostgreSqlFlexibleServerSku
        {
            Name = postgresSkuName,
            Tier = postgresSkuTier
        };
    });

    var keyVault = builder.AddAzureKeyVault("keyvault");

    // The CD pipeline's "Seed Key Vault secrets" step (cd.yml) writes secrets directly into this
    // vault via `az keyvault secret set`, using the same service principal that runs `azd
    // provision`/`azd deploy`. That principal only holds subscription-level Contributor/User
    // Access Administrator, neither of which grants Key Vault data-plane access on an
    // RBAC-authorized vault - so without an explicit data role here, the seed step fails with
    // 403 Forbidden.
    var deployPrincipalId = Environment.GetEnvironmentVariable("AZURE_PRINCIPAL_ID")
        ?? throw new InvalidOperationException("AZURE_PRINCIPAL_ID environment variable is not set.");

    keyVault.ConfigureInfrastructure(infra =>
    {
        var vault = infra.GetProvisionableResources().OfType<KeyVaultService>().Single();
        var roleAssignment = vault.CreateRoleAssignment(
            KeyVaultBuiltInRole.KeyVaultSecretsOfficer,
            RoleManagementPrincipalType.ServicePrincipal,
            Guid.Parse(deployPrincipalId),
            "deploy-pipeline");
        infra.Add(roleAssignment);
    });

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

    // Replica scaling comes from the AZURE_CONTAINERAPP_* repo vars (currently 1/10/50) so
    // adjusting capacity - e.g. scaling to zero during an off-season "dark" period - is a
    // config change, not a code change. See .github/workflows/infrastructure_preview.yml and
    // cd.yml for where these are supplied.
    var minReplicas = int.Parse(
        Environment.GetEnvironmentVariable("AZURE_CONTAINERAPP_MIN_REPLICAS")
            ?? throw new InvalidOperationException("AZURE_CONTAINERAPP_MIN_REPLICAS environment variable is not set."),
        CultureInfo.InvariantCulture);
    var maxReplicas = int.Parse(
        Environment.GetEnvironmentVariable("AZURE_CONTAINERAPP_MAX_REPLICAS")
            ?? throw new InvalidOperationException("AZURE_CONTAINERAPP_MAX_REPLICAS environment variable is not set."),
        CultureInfo.InvariantCulture);
    var concurrentRequests = Environment.GetEnvironmentVariable("AZURE_CONTAINERAPP_CONCURRENT_REQUESTS")
        ?? throw new InvalidOperationException("AZURE_CONTAINERAPP_CONCURRENT_REQUESTS environment variable is not set.");

    void ConfigureScale(AzureResourceInfrastructure infra, ContainerApp app)
    {
        app.Template.Scale.MinReplicas = minReplicas;
        app.Template.Scale.MaxReplicas = maxReplicas;
        app.Template.Scale.Rules.Add(new ContainerAppScaleRule
        {
            Name = "http-concurrency",
            Http = new ContainerAppHttpScaleRule
            {
                Metadata = { ["concurrentRequests"] = concurrentRequests }
            }
        });
    }

    api.PublishAsAzureContainerApp(ConfigureScale);
    web.PublishAsAzureContainerApp(ConfigureScale);
}
else
{
    var mailpit = builder.AddMailPit("mailpit");

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}

await builder.Build().RunAsync();