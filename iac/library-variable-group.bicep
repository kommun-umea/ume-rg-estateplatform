// ------------ Parameters ------------
@allowed([
  'dev'
  'test'
  'prod'
])
param environment string
param companyPrefix string
param location string = resourceGroup().location
param variableGroupPurpose string
param tags object = {}
param dateNowUtc string = utcNow()

param generalPurpose string

param personalAccessToken string
param organization string
param project string

// ------------ Variables ------------
var libraryVariables = [
  {
    name: 'application-insights-connection-string'
    value: applicationInsights_general.properties.ConnectionString
  }
  {
    name: 'pythagoras-api-key'
    value: ''
  }
  {
    name: 'pythagoras-api-url'
    value: ''
  }
  {
    name: 'estateservice-api-key'
    value: ''
  }
]

// ------------ Resources ------------
// Application Insights - General
resource applicationInsights_general 'Microsoft.Insights/components@2020-02-02' existing = {
  scope: az.resourceGroup('${companyPrefix}-rg-${generalPurpose}-${environment}')
  name: '${companyPrefix}-appi-${generalPurpose}-${environment}'
}

// Deployment Script - Library Variables
module libraryVariablesDeploymentScript 'br/ume:umea.deploymentscripts.libraryvariables:v2.2' = {
  name: 'libraryVariablesDeploymentScript'
  params: {
    environment: environment
    companyPrefix: companyPrefix
    location: location
    variableGroupPurpose: variableGroupPurpose
    tags: tags
    dateNowUtc: dateNowUtc

    personalAccessToken: personalAccessToken
    organization: organization
    project: project
    libraryVariables: libraryVariables
  }
}
