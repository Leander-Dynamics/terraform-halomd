# Option A — artbit Infrastructure (Terraform, Azure)

This repository contains **infrastructure only** (no application code).  
It provisions the **artbit** platform into Azure across **dev, qa, stage, prod**.

## Key points

- Canonical modules live under `platform/infra/Azure/modules/`
- Workflow module: `platform/infra/Azure/modules/artbit`
- **AKS/ACR removed**
- **Key Vault Private Endpoint + Private DNS**
  - **Dev**: disabled by default (public access enabled for speed)
  - **QA/Stage/Prod**: enabled (public access disabled)
- **No conflict code**: the legacy `new-terraform-modules/**` tree is removed and the old module name `arbit_workflow` is not used.

## Pipelines

- **Dev only**: `azure-pipelines-infra-dev.yml`
- **All envs**: `azure-pipelines-infra-all.yml` (Dev → QA → Stage → Prod, with approvals)

Both YAMLs consume Azure DevOps variable groups:

- `terraform-global-common` — e.g., `TF_VERSION`, `TF_LOCK_TIMEOUT`, `PROJECT_NAME`, `LOCATION_DEFAULT`
- `terraform-<env>` — e.g., `AZ_SUBSCRIPTION_ID`, `AZ_TENANT_ID`, `ENV_NAME`, `SERVICE_CONNECTION`, `TF_BACKEND_*`, `TFSTATE_KV_NAME`

## Per-environment toggles (in `platform/infra/envs/<env>/terraform.tfvars`)

```hcl
# Dev
enable_key_vault_private_endpoint = false

# QA / Stage / Prod
enable_key_vault_private_endpoint = true

# All envs
vault_dns_zone_name           = "privatelink.vaultcore.azure.net"
vault_dns_resource_group_name = "hub-eus2-vnet-rg-1" # update if your hub DNS RG differs
```

## Local usage

```bash
az login
az account set --subscription "<subscription-guid>"

cd platform/infra/envs/dev
terraform init -backend-config=backend.tfvars
terraform validate
terraform plan -var-file=terraform.tfvars
terraform apply -var-file=terraform.tfvars
```

## Conflict guards

Before any apply, the pipelines check and fail if any of these are reintroduced:

- `new-terraform-modules`
- `arbit_workflow`
- `platform/infra/Azure/modules/aks`
- `platform/infra/Azure/modules/acr`