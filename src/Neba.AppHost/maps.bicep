@description('Name of the Azure Maps account.')
param mapsAccountName string = 'neba-maps'

@description('Name of the existing Key Vault to write the Maps subscription key into.')
param keyVaultName string

@description('Unused: Azure Maps accounts are global. Declared because Aspire passes a location parameter to every Bicep module by convention.')
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Azure Maps accounts are a global (non-regional) resource.
resource mapsAccount 'Microsoft.Maps/accounts@2023-06-01' = {
  name: mapsAccountName
  location: 'global'
  kind: 'Gen2'
  sku: {
    name: 'G2'
  }
}

// Written directly here, rather than seeded from cd.yml like the other Key Vault secrets,
// because the subscription key only exists once the account itself has been provisioned.
resource mapsSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AzureMaps--SubscriptionKey'
  properties: {
    value: mapsAccount.listKeys().primaryKey
  }
}

output mapsAccountUniqueId string = mapsAccount.properties.uniqueId
