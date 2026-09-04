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

// Runs Neba.Api's EF Core migrations against a dedicated DbContext. Each is a one-shot
// process that exits after applying pending migrations, using the same managed-identity
// Postgres auth as the api - so no DB credentials are needed here or in CD. See
// .github/workflows/cd.yml for how these are triggered as Azure Container App Jobs.
var appMigrations = builder.AddProject<Projects.Neba_MigrationService>("api-migrations")
    .WithArgs("app")
    .WithReference(database)
    .WaitFor(database);

var securityMigrations = builder.AddProject<Projects.Neba_MigrationService>("api-security-migrations")
    .WithArgs("security")
    .WithReference(database)
    .WaitFor(database);

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
    .WaitForCompletion(appMigrations)
    .WaitForCompletion(securityMigrations)
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
    // Shares the same blob storage account as the api, so both apps can persist Data Protection
    // keys to the same container and decrypt each other's cookie-auth tickets (see
    // AccountConfiguration.cs / SecurityConfiguration.cs) - lets a signed-in Webmaster navigate
    // straight to the api's /background-jobs Hangfire dashboard without a separate token.
    .WithReference(blobs)
    .WaitFor(blobs)
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

        // Storage isn't part of the seasonal on/off toggle above, so it's pinned here rather
        // than pulled from a repo var - it can't be scaled down once set, so there's no "dark
        // period" value to switch to/from. 32 GiB / P6 (240 IOPS) / no autogrow.
        server.Storage = new PostgreSqlFlexibleServerStorage
        {
            StorageSizeInGB = 32,
            Tier = PostgreSqlManagedDiskPerformanceTier.P6,
            AutoGrow = StorageAutoGrow.Disabled
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
            "deploy_pipeline");
        infra.Add(roleAssignment);
    });

    // RSA key that wraps (encrypts) the shared Data Protection key ring both apps persist to
    // Blob Storage - see StorageConfiguration.AddSharedDataProtection /
    // InfrastructureConfiguration.AddSharedDataProtection. No typed Aspire/Azure.Provisioning
    // resource exists for Key Vault keys (only secrets), so - same as maps.bicep below - this
    // is a handwritten Bicep template.
    var dataProtectionKey = builder.AddBicepTemplate("dataprotectionkey", "dataprotectionkey.bicep")
        .WithParameter("keyVaultName", keyVault.Resource.NameOutputReference);

    api
        .WithReference(appInsights)
        .WithReference(keyVault)
        .WithRoleAssignments(keyVault, KeyVaultBuiltInRole.KeyVaultSecretsUser, KeyVaultBuiltInRole.KeyVaultCryptoUser)
        .WithEnvironment("DataProtection__KeyVaultKeyUri", dataProtectionKey.GetOutput("keyUri"));

    // No Aspire.Hosting.Azure.Maps / Azure.Provisioning.Maps package exists, so the Maps
    // account is provisioned via a handwritten Bicep template. See maps.bicep and issue #28
    // for why this stays on subscription-key auth (via Key Vault) instead of managed identity.
    var maps = builder.AddBicepTemplate("maps", "maps.bicep")
        .WithParameter("keyVaultName", keyVault.Resource.NameOutputReference);

    web
        .WithReference(appInsights)
        .WithReference(keyVault)
        .WithRoleAssignments(keyVault, KeyVaultBuiltInRole.KeyVaultSecretsUser, KeyVaultBuiltInRole.KeyVaultCryptoUser)
        .WithEnvironment("AzureMaps__AccountId", maps.GetOutput("mapsAccountUniqueId"))
        .WithEnvironment("DataProtection__KeyVaultKeyUri", dataProtectionKey.GetOutput("keyUri"));

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

    // Custom domain + managed certificate binding, codified so it survives every future
    // `azd provision` (previously these were only bound by hand in the Portal, and a fresh
    // provision wiped them out - see issue #116). Certificate name parameters start empty:
    // the first deploy after adding a new domain binds the hostname only (BindingType
    // Disabled, no cert), which is enough for Azure to validate the DNS records. Once
    // validated, provision a free managed certificate for that hostname via the Portal and
    // feed its name back in via the AZURE_*_CERTIFICATE_NAME repo var - that permanently
    // binds it (BindingType SniEnabled) from then on. See
    // .github/workflows/infrastructure_preview.yml and cd.yml for where these are supplied.
    var webCustomDomain = builder.AddParameter(
        "webCustomDomain",
        Environment.GetEnvironmentVariable("AZURE_WEB_CUSTOM_DOMAIN")
            ?? throw new InvalidOperationException("AZURE_WEB_CUSTOM_DOMAIN environment variable is not set."),
        publishValueAsDefault: true);
    var webCertificateName = builder.AddParameter(
        "webCertificateName",
        Environment.GetEnvironmentVariable("AZURE_WEB_CERTIFICATE_NAME") ?? string.Empty,
        publishValueAsDefault: true);
    var apiCustomDomain = builder.AddParameter(
        "apiCustomDomain",
        Environment.GetEnvironmentVariable("AZURE_API_CUSTOM_DOMAIN")
            ?? throw new InvalidOperationException("AZURE_API_CUSTOM_DOMAIN environment variable is not set."),
        publishValueAsDefault: true);
    var apiCertificateName = builder.AddParameter(
        "apiCertificateName",
        Environment.GetEnvironmentVariable("AZURE_API_CERTIFICATE_NAME") ?? string.Empty,
        publishValueAsDefault: true);

    Action<AzureResourceInfrastructure, ContainerApp> configureScale = ConfigureScale;

#pragma warning disable ASPIREACADOMAINS001
    api.PublishAsAzureContainerApp(configureScale + ((_, app) => app.ConfigureCustomDomain(apiCustomDomain, apiCertificateName)));
    web.PublishAsAzureContainerApp(configureScale + ((_, app) => app.ConfigureCustomDomain(webCustomDomain, webCertificateName)));
#pragma warning restore ASPIREACADOMAINS001

    // Manually-triggered jobs, not started automatically by azd deploy - cd.yml deploys these
    // two job images first, triggers each with `az containerapp job start`, and waits for them
    // to finish before deploying api/web, so the schema is migrated before new code takes traffic.
    appMigrations.PublishAsAzureContainerAppJob();
    securityMigrations.PublishAsAzureContainerAppJob();
}
else
{
    var mailpit = builder.AddMailPit("mailpit");

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}

await builder.Build().RunAsync();