@description('Name of the existing Key Vault to create the Data Protection encryption key in.')
param keyVaultName string

@description('Name of the RSA key used to encrypt the shared ASP.NET Core Data Protection key ring.')
param keyName string = 'dataprotection-key'

@description('Unused: Key Vault keys are not a regional resource in this template. Declared because Aspire passes a location parameter to every Bicep module by convention.')
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Wraps (encrypts) the shared Data Protection key ring both apps persist to Blob Storage - see
// StorageConfiguration.AddSharedDataProtection / InfrastructureConfiguration.AddSharedDataProtection.
// Both apps' managed identities need the Key Vault Crypto User role on this vault (granted via
// WithRoleAssignments in AppHost.cs) to wrap/unwrap with it.
resource dataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: keyName
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
  }
}

output keyUri string = dataProtectionKey.properties.keyUriWithVersion
