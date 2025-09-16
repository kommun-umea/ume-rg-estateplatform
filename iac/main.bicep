// ------------ Parameters ------------
@allowed([
  'dev'
  'test'
  'prod'
])
@description('The current environment.')
param environment string

@description('The company prefix to use in resource names.')
param companyPrefix string

@description('The purpose of this deployment to use in resource names.')
param purpose string

@description('The default location of resources.')
param location string

@description('Azure Devops Organization.')
param organization string

@description('Azure Devops Project.')
param project string

@description('ObjectId of ServiceConnection used for deployment.')
param serviceConnectionObjectId string

@description('Personal Access Token for Azure DevOps.')
param personalAccessToken string

// ------------ Variables ------------
var resourceGroupType = 'rg'
var resourceGroupName = '${companyPrefix}-${resourceGroupType}-${purpose}-${environment}'

var groupObjectIds = {
  aItUtvecklare: '40522447-c5bf-4fa2-bbfc-6c5903a3f81a'
  azureDevOpsUtvecklare: '6ad27c94-eb4e-49d1-bc1a-eb24db26ad3d'
}

var keyVaultPermissions = {
  readAll: {
    secrets: ['List', 'Get']
    certificates: ['List', 'Get']
    keys: ['List', 'Get']
  }
  serviceConnectionPermissions: {
    secrets: ['Get', 'List', 'Set', 'Delete']
  }
}

// ------------ Functions ------------
func getKeyVaultAccessPolicy(objectId string, permissions object) object => {
  tenantId: subscription().tenantId
  objectId: objectId
  permissions: permissions
}

// ------------ Resources ------------
targetScope = 'subscription'

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: resourceGroupName
  location: location
}

// Key vault
module keyVault 'br/ume:microsoft.keyvault.vaults:v2.0' = {
  scope: resourceGroup
  name: 'keyVault'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    accessPolicies: [
      getKeyVaultAccessPolicy(serviceConnectionObjectId, keyVaultPermissions.serviceConnectionPermissions)
      getKeyVaultAccessPolicy(app_estateservice.outputs.principalId, keyVaultPermissions.readAll)
      getKeyVaultAccessPolicy(appSlot_estateservice.outputs.principalId, keyVaultPermissions.readAll)

      ...environment == 'dev'
        ? [
            getKeyVaultAccessPolicy(groupObjectIds.aItUtvecklare, keyVaultPermissions.readAll)
            getKeyVaultAccessPolicy(groupObjectIds.azureDevOpsUtvecklare, keyVaultPermissions.readAll)
          ]
        : []
    ]
  }
}

// App Service Plan
module appServicePlan 'br/ume:microsoft.web.serverfarms:v2.0' = {
  scope: resourceGroup
  name: 'appServicePlan'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    skuName: environment == 'prod' ? 'P2V3' : 'P0V3'
  }
}

// App Service - EstateService
module app_estateservice 'br/ume:microsoft.web.sites:v2.1' = {
  scope: resourceGroup
  name: 'app_estateservice'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: 'estateservice'

    appServicePlanId: appServicePlan.outputs.id
  }
}

// App Service (Slot) - EstateService
module appSlot_estateservice 'br/ume:microsoft.web.sites.slots:v2.0' = {
  scope: resourceGroup
  name: 'appSlot_estateservice'
  params: {
    environment: environment

    appName: app_estateservice.outputs.name
    appServicePlanId: appServicePlan.outputs.id
  }
}

// Library variables
module libraryVariables 'library-variable-group.bicep' = {
  scope: resourceGroup
  name: 'libraryVariables'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    variableGroupPurpose: purpose

    generalPurpose: 'general'

    personalAccessToken: personalAccessToken
    organization: organization
    project: project
  }
}
