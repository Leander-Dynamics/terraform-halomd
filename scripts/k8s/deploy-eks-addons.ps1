Param(
  [Parameter(Mandatory=$true)][string]$ClusterName,
  [Parameter(Mandatory=$true)][string]$Region,
  [string]$AlbControllerRoleArn,
  [switch]$SkipMetricsServer,
  [string]$MetricsNamespace = "kube-system",
  [string]$AlbNamespace = "kube-system",
  [string]$AlbControllerChartVersion,
  [string]$MetricsServerChartVersion
)

$ErrorActionPreference = 'Stop'

function Ensure-Tool($name) {
  $exists = (Get-Command $name -ErrorAction SilentlyContinue) -ne $null
  if (-not $exists) { throw "Required tool not found on PATH: $name" }
}

Ensure-Tool aws
Ensure-Tool kubectl
Ensure-Tool helm

Write-Host "Configuring kubeconfig for EKS cluster $ClusterName in $Region..."
aws eks update-kubeconfig --name $ClusterName --region $Region | Out-Host

helm repo add metrics-server https://kubernetes-sigs.github.io/metrics-server | Out-Null
helm repo add eks https://aws.github.io/eks-charts | Out-Null
helm repo update | Out-Host

if (-not $SkipMetricsServer) {
  Write-Host "Installing/Upgrading metrics-server in namespace '$MetricsNamespace'..."
  $metricsArgs = @(
    'upgrade','--install','metrics-server','metrics-server/metrics-server',
    '-n', $MetricsNamespace,'--create-namespace'
  )
  if ($MetricsServerChartVersion) { $metricsArgs += @('--version', $MetricsServerChartVersion) }
  helm @metricsArgs | Out-Host
  kubectl -n $MetricsNamespace rollout status deploy/metrics-server --timeout=120s | Out-Host
}

if ($AlbControllerRoleArn) {
  Write-Host "Installing/Upgrading AWS Load Balancer Controller in namespace '$AlbNamespace'..."
  $svcAcctName = 'aws-load-balancer-controller'

  # Create namespace if missing
  kubectl get ns $AlbNamespace 2>$null 1>$null
  if ($LASTEXITCODE -ne 0) { kubectl create ns $AlbNamespace | Out-Host }

  # Helm install/upgrade with IRSA annotation
  $albArgs = @(
    'upgrade','--install','aws-load-balancer-controller','eks/aws-load-balancer-controller',
    '-n', $AlbNamespace,
    '--set', "clusterName=$ClusterName",
    '--set', 'serviceAccount.create=true',
    '--set', "serviceAccount.name=$svcAcctName",
    '--set', "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn=$AlbControllerRoleArn",
    '--set', "region=$Region"
  )
  if ($AlbControllerChartVersion) { $albArgs += @('--version', $AlbControllerChartVersion) }
  helm @albArgs | Out-Host
  kubectl -n $AlbNamespace rollout status deploy/aws-load-balancer-controller --timeout=180s | Out-Host
} else {
  Write-Host "Skipping AWS Load Balancer Controller (no AlbControllerRoleArn provided)."
}

Write-Host "EKS add-ons deployment complete."
