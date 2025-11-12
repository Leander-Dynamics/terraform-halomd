Param(
  [string]$Namespace = "dev",
  [switch]$CreateFalconSecret,
  [string]$FalconClientId,
  [string]$FalconClientSecret
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
$chartRoot = Join-Path $repoRoot 'charts/edr-agent'

if (!(Test-Path $chartRoot)) { Write-Error "Chart not found: $chartRoot" }

$valuesBase = Join-Path $chartRoot 'values.yaml'
$valuesEnv  = Join-Path $chartRoot ("values.{0}.yaml" -f $Namespace)

$valuesArgs = @('-f', $valuesBase)
if (Test-Path $valuesEnv) { $valuesArgs += @('-f', $valuesEnv) }

if ($CreateFalconSecret) {
  if (-not $FalconClientId -or -not $FalconClientSecret) {
    $FalconClientId = $env:FALCON_CLIENT_ID
    $FalconClientSecret = $env:FALCON_CLIENT_SECRET
  }
  if (-not $FalconClientId -or -not $FalconClientSecret) {
    Write-Error "Falcon credentials not provided. Set params or FALCON_CLIENT_ID/FALCON_CLIENT_SECRET environment variables."
  }
  $secretYaml = @(
    "apiVersion: v1",
    "kind: Secret",
    "metadata:",
    "  name: falcon-creds",
    "  namespace: $Namespace",
    "type: Opaque",
    "stringData:",
    "  FALCON_CLIENT_ID: '$FalconClientId'",
    "  FALCON_CLIENT_SECRET: '$FalconClientSecret'"
  ) -join "`n"

  $tmp = New-TemporaryFile
  $secretYaml | Set-Content -Encoding UTF8 $tmp
  kubectl apply -f $tmp | Out-Host
  Remove-Item $tmp -Force
}

helm upgrade --install edr $chartRoot -n $Namespace @valuesArgs | Out-Host
