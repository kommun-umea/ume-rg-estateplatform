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

# ------------ Outputs ------------
# Pass sqlConfiguration to the pipeline as an output variable for subsequent steps
$sqlConfigurationJson = $outputs.sqlConfiguration.value | ConvertTo-Json -Depth 10 -Compress
Write-Host "##vso[task.setvariable variable=sqlConfiguration;isOutput=true]$sqlConfigurationJson"

Write-Host "Finished deployment of [$resourceGroupName]"
