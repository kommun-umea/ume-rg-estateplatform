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

# ------------ Variables ------------
$parametersFilePath = (Resolve-Path ".\iac\parameters.$environment.json").Path
$parameters = (Get-Content -Path $parametersFilePath | ConvertFrom-Json).parameters

if ($environment -ne $parameters.environment.value) {
  throw "[Ume]: Environment parameter mismatch. Expected [$environment] but got [$($parameters.environment.value)]"
}

$companyPrefix = $parameters.companyPrefix.value
$purpose = $parameters.purpose.value
$location = $parameters.location.value
$templateFile = "./iac/main.bicep"
$resourceGroupAbbreviation = "rg"
$resourceGroupName = "$companyPrefix-$resourceGroupAbbreviation-$purpose-$environment"
$deploymentName = "${resourceGroupName}_$(Get-Date -Format 'yyyyMMddTHHmmss')"

# ------------ Resources ------------
# Resources
Write-Host "[Ume]: Deploying [$resourceGroupName]..."
az deployment sub create --name $deploymentName --location $location --template-file $templateFile --parameters $parametersFilePath personalAccessToken=$personalAccessToken --output none

# Finish
Write-Host "[Ume]: Finished deployment of [$resourceGroupName]"