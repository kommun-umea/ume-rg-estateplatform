# ------------ Parameters ------------
[CmdletBinding()]
param
(
  [Parameter(Mandatory = $true)]
  [ValidateSet("dev", "test", "prod")]
  [String] $environment,

  [Parameter(Mandatory = $true)]
  [String] $personalAccessToken
)
$ErrorActionPreference = 'Stop'

# ------------ Variables ------------
$parametersFilePath = (Resolve-Path "./iac/parameters.$environment.json").Path
$parameters = (Get-Content -Path $parametersFilePath | ConvertFrom-Json).parameters

if ($environment -ne $parameters.environment.value) {
  throw "Environment parameter mismatch. Expected [$environment] but got [$($parameters.environment.value)]"
}

$sqlScriptBase64 = $env:SQL_SCRIPT_BASE64
if ([string]::IsNullOrWhiteSpace($sqlScriptBase64)) {
  throw "SQL script content was expected but SQL_SCRIPT_BASE64 was not provided."
}
$sqlBytes = [Convert]::FromBase64String($sqlScriptBase64)
$sqlScriptContent = [System.Text.Encoding]::UTF8.GetString($sqlBytes)

$companyPrefix = $parameters.companyPrefix.value
$purpose = $parameters.purpose.value
$location = $parameters.location.value
$templateFile = "./iac/main.bicep"
$resourceGroupAbbreviation = "rg"
$resourceGroupName = "$companyPrefix-$resourceGroupAbbreviation-$purpose-$environment"
$deploymentName = "${resourceGroupName}_$(Get-Date -Format 'yyyyMMddTHHmmss')"

# ------------ Resources ------------
Write-Host "Deploying [$resourceGroupName]..."
$deployment = az deployment sub create `
  --name $deploymentName `
  --location $location `
  --template-file $templateFile `
  --parameters $parametersFilePath `
    personalAccessToken=$personalAccessToken `
  --output json | ConvertFrom-Json
$outputs = $deployment.properties.outputs

if ($null -ne $deployment) {
  Write-Host "Deployment succeeded!"
  Write-Host ""
} else {
  throw "Deployment failed!"
}

# ------------ SQL ------------
Write-Host "Configuring SQL permissions..."
# sqlConfiguration object structure:
# {
#   "serverName": "string",
#   "serverPrincipalId": "string",
#   "serverFullyQualifiedDomainName": "string",
#   "databases": {
#     "<databasePurpose>": {
#       "id": "string",
#       "name": "string",
#       "users": [
#         "string" // Name of Entra identity
#       ]
#     },
#     ...
#   }
# }
$scriptBlock = [ScriptBlock]::Create($sqlScriptContent)
& $scriptBlock -environment $environment -sqlConfiguration $outputs.sqlConfiguration.value

Write-Host "Finished deployment of [$resourceGroupName]"
