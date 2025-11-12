Param(
  [string]$EnvPath = "platform/infra/envs/dev",
  [string]$OutDir = "artifacts",
  [switch]$Guard
)

$ErrorActionPreference = 'Stop'

# Resolve repo root as the parent of this script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

$envFullPath = Join-Path $repoRoot $EnvPath
$outFullPath = Join-Path $repoRoot $OutDir

if (!(Test-Path $envFullPath)) {
  Write-Error "Environment path not found: $envFullPath"
}

if (!(Test-Path $outFullPath)) {
  New-Item -ItemType Directory -Path $outFullPath | Out-Null
}

$ts = (Get-Date).ToString('yyyyMMddTHHmmssZ')
$planPath = Join-Path $outFullPath "dev-plan-$ts.tfplan"
$jsonPath = Join-Path $outFullPath "dev-plan-$ts.json"
$txtSummaryPath = Join-Path $outFullPath "dev-plan-$ts-summary.txt"
$statusPath = Join-Path $outFullPath "dev-plan-$ts-status.json"

Push-Location $envFullPath
try {
  # Temporarily disable remote backend if present to allow local, no-auth planning
  $backendFile = Join-Path $envFullPath 'backend.tf'
  $backendBackup = Join-Path $envFullPath 'backend.tf.local-disabled'
  $backendTemporarilyDisabled = $false
  if (Test-Path $backendFile) {
    Rename-Item -Path $backendFile -NewName (Split-Path $backendBackup -Leaf)
    $backendTemporarilyDisabled = $true
  }

  Write-Host "Initializing Terraform (no backend)..."
  terraform init -backend=false | Write-Host

  Write-Host "Planning..." 
  terraform plan -out $planPath | Tee-Object -Variable planOutput | Out-Host
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Terraform plan failed. See output above."
  }

  Write-Host "Exporting plan to JSON and text..."
  terraform show -json $planPath | Set-Content -Encoding UTF8 $jsonPath
  terraform show $planPath | Set-Content -Encoding UTF8 $txtSummaryPath
}
finally {
  # Restore backend.tf if we disabled it
  if ($backendTemporarilyDisabled -and (Test-Path $backendBackup)) {
    Rename-Item -Path $backendBackup -NewName 'backend.tf'
  }
  Pop-Location
}

Write-Host "Analyzing plan JSON for guardrails..."
$json = Get-Content $jsonPath -Raw | ConvertFrom-Json
$changes = @()
if ($null -ne $json.resource_changes) { $changes = $json.resource_changes }

function HasAction($rc, $action) { return ($rc.change.actions -contains $action) }

$adds      = ($changes | Where-Object { HasAction $_ 'create' }).Count
$updates   = ($changes | Where-Object { HasAction $_ 'update' }).Count
$deletes   = ($changes | Where-Object { HasAction $_ 'delete' }).Count
$replaces  = ($changes | Where-Object { HasAction $_ 'replace' }).Count

# SQL-only change count (mssql and legacy sql resources)
# Count Azure SQL changes (back-compat)
$sqlChanges = ($changes | Where-Object { $_.type -like 'azurerm_mssql_*' -or $_.type -like 'azurerm_sql_*' }).Count

# Count AWS RDS changes
$awsRdsChanges = ($changes | Where-Object { $_.type -like 'aws_db_*' -or $_.type -like 'aws_rds_*' -or $_.type -eq 'aws_rds_cluster' -or $_.type -eq 'aws_rds_cluster_instance' }).Count

# Aggregate DB-only change count across clouds
$dbChanges = $sqlChanges + $awsRdsChanges

$status = [pscustomobject]@{
  timestamp   = $ts
  adds        = $adds
  updates     = $updates
  deletes     = $deletes
  replaces    = $replaces
  sql_changes      = $sqlChanges       # Azure SQL changes (legacy key)
  aws_rds_changes  = $awsRdsChanges    # AWS RDS changes
  db_changes       = $dbChanges        # Aggregate DB changes
}

$status | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $statusPath

Write-Host "Plan summary:" ($status | Format-Table | Out-String)

if ($Guard) {
  if ($deletes -gt 0 -or $replaces -gt 0 -or $dbChanges -gt 0) {
    Write-Error ("Guardrails failed: deletes={0}, replaces={1}, db_changes={2} (azure_sql={3}, aws_rds={4})" -f $deletes, $replaces, $dbChanges, $sqlChanges, $awsRdsChanges)
    exit 2
  } else {
    Write-Host "Guardrails passed: no deletes/replaces/DB changes."
  }
}

Write-Host "Artifacts saved to: $outFullPath"
