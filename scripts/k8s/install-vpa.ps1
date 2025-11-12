Param(
  [string]$Version = "v0.15.0",
  [string]$Namespace = "kube-system"
)

$ErrorActionPreference = 'Stop'

$base = "https://github.com/kubernetes/autoscaler/releases/download/vertical-pod-autoscaler-$Version"

$manifests = @(
  "$base/crd/vpa-v1-crd.yaml",
  "$base/deploy/cluster-autoscaler/vertical-pod-autoscaler.yaml"
)

foreach ($m in $manifests) {
  Write-Host "Applying $m ..."
  kubectl apply -f $m | Out-Host
}

Write-Host "VPA components applied. Verify pods in $Namespace namespace."
