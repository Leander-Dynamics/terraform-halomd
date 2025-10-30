param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory=$true)]
    [string]$ClusterName,

    [Parameter(Mandatory=$false)]
    [string]$ContextName,

    [Parameter(Mandatory=$false)]
    [string]$OutputPath,

    [Parameter(Mandatory=$false)]
    [switch]$Admin,

    [Parameter(Mandatory=$false)]
    [switch]$Overwrite,

    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory=$false)]
    [string]$TenantId
)

$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Warning $msg }
function Write-Err($msg)  { Write-Error $msg }

# Ensure az exists
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Err "Azure CLI (az) not found. Install from https://aka.ms/install-azure-cli"
    exit 127
}

# Optional: set account
if ($SubscriptionId) {
    Write-Info "Setting Azure subscription to $SubscriptionId"
    az account set --subscription $SubscriptionId | Out-Null
}

if ($TenantId) {
    Write-Info "Verifying Azure login context for tenant $TenantId"
    $acct = az account show --query tenantId -o tsv 2>$null
    if (-not $acct -or $acct -ne $TenantId) {
        Write-Info "Logging in to tenant $TenantId via device code (browserless)"
        az login --tenant $TenantId --use-device-code | Out-Null
    }
}

# Build base command
$cmd = @('aks','get-credentials','--resource-group',$ResourceGroup,'--name',$ClusterName)
if ($Admin) { $cmd += '--admin' }
if ($Overwrite) { $cmd += '--overwrite-existing' }
if ($OutputPath) { $cmd += @('--file',$OutputPath) }

Write-Info "Fetching kubeconfig for $ResourceGroup/$ClusterName"
az @cmd

# Optionally rename context for convenience (requires kubectl)
if ($ContextName) {
    if (Get-Command kubectl -ErrorAction SilentlyContinue) {
        try {
            Write-Info "Attempting to rename current context to '$ContextName'"
            $current = kubectl config current-context 2>$null
            if ($LASTEXITCODE -eq 0 -and $current) {
                if ($current -ne $ContextName) {
                    kubectl config rename-context $current $ContextName | Out-Null
                    Write-Info "Context renamed: $current -> $ContextName"
                } else {
                    Write-Info "Context already named '$ContextName'"
                }
            } else {
                Write-Warn "Could not determine current context; skipping rename."
            }
        } catch {
            Write-Warn "Failed to rename context: $($_.Exception.Message)"
        }
    } else {
        Write-Warn "kubectl not found; skipping context rename. Install from https://kubernetes.io/docs/tasks/tools/"
    }
}

Write-Info "Done. To use: kubectl get nodes"
