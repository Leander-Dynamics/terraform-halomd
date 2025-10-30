# AKS kubeconfig helper

This repo includes a PowerShell script to fetch/merge kubeconfig for an AKS cluster.

File: `scripts/kubeconfig-aks.ps1`

## Prereqs

- Azure CLI (`az`) installed and logged in.
- Optionally `kubectl` if you want to rename the context.

## Usage (PowerShell)

```powershell
# Merge into default ~/.kube/config (create if needed), overwrite existing entries
pwsh -File scripts/kubeconfig-aks.ps1 -ResourceGroup <rg> -ClusterName <aks> -Overwrite

# Admin credentials (cluster-admin), write to a specific file path
pwsh -File scripts/kubeconfig-aks.ps1 -ResourceGroup <rg> -ClusterName <aks> -Admin -OutputPath "C:\\temp\\kubeconfig"

# Provide subscription/tenant and rename context
pwsh -File scripts/kubeconfig-aks.ps1 -ResourceGroup <rg> -ClusterName <aks> -SubscriptionId <subId> -TenantId <tenantId> -ContextName <friendly-name> -Overwrite
```

## Notes

- Kubeconfig files are intentionally ignored by `.gitignore` and should not be committed.
- If you see auth errors, ensure your account has AKS `clusterUser`/`clusterAdmin` access as appropriate.
- On Windows, default kubeconfig lives at `%USERPROFILE%\\.kube\\config`.
