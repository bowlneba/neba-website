using Aspire.Hosting.Azure;

using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.PostgreSql;
using Azure.Provisioning.Storage;

// WithDisplayOrder is experimental in Aspire 17.
#pragma warning disable ASPIRERESOURCETRAITS001

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
const string migrationServiceDirectory = "../Neba.MigrationService/";

var appMigrations = builder.AddDotnetProject("api-migrations", migrationServiceDirectory)
    .WithArgs("app")
    .WithReference(database)
    .WaitFor(database);

var securityMigrations = builder.AddDotnetProject("api-security-migrations", migrationServiceDirectory)
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

var api = builder.AddDotnetProject("api", "../Neba.Api/")
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
    // Aspire's default role for a referenced blob resource is Storage Blob Data Contributor,
    // which covers blob CRUD but not Set Container ACL (the call AzureBlobStorageService
    // makes to set a container's public access level) - that needs
    // containers/setacl/action, which only Storage Blob Data Owner grants. Table Data
    // Contributor is included explicitly because WithRoleAssignments replaces every default
    // role Aspire would otherwise assign to "storage" for this project, not just the blob one.
    .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataOwner, StorageBuiltInRole.StorageTableDataContributor)
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

#pragma warning disable ASPIREBROWSERLOGS001
var web = builder.AddDotnetProject("web", "../Neba.Website.Server/")
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

    // Deploy-time values arrive as Aspire parameters: set Parameters__<name> environment
    // variables (CI maps them from the AZURE_* repo vars) or user secrets. A required
    // parameter rejects an empty value.

    // Postgres compute (currently Standard_B1ms/Burstable) comes from parameters so switching
    // tiers - e.g. downgrading during an off-season "dark" period - is a config change, not a
    // code change.
    var postgresSkuName = builder.AddParameter("postgresSkuName");
    var postgresSkuTier = builder.AddParameter("postgresSkuTier");

    postgres.ConfigureInfrastructure(infra =>
    {
        var server = infra.GetProvisionableResources().OfType<PostgreSqlFlexibleServer>().Single();
        server.Sku = new PostgreSqlFlexibleServerSku
        {
            Name = postgresSkuName.AsProvisioningParameter(infra, "postgresSkuName"),
            Tier = new BicepValue<PostgreSqlFlexibleServerSkuTier>(
                ((BicepValue<string>)postgresSkuTier.AsProvisioningParameter(infra, "postgresSkuTier")).Compile())
        };

        // Storage isn't part of the seasonal on/off toggle above, so it's pinned here rather
        // than pulled from a parameter - it can't be scaled down once set, so there's no "dark
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
    // vault via `az keyvault secret set`, using the same service principal that runs the deploy.
    // That principal only holds subscription-level Contributor/User Access Administrator,
    // neither of which grants Key Vault data-plane access on an RBAC-authorized vault - so
    // without an explicit data role here, the seed step fails with 403 Forbidden.
    var deployPrincipalId = builder.AddParameter("deployPrincipalId");

    keyVault.ConfigureInfrastructure(infra =>
    {
        var vault = infra.GetProvisionableResources().OfType<KeyVaultService>().Single();
        var roleAssignment = vault.CreateRoleAssignment(
            KeyVaultBuiltInRole.KeyVaultSecretsOfficer,
            RoleManagementPrincipalType.ServicePrincipal,
            new BicepValue<Guid>(((BicepValue<string>)deployPrincipalId.AsProvisioningParameter(infra, "deployPrincipalId")).Compile()),
            "deploy_pipeline");
        infra.Add(roleAssignment);
    });

    // Aspire's default Storage Account bicep sets AllowBlobPublicAccess to false. That's an
    // account-level gate: even though bowlneba-public is created with PublicAccessType.Blob
    // (AzureBlobStorageService), the account itself refuses anonymous reads until this is
    // true, causing "Public access is not permitted on this storage account" (409
    // PublicAccessNotPermitted) on every request for a public blob (e.g. tournament logos).
    // Individual container-level access is still governed by AzureBlobStorageService - this
    // only lifts the account-level block that would otherwise override it.
    storage.ConfigureInfrastructure(infra =>
    {
        var account = infra.GetProvisionableResources().OfType<StorageAccount>().Single();
        account.AllowBlobPublicAccess = true;
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

    // Replica scaling (currently 1/10/50) comes from parameters so adjusting capacity - e.g.
    // scaling to zero during an off-season "dark" period - is a config change, not a code change.
    var minReplicas = builder.AddParameter("minReplicas");
    var maxReplicas = builder.AddParameter("maxReplicas");
    var concurrentRequests = builder.AddParameter("concurrentRequests");

    Action<AzureResourceInfrastructure, ContainerApp> configureScale = (infrastructure, app) =>
    {
        app.Template.Scale.MinReplicas = ToBicepInt(minReplicas.AsProvisioningParameter(infrastructure, "minReplicas"));
        app.Template.Scale.MaxReplicas = ToBicepInt(maxReplicas.AsProvisioningParameter(infrastructure, "maxReplicas"));
        app.Template.Scale.Rules.Add(new ContainerAppScaleRule
        {
            Name = "http-concurrency",
            Http = new ContainerAppHttpScaleRule
            {
                Metadata = { ["concurrentRequests"] = concurrentRequests.AsProvisioningParameter(infrastructure, "concurrentRequests") }
            }
        });
    };

    // Custom domain + managed certificate binding, codified so it survives every future
    // `azd provision` (previously these were only bound by hand in the Portal, and a fresh
    // provision wiped them out - see issue #116). Certificate name parameters start empty:
    // the first deploy after adding a new domain binds the hostname only (BindingType
    // Disabled, no cert), which is enough for Azure to validate the DNS records. Once
    // validated, provision a free managed certificate for that hostname via the Portal and
    // feed its name back in via the certificate-name parameter (AZURE_*_CERTIFICATE_NAME repo var) - that permanently
    // binds it (BindingType SniEnabled) from then on. See
    var webCustomDomain = builder.AddParameter("webCustomDomain");
    var webCertificateName = builder.AddParameter("webCertificateName").AsOptional();
    var apiCustomDomain = builder.AddParameter("apiCustomDomain");
    var apiCertificateName = builder.AddParameter("apiCertificateName").AsOptional();

    // Optional second hostname on the web app (www.<apex>). The app redirects www to the apex,
    // so www must reach the container app over HTTPS, which needs its own binding and managed
    // certificate. Same two-step flow as above; leave Parameters__webWwwCustomDomain unset to skip.
    IResourceBuilder<ParameterResource>? webWwwCustomDomain = null;
    IResourceBuilder<ParameterResource>? webWwwCertificateName = null;

    if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:webWwwCustomDomain"]))
    {
        webWwwCustomDomain = builder.AddParameter("webWwwCustomDomain");
        webWwwCertificateName = builder.AddParameter("webWwwCertificateName").AsOptional();
    }

#pragma warning disable ASPIREACADOMAINS001
    api.PublishAsAzureContainerApp(configureScale + ((_, app) => app.ConfigureCustomDomain(apiCustomDomain, apiCertificateName)));
    web.PublishAsAzureContainerApp(configureScale + ((_, app) =>
    {
        app.ConfigureCustomDomain(webCustomDomain, webCertificateName);

        if (webWwwCustomDomain is not null && webWwwCertificateName is not null)
        {
            app.ConfigureCustomDomain(webWwwCustomDomain, webWwwCertificateName);
        }
    }));
#pragma warning restore ASPIREACADOMAINS001

    // Manually-triggered jobs, not started automatically by azd deploy - cd.yml deploys these
    // two job images first, triggers each with `az containerapp job start`, and waits for them
    // to finish before deploying api/web, so the schema is migrated before new code takes traffic.
    appMigrations.PublishAsAzureContainerAppJob();
    securityMigrations.PublishAsAzureContainerAppJob();
}
else
{
    var mailpit = builder.AddMailPit("mailpit")
        .WithDisplayOrder(-10);

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}

// Dashboard order, highest first: the apps you open, then migrations, then backing services.
// Resources without a value (pgAdmin, blob and table children) sort after these by type and name.
web.WithDisplayOrder(50);
api.WithDisplayOrder(40);
appMigrations.WithDisplayOrder(30);
securityMigrations.WithDisplayOrder(20);
postgres.WithDisplayOrder(10);
storage.WithDisplayOrder(10);

await builder.Build().RunAsync();

// Container App scale values are ints, but Aspire parameters are strings: convert in Bicep.
static BicepValue<int> ToBicepInt(BicepValue<string> value)
    => new(new FunctionCallExpression(new IdentifierExpression("int"), value.Compile()));