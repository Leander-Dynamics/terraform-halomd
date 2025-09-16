[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)] [string] $ResourceGroup,
    [Parameter(Mandatory=$true)] [string] $AppName,
    [Parameter(Mandatory=$true)] [string] $Environment,
    [string] $Configuration = 'Release',
    [string] $OutputDirectory,
    [string] $Slot,
    [switch] $UseZipDeploy
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ResourceGroup = $ResourceGroup.Trim()
$AppName = $AppName.Trim()
$Environment = $Environment.Trim()
if ($PSBoundParameters.ContainsKey('Configuration')) {
    $Configuration = $Configuration.Trim()
    if ([string]::IsNullOrWhiteSpace($Configuration)) {
        $Configuration = 'Release'
    }
}
if ($Slot) {
    $Slot = $Slot.Trim()
    if ([string]::IsNullOrWhiteSpace($Slot)) {
        $Slot = $null
    }
}
if ($OutputDirectory) {
    $OutputDirectory = $OutputDirectory.Trim()
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        $OutputDirectory = $null
    }
}

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    throw 'Resource group is required.'
}

if ([string]::IsNullOrWhiteSpace($AppName)) {
    throw 'App Service name is required.'
}

if ([string]::IsNullOrWhiteSpace($Environment)) {
    throw 'Environment name is required.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet CLI is required but was not found on PATH.'
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI (az) is required but was not found on PATH.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'Node.js (npm) is required but was not found on PATH.'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$solutionPath = Join-Path $repoRoot 'Arbitration/MPArbitration.sln'
$clientAppPath = Join-Path $repoRoot 'Arbitration/MPArbitration/ClientApp'

if (-not (Test-Path $solutionPath)) {
    throw "Solution not found at $solutionPath"
}

if (-not (Test-Path $clientAppPath)) {
    throw "Client app folder not found at $clientAppPath"
}

$artifactRoot = if ($OutputDirectory) {
    if (-not (Test-Path $OutputDirectory)) {
        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    }
    (Resolve-Path -Path $OutputDirectory).Path
} else {
    $defaultPath = Join-Path $repoRoot "artifacts/arbitration/$Environment"
    New-Item -ItemType Directory -Path $defaultPath -Force | Out-Null
    (Resolve-Path -Path $defaultPath).Path
}

$publishDir = Join-Path $artifactRoot 'publish'
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Restoring .NET solution..."
dotnet restore $solutionPath --nologo

Write-Host "Installing client dependencies..."
npm ci --prefix $clientAppPath

$clientBuildScript = switch -Regex ($Environment.ToLowerInvariant()) {
    '^dev(elopment)?$'   { 'build-dev'; break }
    '^stage(ing)?$'     { 'build-stage'; break }
    '^prod(uction)?$'   { 'build'; break }
    default             { 'build'; break }
}

Write-Host "Building client app using 'npm run $clientBuildScript'..."
npm run $clientBuildScript --prefix $clientAppPath

Write-Host "Publishing .NET solution to $publishDir..."
dotnet publish $solutionPath --configuration $Configuration --no-restore --output $publishDir --nologo

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$zipPath = Join-Path $artifactRoot ("MPArbitration-$Environment-$timestamp.zip")
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

Write-Host "Packaging publish folder into $zipPath..."
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

$azArgs = if ($UseZipDeploy.IsPresent) {
    $args = @('webapp', 'deployment', 'source', 'config-zip', '--resource-group', $ResourceGroup, '--name', $AppName, '--src', $zipPath)
    if ($Slot) { $args += @('--slot', $Slot) }
    $args
} else {
    $args = @('webapp', 'deploy', '--resource-group', $ResourceGroup, '--name', $AppName, '--src-path', $zipPath, '--type', 'zip')
    if ($Slot) { $args += @('--slot', $Slot) }
    $args
}

Write-Host "Deploying package to Azure Web App '$AppName' (resource group '$ResourceGroup')..."
az @azArgs

Write-Host "Deployment complete. Package path: $zipPath"
