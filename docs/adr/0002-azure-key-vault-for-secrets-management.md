# ADR-0002: Azure Key Vault for Secrets Management

## Status

Proposed

## Context

The NEBA Website application requires sensitive configuration values that must not be stored in source control. Currently these include Google Drive service account credentials (PrivateKey, PrivateKeyId, ClientEmail). In the near future, the application will need to handle PII encryption (SSNs), which requires managed encryption keys.

### Current Approach

- **Local development**: .NET User Secrets (`secrets.json`) provide sensitive values that override the `null` placeholders in `appsettings.json`
- **Production**: No secrets management solution is in place yet — Google Drive integration is not yet wired into DI
- **Deployment**: `azd up` runs via GitHub Actions with OIDC authentication (no long-lived credentials)

### Constraints

- Deployment is fully automated via GitHub Actions (`azd provision` + `azd deploy`)
- The application uses .NET Aspire for orchestration and Azure Container Apps as the hosting platform
- PostgreSQL credentials are already handled via Azure Managed Identity (no password in connection string)
- Application Insights is provisioned by Aspire in publish mode
- Future requirements include encryption key management for PII (SSNs)

### Options Considered

1. **Azure Key Vault (via Aspire hosting integration)**
   - Aspire provisions the Key Vault via Bicep on `azd up`
   - API project uses `AddAzureKeyVaultSecrets()` as a configuration provider
   - Secrets layer into `IConfiguration` — existing settings classes bind without code changes
   - No local emulator exists; local dev continues using User Secrets
   - Supports both secrets and encryption keys (for future SSN encryption)

2. **Container App secrets via `AddParameter(secret: true)`**
   - Aspire-native approach using parameterized secrets
   - Secrets stored in Container App's built-in secret store
   - Values provided via `azd env set` in GitHub Actions
   - No additional Azure resources required
   - Does **not** support encryption keys — only configuration values

3. **Environment variables via `azd env set`**
   - Simplest approach — set env vars in GitHub Actions before `azd up`
   - No Aspire integration needed
   - Secrets stored in Container App environment
   - Does **not** support encryption keys or centralized secret management

## Decision

We will use **Azure Key Vault** (Option 1) as the centralized secrets management solution for all environments.

### Rationale

- **Encryption key support**: Key Vault is the only option that supports both secrets and cryptographic keys. Since SSN encryption is a near-term requirement, Key Vault will be needed regardless — establishing it now avoids a future migration.
- **Centralized management**: All secrets live in one place with audit logging, access policies, and rotation capabilities.
- **Configuration provider model**: `AddAzureKeyVaultSecrets()` layers into `IConfiguration`, so existing settings classes (`GoogleDriveSettings`, future settings) require no code changes.
- **Aspire integration**: Key Vault is provisioned declaratively in the AppHost alongside other Azure resources, keeping infrastructure-as-code consistent.

### Secret Naming Convention

Key Vault secret names do not support `:` or `__` as hierarchical separators. Use `--` (double hyphen) as the separator, which .NET's Key Vault configuration provider translates to `:` automatically.

Example: `GoogleDrive:Credentials:PrivateKey` → Key Vault secret name `GoogleDrive--Credentials--PrivateKey`

### Secrets to Store

| Secret Name | Key Vault Name | Source |
| --- | --- | --- |
| `GoogleDrive:Credentials:PrivateKey` | `GoogleDrive--Credentials--PrivateKey` | Google Cloud service account |
| `GoogleDrive:Credentials:PrivateKeyId` | `GoogleDrive--Credentials--PrivateKeyId` | Google Cloud service account |
| `GoogleDrive:Credentials:ClientEmail` | `GoogleDrive--Credentials--ClientEmail` | Google Cloud service account |

> **Note**: `GoogleDrive:Credentials:ProjectId` and `GoogleDrive:ApplicationName` are not sensitive and remain in `appsettings.json`. PostgreSQL and Application Insights connection strings are managed by Aspire/Managed Identity and do not need Key Vault.

### Deployment Flow

1. **GitHub Actions** provisions Key Vault via `azd provision` (Aspire generates Bicep)
2. **GitHub Actions** writes secrets to Key Vault using `az keyvault secret set` (values sourced from GitHub repository secrets)
3. **`azd deploy`** deploys the application — the API reads secrets from Key Vault at startup via the configuration provider
4. **Managed Identity** grants the Container App access to Key Vault (Aspire configures role assignments automatically)

### Local Development

No change — User Secrets (`secrets.json`) continue to provide sensitive values locally. Key Vault is not accessed during local development. The configuration provider is only registered in publish mode or can be conditionally added.

## Consequences

### Positive

- **Single secret store** for configuration secrets and future encryption keys
- **Audit trail** via Key Vault access logs
- **No secrets in CI/CD pipeline** beyond the initial seeding step — runtime access uses Managed Identity
- **Existing code unchanged** — `GoogleDriveSettings` binds from `IConfiguration` regardless of provider
- **Secret rotation** possible without redeployment (Key Vault configuration provider supports polling)
- **Consistent with Aspire patterns** — provisioned alongside other Azure resources

### Negative

- **Additional Azure resource** with minor cost (~$0.03/10K operations)
- **No local emulator** — cannot test Key Vault integration locally without a real instance
- **Initial setup complexity** — requires GitHub Actions changes to seed secrets into Key Vault
- **Cold start impact** — configuration provider makes a network call to Key Vault on startup

### Mitigation Strategies

- **Local dev gap**: User Secrets provide identical configuration shape; integration tests can mock `IConfiguration`
- **Startup latency**: Key Vault secrets are cached after first load; reload interval can be tuned
- **Secret seeding**: GitHub Actions step is a one-time addition to `cd.yml`; subsequent secret updates use the same `az keyvault secret set` command

## Related Decisions

- [ADR-0001](0001-container-apps-revision-mode-and-cost-management.md): Container Apps deployment model (Key Vault access is via the Container App's Managed Identity)
