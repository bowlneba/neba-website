@description('Name of the existing Key Vault to create the bowler PII key in.')
param keyVaultName string

@description('Name of the RSA key that wraps the per-value AES keys for bowler PII.')
param keyName string = 'bowlerpii-key'

@description('Aspire passes this to every Bicep module, so the deploy fails without it. The key takes its region from the vault.')
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Only the api's managed identity may wrap and unwrap with it (Key Vault Crypto Service Encryption
// User on this vault, granted in AppHost.cs). A deploy does not add a version to an existing key;
// rotation is manual.
// Keep old versions enabled: each encrypted value names the version that decrypts it.
resource bowlerPiiKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
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

output keyUriWithVersion string = bowlerPiiKey.properties.keyUriWithVersion
// Uses the parameter, so analyzers do not report it as unused.
output deploymentLocation string = location
