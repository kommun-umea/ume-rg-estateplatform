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

@description('Personal Access Token for Azure DevOps.')
param personalAccessToken string

@description('The current date and time in UTC format.')
param dateNowUtc string = utcNow()

// ------------ Variables ------------
var resourceGroupType = 'rg'
var resourceGroupTags = {
  Environment: environment
  LastUpdated: dateNowUtc
}
var resourceGroupName = '${companyPrefix}-${resourceGroupType}-${purpose}-${environment}'

var databasePurposes = {
  estateservice: 'estateservice'
}
var roleDefinitionIds = {
  CognitiveServicesOpenAIUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

// ------------ Functions ------------
func getKeyVaultAssignee(principalType string, principalId string) object => {
  principalId: principalId
  principalType: principalType
}

// ------------ Dependencies ------------
// Application Insights - General
resource dependency_applicationInsights_general 'Microsoft.Insights/components@2020-02-02' existing = {
  scope: az.resourceGroup('${companyPrefix}-rg-general-${environment}')
  name: '${companyPrefix}-appi-general-${environment}'
}

// ------------ Resources ------------
targetScope = 'subscription'

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceGroupTags
}

// Default Role Assignments
module defaultRoleAssignments 'br/ume:umea.roleassignments.turkos.defaults:v2.0' = {
  scope: resourceGroup
  name: 'defaultRoleAssignments'
  params: {
    environment: environment
  }
}

// Role Assignment - Cognitive Services OpenAI User
module openaiRoleAssignments 'br/ume:microsoft.authorization.roleassignments:v2.2' = {
  scope: resourceGroup
  name: 'openaiRoleAssignments'
  params: {
    roleDefinitionId: roleDefinitionIds.CognitiveServicesOpenAIUser
    assignees: [
      {
        principalId: app_estateservice.outputs.principalId
        principalType: 'ServicePrincipal'
      }
      {
        principalId: app_estateservice.outputs.?stageDeploymentSlot.principalId!
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// Key Vault
module keyVault 'br/ume:microsoft.keyvault.vaults:v2.1' = {
  scope: resourceGroup
  name: 'keyVault'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    permissions: [
      {
        role: 'Key Vault User'
        assignees: [
          getKeyVaultAssignee('ServicePrincipal', app_estateservice.outputs.principalId)
          getKeyVaultAssignee('ServicePrincipal', app_estateservice.outputs.?stageDeploymentSlot.principalId!)
        ]
      }
    ]
  }
}

// App Service Plan
module appServicePlan 'br/ume:microsoft.web.serverfarms:v2.3' = {
  scope: resourceGroup
  name: 'appServicePlan'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    skuName: environment == 'prod' ? 'P1V4' : 'P0V4'
  }
}

// App Service - EstateService
module app_estateservice 'br/ume:microsoft.web.sites:v2.3' = {
  scope: resourceGroup
  name: 'app_estateservice'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: 'estateservice'
    dateNowUtc: dateNowUtc

    appServicePlanId: appServicePlan.outputs.id
    withStageDeploymentSlot: true
  }
}

// Storage Account - Image Cache
module storageAccount 'br/ume:microsoft.storage.storageaccounts:v2.1' = {
  scope: resourceGroup
  name: 'storageAccount'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: 'estateplatform'
    dateNowUtc: dateNowUtc

    containers: [
      {
        purpose: 'imagecache'
      }
    ]
    contributors: [
      {
        principalId: app_estateservice.outputs.principalId
        principalType: 'ServicePrincipal'
      }
      {
        principalId: app_estateservice.outputs.?stageDeploymentSlot.principalId!
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// OpenAI
module openai 'br/ume:microsoft.cognitiveservices.openai:v2.0' = {
  scope: resourceGroup
  name: 'openai'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    location: location
    purpose: purpose

    deployments: [
      {
        name: 'gpt5nano'
        sku: {
          name: 'DataZoneStandard'
          capacity: 10
        }
        model: {
          name: 'gpt-5-nano'
          version: '2025-08-07'
        }
      }
    ]
  }
}

// SQL Server
module sqlServer 'br/ume:microsoft.sql.servers:v2.1' = {
  scope: resourceGroup
  name: 'sqlServer'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    purpose: purpose
    dateNowUtc: dateNowUtc

    databases: [
      {
        purpose: databasePurposes.estateservice
        users: [
          app_estateservice.outputs.name
          app_estateservice.outputs.?stageDeploymentSlot.principalName!
        ]
      }
    ]
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
    dateNowUtc: dateNowUtc

    sqlServerName: sqlServer.outputs.name
    estateserviceSqldbName: sqlServer.outputs.databases.estateservice.name

    openaiEndpoint: openai.outputs.resourceUri
    applicationInsightsConnectionString: dependency_applicationInsights_general.properties.ConnectionString

    personalAccessToken: personalAccessToken
    organization: organization
    project: project
  }
}

// ------------ Outputs ------------
output sqlConfiguration object = {
  serverName: sqlServer.outputs.name
  serverPrincipalId: sqlServer.outputs.principalId
  serverFullyQualifiedDomainName: sqlServer.outputs.fullyQualifiedDomainName
  databases: sqlServer.outputs.databases
}
