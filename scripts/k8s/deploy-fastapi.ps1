Param(
  [string]$Namespace = "dev"
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
$chartRoot = Join-Path $repoRoot 'charts/fastapi-app'

if (!(Test-Path $chartRoot)) { Write-Error "Chart not found: $chartRoot" }

$valuesBase = Join-Path $chartRoot 'values.yaml'
$valuesEnv  = Join-Path $chartRoot ("values.{0}.yaml" -f $Namespace)

$valuesArgs = @('-f', $valuesBase)
if (Test-Path $valuesEnv) { $valuesArgs += @('-f', $valuesEnv) }

helm upgrade --install fastapi $chartRoot -n $Namespace @valuesArgs | Out-Host
